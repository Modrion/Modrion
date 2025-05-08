namespace Modrion.Core;

public class NullLogger : IModrionLogger
{
    public void Debug(string message) { }

    public void Info(string message) { }

    public void Warn(string message) { }

    public void Error(string message) { }
}
