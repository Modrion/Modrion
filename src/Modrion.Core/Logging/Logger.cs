using System;

namespace Modrion.Core;

public static class Logger
{
    private static IModrionLogger _impl = new NullLogger();

    public static void SetLogger(IModrionLogger logger)
    {
        _impl = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static void Debug(string message) => _impl.Debug(message);

    public static void Info(string message) => _impl.Info(message);

    public static void Warn(string message) => _impl.Warn(message);

    public static void Error(string message) => _impl.Error(message);
}
