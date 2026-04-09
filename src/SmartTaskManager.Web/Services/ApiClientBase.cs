using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTaskManager.Web.Models;

namespace SmartTaskManager.Web.Services;

public abstract class ApiClientBase
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly HttpClient _httpClient;

    protected ApiClientBase(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    protected Task<TResponse> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return SendForJsonAsync<TResponse>(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);
    }

    protected Task<TResponse> GetAuthorizedAsync<TResponse>(
        string requestUri,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SendForJsonAsync<TResponse>(
            CreateAuthorizedRequest(HttpMethod.Get, requestUri, accessToken),
            cancellationToken);
    }

    protected Task<TResponse> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = new(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(request, options: SerializerOptions)
        };

        return SendForJsonAsync<TResponse>(message, cancellationToken);
    }

    protected Task<TResponse> PostAuthorizedAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = CreateAuthorizedRequest(HttpMethod.Post, requestUri, accessToken);
        message.Content = JsonContent.Create(request, options: SerializerOptions);

        return SendForJsonAsync<TResponse>(message, cancellationToken);
    }

    protected Task<TResponse> PatchAuthorizedAsync<TResponse>(
        string requestUri,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return SendForJsonAsync<TResponse>(
            CreateAuthorizedRequest(HttpMethod.Patch, requestUri, accessToken),
            cancellationToken);
    }

    protected Task<TResponse> PatchAuthorizedAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        HttpRequestMessage message = CreateAuthorizedRequest(HttpMethod.Patch, requestUri, accessToken);
        message.Content = JsonContent.Create(request, options: SerializerOptions);

        return SendForJsonAsync<TResponse>(message, cancellationToken);
    }

    private async Task<TResponse> SendForJsonAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = request;
        using HttpResponseMessage response = await _httpClient.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiExceptionAsync(response, cancellationToken);
        }

        TResponse? result = await response.Content.ReadFromJsonAsync<TResponse>(SerializerOptions, cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("The API returned an empty response.");
        }

        return result;
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
}
