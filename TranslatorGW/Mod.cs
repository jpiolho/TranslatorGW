﻿using CsvHelper;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using TranslatorGW.Configuration;
using TranslatorGW.Template;

namespace TranslatorGW;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
    private const int TranslationBufferSize = 1024 * 64; // 64kb

    [Function(CallingConventions.Cdecl)]
    private delegate IntPtr HookStringParse(IntPtr arg1, IntPtr arg2, int stringId, IntPtr replacementTerm, IntPtr arg5, IntPtr arg6, IntPtr arg7);
    private IHook<HookStringParse> _hookStringParse;


    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;


    private Dictionary<int, string> _translationsById = new();
    private object _lockObject = new();

    private unsafe int* _gwLanguage = (int*)0;
    private int _currentLanguage = -1;

    private CancellationTokenSource _cancelAllTasksCts;
    private Task _taskDatabaseWrite;
    private ConcurrentQueue<DatabaseEntry> _databaseQueue;
    private IntPtr _translationBuffer;


    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        if (_hooks is null)
            throw new Exception("Hooks is null");

        var mainModule = Process.GetCurrentProcess().MainModule!;
        if (!_modLoader.GetController<IStartupScanner>().TryGetTarget(out var scanner))
            throw new Exception("Failed to get scanner");

        _cancelAllTasksCts = new CancellationTokenSource();


        _translationBuffer = Marshal.AllocHGlobal(TranslationBufferSize);

        if (_configuration.SQLiteEnable)
        {
            _databaseQueue = new();
            _taskDatabaseWrite = TaskDatabaseAsync(_cancelAllTasksCts.Token);
        }

        // Find the global address for selected language
        scanner.AddMainModuleScan("8B 35 ?? ?? ?? ?? 83 C4 20 56", result =>
        {
            unsafe
            {
                int offset = *(int*)(mainModule.BaseAddress + result.Offset + 2);
                _gwLanguage = (int*)offset;
                _logger.WriteLine($"LanguageGlobal: {(int)_gwLanguage:X8}");
            }
        });

        // Find the function that parses strings
        scanner.AddMainModuleScan("55 8B EC 83 EC 20 53 8B 5D ?? 56 8B 75 ?? 57 8B 7D ??", result =>
        {
            var offset = mainModule.BaseAddress + result.Offset;

            _logger.WriteLine($"StringParse offset: {offset:X8}");
            _hookStringParse = _hooks.CreateHook<HookStringParse>(StringParseHandler, (long)offset).Activate();
        });

        _logger.WriteLine("[TranslatorGW] Started", Color.Yellow);
    }


    public override void Resume()
    {
        _logger.WriteLine("[TranslatorGW] Resuming...", Color.Yellow);
        _hookStringParse?.Activate();
    }

    public override void Suspend()
    {
        _logger.WriteLine("[TranslatorGW] Suspending...", Color.Yellow);
        _hookStringParse?.Disable();
    }

    public override void Disposing()
    {
        _logger.WriteLine("[TranslatorGW] Disposing...", Color.Yellow);
        _cancelAllTasksCts.Cancel();
    }

    private async Task TaskDatabaseAsync(CancellationToken cancellationToken)
    {
        using var sql = new TranslationsSQLite(_configuration.SQLitePath);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (
                    !_databaseQueue.TryGetNonEnumeratedCount(out var count) ||  // Try get the count
                    count == 0 || // No items in the queue
                    !_databaseQueue.TryDequeue(out var entry) // Try get the item
                )
                {
                    await Task.Delay(10);
                    continue;
                }

                await sql.InsertTranslationAsync(entry.LanguageId, entry.StringId, entry.Term, cancellationToken);

                _logger.WriteLine($"Inserted into database: {entry.LanguageId} | {entry.StringId} | {entry.Term}");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.WriteLine($"[ERROR] ThreadDatabase: {ex}", Color.Red);
            }
        }
    }

    private void LoadLanguage(string? file)
    {
        _translationsById = new();

        if (string.IsNullOrEmpty(file))
            return;

        _logger.WriteLine($"Loading new language: {file}");

        using (var reader = new StreamReader(file))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<CsvRecord>();
            foreach (var record in records)
            {
                if (record.StringId.HasValue)
                    _translationsById[record.StringId.Value] = record.Text;
            }
        }
    }

    private string? GetOverrideLanguageFile(string? configurationString)
    {
        if (string.IsNullOrEmpty(configurationString))
            return null;

        return configurationString.Split(",")[1];
    }

    private unsafe IntPtr StringParseHandler(IntPtr arg1, IntPtr arg2, int stringId, IntPtr termPointer, IntPtr arg5, IntPtr arg6, IntPtr arg7)
    {
        try
        {
            if (_gwLanguage == (int*)0)
                return _hookStringParse.OriginalFunction(arg1, arg2, stringId, termPointer, arg5, arg6, arg7);

            // Did the language change?
            if (_currentLanguage != *_gwLanguage)
            {
                _currentLanguage = *_gwLanguage;
                _logger.WriteLine($"Set new language: {_currentLanguage}");

                string? overrideFile = null;
                try
                {
                    overrideFile = GetOverrideLanguageFile(_currentLanguage switch
                    {
                        0 => _configuration.EnglishOverride,
                        1 => _configuration.KoreanOverride,
                        2 => _configuration.FrenchOverride,
                        3 => _configuration.GermanOverride,
                        4 => _configuration.ItalianOverride,
                        5 => _configuration.SpanishOverride,
                        6 => _configuration.TraditionalChineseOverride,
                        9 => _configuration.PolishOverride,
                        10 => _configuration.RussianOverride,
                        17 => _configuration.BorkBorkBorkOverride,
                        _ => null,
                    });

                    LoadLanguage(overrideFile);
                }
                catch (Exception ex)
                {
                    _logger.WriteLine($"Failed to load language file ({overrideFile}): {ex}", Color.Red);
                }

            }

            string? term = Marshal.PtrToStringUni(termPointer);
            string? originalTerm = term;
            if (term is not null)
            {
                // Are we saving translations into database?
                if (_databaseQueue is not null)
                {
                    _databaseQueue.Enqueue(new DatabaseEntry()
                    {
                        LanguageId = _currentLanguage,
                        StringId = stringId,
                        Term = term
                    });
                }


                bool translated = false;

                // Check if there's a translation for this stringId
                if (_translationsById.TryGetValue(stringId, out var translation))
                {
                    term = translation;
                    translated = true;

                    if (_configuration.TranslationVerbose)
                        _logger.WriteLine($"Translated {stringId}: {translation}");
                }
                else if (_configuration.TranslationVerbose)
                    _logger.WriteLine($"Translation not found for {stringId}: {originalTerm}");

                // Should we show stringId?
                if (_configuration.TranslationStringId == Config.TranslationStringIdMode.ShowAll ||
                    (_configuration.TranslationStringId == Config.TranslationStringIdMode.ShowIfNotTranslated && !translated)
                )
                {
                    //if (term[0] != '[' && term[^1] != ']' && term[0] != '<' && term[^1] != '>')
                        term = $"{{{stringId}|{term}}}";
                }

                // If the term has changed, then we have some text to change
                if (term != originalTerm)
                {
                    var encoded = Encoding.Unicode.GetBytes($"{term}\0");
                    int encodedSize = encoded.Length;

                    // Fail safe if somehow the buffer is bigger
                    if (encodedSize >= TranslationBufferSize)
                    {
                        encoded[TranslationBufferSize - 1] = 0;
                        encoded[TranslationBufferSize - 2] = 0;
                        encodedSize = TranslationBufferSize;

                        _logger.WriteLine($"WARNING: {stringId} translation overflows maximum translation buffer of {TranslationBufferSize} bytes. It has {encodedSize} bytes", Color.Yellow);
                    }

                    Marshal.Copy(encoded, 0, _translationBuffer, encodedSize);
                    termPointer = _translationBuffer;
                }
            }
            else
            {
                _logger.WriteLine($"[WARNING] PtrToStringUni returned null for string {stringId}.", Color.Yellow);
            }
        }
        catch (Exception ex)
        {
            _logger.WriteLine($"[ERROR] Exception when translating: {ex}");
        }

        return _hookStringParse.OriginalFunction(arg1, arg2, stringId, termPointer, arg5, arg6, arg7);
    }



    #region Standard Overrides
    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }
    #endregion

    #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod() { }
#pragma warning restore CS8618
    #endregion
}