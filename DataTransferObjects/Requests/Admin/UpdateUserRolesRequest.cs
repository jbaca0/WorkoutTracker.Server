namespace WorkoutTracker.Server.DataTransferObjects.Requests.Admin;

public class UpdateUserRolesRequest
{
    public List<string> Roles { get; set; } = [];
}
