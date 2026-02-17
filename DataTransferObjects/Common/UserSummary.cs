namespace WorkoutTracker.Server.DataTransferObjects.Common;

public class UserSummary
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}
