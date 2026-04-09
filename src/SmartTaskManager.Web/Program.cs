using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using SmartTaskManager.Web.Components;
using SmartTaskManager.Web.Options;
using SmartTaskManager.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddOptions<SmartTaskManagerApiOptions>()
    .Bind(builder.Configuration.GetSection(SmartTaskManagerApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<UsersApiClient>(ConfigureApiHttpClient);
builder.Services.AddHttpClient<TasksApiClient>(ConfigureApiHttpClient);

builder.Services.AddScoped<UserSession>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string EnsureTrailingSlash(string baseUrl)
{
    return baseUrl.EndsWith("/", StringComparison.Ordinal)
        ? baseUrl
        : $"{baseUrl}/";
}

static void ConfigureApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
{
    SmartTaskManagerApiOptions options = serviceProvider
        .GetRequiredService<IOptions<SmartTaskManagerApiOptions>>()
        .Value;

    httpClient.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl), UriKind.Absolute);
    httpClient.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
}
