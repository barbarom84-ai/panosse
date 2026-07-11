using System;

namespace Panosse.Services;

public interface ITelemetryService
{
    void Increment(string counterName, long amount = 1);
    void AddToCounter(string counterName, long amount);
    void RecordDuration(string metricName, TimeSpan duration);
}
