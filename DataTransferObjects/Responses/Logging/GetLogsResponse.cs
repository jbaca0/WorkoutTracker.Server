using System;

namespace WorkoutTracker.Server.DataTransferObjects.Responses.Logging;

public class GetLogsResponse
{
    public List<Common.LogEntry> logs { get; set; } = new List<Common.LogEntry>();
    public int totalCount { get; set; }
}
