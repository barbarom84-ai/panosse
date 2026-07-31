using System.Collections.Generic;

namespace Panosse.Services;

public interface IOperationHistoryService
{
    void AddEntry(OperationHistoryEntry entry);
    IReadOnlyList<OperationHistoryEntry> GetRecentEntries(int maxEntries = 20);
    void Clear();
}
