namespace CvPlatform.Domain.Entities;

public static class BuiltInAttributes
{
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string Location = "Location";
    public const string Photo = "Photo";

    public static readonly IReadOnlyList<string> All = [FirstName, LastName, Location, Photo];
}
