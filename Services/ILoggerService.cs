using System;

namespace Panosse.Services;

public interface ILoggerService
{
    void LogInfo(string category, string message);
    void LogWarning(string category, string message);
    void LogError(string category, string message, Exception? exception = null);
}
