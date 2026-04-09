using SmartTaskManager.Web.Models;

namespace SmartTaskManager.Web.Services;

public sealed class UserSession
{
    public event Action? Changed;

    public UserSummary? CurrentUser { get; private set; }

    public AccessTokenResult? CurrentToken { get; private set; }

    public bool HasActiveUser => CurrentUser is not null && CurrentToken is not null;

    public string? BearerToken => CurrentToken?.AccessToken;

    public DateTime? TokenExpiresAtUtc => CurrentToken?.ExpiresAtUtc;

    public void Start(UserSummary user, AccessTokenResult token)
    {
        CurrentUser = user;
        CurrentToken = token;
        Changed?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        CurrentToken = null;
        Changed?.Invoke();
    }
}
