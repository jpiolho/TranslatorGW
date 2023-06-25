using CsvHelper;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using TranslatorGW.Configuration;
using TranslatorGW.Template;

namespace TranslatorGW;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public class Mod : ModBase // <= Do not Remove.
{
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
    private Dictionary<string, string> _translationsByText = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<int> _ignoredTranslations = new();
    private object _lockObject = new();

    private unsafe int* _gwLanguage = (int*)0;
    private int _currentLanguage = -1;

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

    }

    private void LoadLanguage(string? file)
    {
        _ignoredTranslations = new();
        _translationsById = new();
        _translationsByText = new();

        if (string.IsNullOrEmpty(file))
            return;

        _logger.WriteLine($"Loading new language: {file}");

        using (var reader = new StreamReader(file))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<CsvRecord>();
            foreach (var record in records)
            {
                if (record.Ignored.HasValue && record.Ignored.Value)
                {
                    if (record.StringId is null)
                    {
                        _logger.WriteLine($"Found translation that's set to be ignored without StringId");
                        continue;
                    }

                    _ignoredTranslations.Add(record.StringId.Value);
                }
                else if (record.StringId.HasValue)
                    _translationsById[record.StringId.Value] = record.Text;
                else
                    _translationsByText[record.OriginalText] = record.Text;
            }
        }
    }

    private string? GetOverrideLanguageFile(string? configurationString)
    {
        if (string.IsNullOrEmpty(configurationString))
            return null;

        return configurationString.Split(",")[1];
    }

    private unsafe IntPtr StringParseHandler(IntPtr arg1, IntPtr arg2, int stringId, IntPtr replacementTerm, IntPtr arg5, IntPtr arg6, IntPtr arg7)
    {
        if (_gwLanguage != (int*)0)
        {
            lock (_lockObject)
            {
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
            }

            if (_translationsById.TryGetValue(stringId, out var translation))
            {
                var encoded = Encoding.Unicode.GetBytes(translation).Append((byte)0).Append((byte)0).ToArray();
                Marshal.Copy(encoded, 0, replacementTerm, encoded.Length);

                _logger.WriteLine($"Translated {stringId}: {translation}");
            }
            else
            {
                if (!_ignoredTranslations.TryGetValue(stringId, out _))
                {
                    _logger.WriteLine($"Translation not found for {stringId}: {Marshal.PtrToStringUni(replacementTerm)}");
                }
            }
        }

        return _hookStringParse.OriginalFunction(arg1, arg2, stringId, replacementTerm, arg5, arg6, arg7);
    }

    private class CsvRecord
    {
        public int? StringId { get; set; }
        public string OriginalText { get; set; }
        public string Text { get; set; }
        public bool? Ignored { get; set; }
    }


    private class Translation
    {
        public string Text { get; set; } = "";
        public IntPtr Pointer { get; set; } = IntPtr.Zero;

        public Translation(string text) { Text = text; }
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