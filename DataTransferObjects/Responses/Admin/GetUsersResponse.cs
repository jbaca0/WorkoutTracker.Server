using WorkoutTracker.Server.DataTransferObjects.Common;

namespace WorkoutTracker.Server.DataTransferObjects.Responses.Admin;

public class GetUsersResponse
{
    public List<UserSummary> Users { get; set; } = [];
}
