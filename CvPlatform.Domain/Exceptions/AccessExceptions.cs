namespace CvPlatform.Domain.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ForbiddenException(string message = "You do not have permission to perform this action.") : Exception(message);
