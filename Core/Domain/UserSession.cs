namespace Vibik.Core.Domain;

public record UserSession(
    string Username,
    DateTime CreatedAt,
    bool IsActive
);

