namespace SmartTaskManager.Web.Models;

public sealed record AccessTokenResult(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string UserName);
