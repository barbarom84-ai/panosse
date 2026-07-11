namespace Panosse.Services;

public sealed class CleanupStepUpdate
{
    public int StepIndex { get; set; }
    public int TotalSteps { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public long StepFreedBytes { get; set; }
}
