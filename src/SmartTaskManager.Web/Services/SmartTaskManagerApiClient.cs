using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTaskManager.Web.Models;
using SmartTaskManager.Web.Models.Requests;

namespace SmartTaskManager.Web.Services;

public sealed class SmartTaskManagerApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly HttpClient _httpClient;

    public SmartTaskManagerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyCollection<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "api/users");
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<List<UserSummary>>(response, cancellationToken);
    }

    public async Task<UserSummary> CreateUserAsync(
        CreateUserRequest user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        using HttpRequestMessage request = new(HttpMethod.Post, "api/users")
        {
            Content = JsonContent.Create(user, options: SerializerOptions)
        };

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<UserSummary>(response, cancellationToken);
    }

    public async Task<AccessTokenResult> CreateTokenAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "api/auth/token")
        {
            Content = JsonContent.Create(new CreateUserRequest(userName), options: SerializerOptions)
        };

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<AccessTokenResult>(response, cancellationToken);
    }

    public async Task<TaskDashboardSummary> GetDashboardAsync(
        Guid userId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"api/users/{userId}/tasks/dashboard",
            accessToken);

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<TaskDashboardSummary>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetTasksAsync(
        Guid userId,
        string accessToken,
        TaskQueryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        string requestUri = BuildTasksUri(userId, filter);
        using HttpRequestMessage request = CreateAuthorizedRequest(HttpMethod.Get, requestUri, accessToken);
        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<List<TaskItem>>(response, cancellationToken);
    }

    public async Task<TaskItem> GetTaskAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"api/users/{userId}/tasks/{taskId}",
            accessToken);

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<TaskItem>(response, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskHistoryEntry>> GetTaskHistoryAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"api/users/{userId}/tasks/{taskId}/history",
            accessToken);

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<List<TaskHistoryEntry>>(response, cancellationToken);
    }

    public async Task<TaskItem> CreateTaskAsync(
        Guid userId,
        CreateTaskRequest task,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        using HttpRequestMessage request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"api/users/{userId}/tasks",
            accessToken);

        request.Content = JsonContent.Create(task, options: SerializerOptions);

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<TaskItem>(response, cancellationToken);
    }

    public async Task<TaskItem> CompleteTaskAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"api/users/{userId}/tasks/{taskId}/complete",
            accessToken);

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<TaskItem>(response, cancellationToken);
    }

    public async Task<TaskItem> ArchiveTaskAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"api/users/{userId}/tasks/{taskId}/archive",
            accessToken);

        using HttpResponseMessage response = await SendAsync(request, cancellationToken);
        return await ReadRequiredAsync<TaskItem>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        throw await CreateApiExceptionAsync(response, cancellationToken);
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken)
    {
        HttpRequestMessage request = new(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        T? result = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("The API returned an empty response.");
        }

        return result;
    }

    private static async Task<SmartTaskManagerApiException> CreateApiExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        ApiErrorDetails? error = null;

        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorDetails>(SerializerOptions, cancellationToken);
        }
        catch (JsonException)
        {
        }

        string message = error is null
            ? $"The API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
            : string.Join(
                " ",
                new[] { error.Title, error.Detail }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

        return new SmartTaskManagerApiException((int)response.StatusCode, message, error);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string BuildTasksUri(Guid userId, TaskQueryFilter? filter)
    {
        if (filter is null)
        {
            return $"api/users/{userId}/tasks";
        }

        List<string> query = new();

        if (filter.Status.HasValue)
        {
            query.Add($"status={Uri.EscapeDataString(filter.Status.Value.ToString())}");
        }

        if (filter.Priority.HasValue)
        {
            query.Add($"priority={Uri.EscapeDataString(filter.Priority.Value.ToString())}");
        }

        if (filter.Overdue)
        {
            query.Add("overdue=true");
        }

        return query.Count == 0
            ? $"api/users/{userId}/tasks"
            : $"api/users/{userId}/tasks?{string.Join("&", query)}";
    }
}
