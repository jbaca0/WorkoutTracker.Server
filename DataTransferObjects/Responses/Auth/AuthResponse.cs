namespace WorkoutTracker.Server.DataTransferObjects.Responses;

public class AuthResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required List<string> Roles { get; set; }
}
