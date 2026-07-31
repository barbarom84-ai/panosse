namespace Panosse.Services;

public enum UpdatePhase
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    Ready,
    Error
}
