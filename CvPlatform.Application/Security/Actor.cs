using CvPlatform.Domain.Exceptions;

namespace CvPlatform.Application.Security;

public static class RoleNames
{
    public const string Candidate = "Candidate";
    public const string Recruiter = "Recruiter";
    public const string Admin = "Admin";

    public const string StaffRoles = Recruiter + "," + Admin;

    public static readonly IReadOnlyList<string> All = [Candidate, Recruiter, Admin];
}

/// <summary>The user performing an operation. Built by the API from the (DB-refreshed) claims.</summary>
public sealed record Actor(Guid? ProfileId, bool IsAdmin, bool IsRecruiter)
{
    public static readonly Actor Anonymous = new(null, false, false);

    public bool IsAuthenticated => ProfileId.HasValue;

    /// <summary>Recruiters and administrators (admins can perform every recruiter action).</summary>
    public bool IsStaff => IsAdmin || IsRecruiter;

    public bool IsOwner(Guid profileId) => ProfileId == profileId;

    public bool CanEditProfile(Guid profileId) => IsAdmin || IsOwner(profileId);

    public Guid RequireProfileId() => ProfileId ?? throw new ForbiddenException("Sign in to perform this action.");

    public void RequireStaff()
    {
        if (!IsStaff)
        {
            throw new ForbiddenException();
        }
    }

    public void RequireCanEditProfile(Guid profileId)
    {
        if (!CanEditProfile(profileId))
        {
            throw new ForbiddenException();
        }
    }
}
