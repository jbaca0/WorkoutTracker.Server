using System;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTracker.Server.Authorization.Requirements;

public class WorkoutOwnerRequirement : IAuthorizationRequirement
{
}