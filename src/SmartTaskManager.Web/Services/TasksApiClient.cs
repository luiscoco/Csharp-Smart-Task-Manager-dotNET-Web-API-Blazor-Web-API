using SmartTaskManager.Web.Models;
using SmartTaskManager.Web.Models.Requests;

namespace SmartTaskManager.Web.Services;

public sealed class TasksApiClient : ApiClientBase
{
    public TasksApiClient(HttpClient httpClient)
        : base(httpClient)
    {
    }

    public Task<TaskDashboardSummary> GetDashboardSummaryAsync(
        Guid userId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return GetAuthorizedAsync<TaskDashboardSummary>(
            $"api/users/{userId}/tasks/dashboard",
            accessToken,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskItem>> ListTasksAsync(
        Guid userId,
        string accessToken,
        TaskQueryFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        return await GetAuthorizedAsync<List<TaskItem>>(
            BuildTasksUri(userId, filter),
            accessToken,
            cancellationToken);
    }

    public Task<TaskItem> GetTaskAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return GetAuthorizedAsync<TaskItem>(
            $"api/users/{userId}/tasks/{taskId}",
            accessToken,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<TaskHistoryEntry>> GetTaskHistoryAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return await GetAuthorizedAsync<List<TaskHistoryEntry>>(
            $"api/users/{userId}/tasks/{taskId}/history",
            accessToken,
            cancellationToken);
    }

    public Task<TaskItem> CreateTaskAsync(
        Guid userId,
        CreateTaskRequest request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PostAuthorizedAsync<CreateTaskRequest, TaskItem>(
            $"api/users/{userId}/tasks",
            request,
            accessToken,
            cancellationToken);
    }

    public Task<TaskItem> UpdateTaskPriorityAsync(
        Guid userId,
        Guid taskId,
        UpdateTaskPriorityRequest request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return PatchAuthorizedAsync<UpdateTaskPriorityRequest, TaskItem>(
            $"api/users/{userId}/tasks/{taskId}/priority",
            request,
            accessToken,
            cancellationToken);
    }

    public Task<TaskItem> CompleteTaskAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return PatchAuthorizedAsync<TaskItem>(
            $"api/users/{userId}/tasks/{taskId}/complete",
            accessToken,
            cancellationToken);
    }

    public Task<TaskItem> ArchiveTaskAsync(
        Guid userId,
        Guid taskId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return PatchAuthorizedAsync<TaskItem>(
            $"api/users/{userId}/tasks/{taskId}/archive",
            accessToken,
            cancellationToken);
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
