using MelonLoader;
using Modrion.Core;
using System.Drawing;

namespace Modrion.MelonLoader;

public class MelonLoggerAdapter : IModrionLogger
{
    private readonly MelonLogger.Instance _logger;

    public MelonLoggerAdapter(MelonLogger.Instance logger)
    {
        _logger = logger;
    }

    public void Debug(string message)
    {
#if DEBUG
        _logger.Msg(Color.Magenta, message);
#endif
    }

    public void Info(string message) => _logger.Msg(Color.LightBlue, message);

    public void Warn(string message) => _logger.Warning(message);

    public void Error(string message) => _logger.Error(message);

    public void Fatal(string message) => _logger.BigError(message);
}