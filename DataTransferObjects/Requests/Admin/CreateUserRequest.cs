namespace WorkoutTracker.Server.DataTransferObjects.Requests.Admin;

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}
