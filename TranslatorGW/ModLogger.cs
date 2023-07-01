using Reloaded.Mod.Interfaces;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace TranslatorGW;

internal class ModLogger
{
    private ILogger _logger;
    private string _modName;

    public ModLogger(string modName, ILogger logger)
    {
        _modName = modName;
        _logger = logger;
    }

    public void Log(string message)
    {
        _logger.WriteLine($"[{_modName}] {message}");
    }

    public void Verbose(string message)
    {
        _logger.WriteLine($"[{_modName}] {message}", Color.Gray);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Verbose(bool condition, string message)
    {
        if (condition)
            Verbose(message);
    }

    public void Warning(string message)
    {
        _logger.WriteLine($"[{_modName}] WARNING: {message}", Color.Yellow);
    }

    public void Error(string message)
    {
        _logger.WriteLine($"[{_modName}] ERROR: {message}", Color.Red);
    }
}
