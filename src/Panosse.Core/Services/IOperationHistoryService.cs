using System.Collections.Generic;

namespace Panosse.Services;

public interface IOperationHistoryService
{
    void AddEntry(OperationHistoryEntry entry);
    IReadOnlyList<OperationHistoryEntry> GetRecentEntries(int maxEntries = 20);
    void Clear();

    /// <summary>
    /// Exports recent entries to a CSV file under %AppData%\Panosse and returns the file path.
    /// </summary>
    string ExportToCsv(int maxEntries = 100);
}
