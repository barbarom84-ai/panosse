using System.Collections.Generic;

namespace Panosse.Services;

public interface IDiagnosticsService
{
    IReadOnlyList<DiagnosticItem> RunChecks();
}
