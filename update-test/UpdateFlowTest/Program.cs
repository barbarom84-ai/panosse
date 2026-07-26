using Panosse.Services;

string currentVersion = args.Length > 0 ? args[0] : "2.2.0";
const string repo = "barbarom84-ai/panosse";

var service = new UpdateService();
UpdateCheckResult result = await service.CheckForUpdateAsync(repo, currentVersion);

if (result.VerificationFailed)
{
    Console.WriteLine("FAIL: verification failed");
    return 1;
}

if (result.IsUpToDate)
{
    Console.WriteLine("OK: up to date");
    return 0;
}

if (!result.HasUpdate || result.ReleaseInfo is null)
{
    Console.WriteLine("FAIL: unexpected result");
    return 1;
}

Console.WriteLine($"OK: update {result.ReleaseInfo.TagName}");
Console.WriteLine($"  Exe: {result.ReleaseInfo.ExeFileName}");
Console.WriteLine($"  SHA256: {result.ReleaseInfo.ExpectedSha256 ?? "(missing)"}");

if (string.IsNullOrWhiteSpace(result.ReleaseInfo.ExpectedSha256))
{
    Console.WriteLine("FAIL: missing SHA256");
    return 1;
}

string currentExe = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "Panosse.exe");
if (!File.Exists(currentExe))
{
    Console.WriteLine($"SKIP install: exe not found at {currentExe}");
    return 0;
}

using var orchestrator = new UpdateOrchestrator(
    new TelemetryService(),
    new OperationHistoryService(),
    new LoggingService());

await foreach (var progress in orchestrator.DownloadAndPrepareInstallAsync(
    result.ReleaseInfo.DownloadUrl,
    currentExe,
    result.ReleaseInfo.TagName,
    result.ReleaseInfo.ExpectedSha256))
{
    Console.WriteLine($"  {progress.ProgressPercent}% - {progress.Message}");
}

var install = await orchestrator.BuildInstallScriptAsync(
    Path.Combine(Path.GetTempPath(), $"Panosse-{result.ReleaseInfo.TagName}.exe"),
    currentExe,
    result.ReleaseInfo.TagName);

Console.WriteLine(install.Success
    ? $"OK: script ready at {install.ScriptPath}"
    : $"FAIL: {install.ErrorMessage}");

return install.Success ? 0 : 1;
