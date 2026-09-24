namespace CvPlatform.Domain.Entities;

/// <summary>
/// System keys of the mandatory "Me" attributes. They live in the same attribute library
/// (can be added to position templates) but cannot be removed.
/// </summary>
public static class BuiltInAttributes
{
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string Location = "Location";
    public const string Photo = "Photo";

    public static readonly IReadOnlyList<string> All = [FirstName, LastName, Location, Photo];
}
