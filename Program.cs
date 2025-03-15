
using System.IO.Compression;
using System.Text;
using chatminimalapi.DTOs;
using chatminimalapi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
string databaseId = builder.Configuration["CosmosDB:DatabaseId"];

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.EnableForHttps = true; // Enable for HTTPS requests
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.SmallestSize; // Or Optimal
});


// Register CosmosClient only in non-development environments
if (!builder.Environment.IsDevelopment())
{
    string connectionString = builder.Configuration["CosmosDB:ConnectionString"];
    builder.Services.AddSingleton(s => new CosmosClient(connectionString));
}
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minimal API Gateway 123", Version = "v1" });
    c.OperationFilter<AddRequiredHeaderParameter>();
});

// Register the appropriate repository based on environment
builder.Services.AddSingleton<ISettingsRepository>(sp =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Use InMemorySettingsRepository with sample data in development
        return new InMemorySettingsRepository(chatminimalapi.Data.DevelopmentSettings.GetSampleSettings());
    }
    else
    {
        // Use CosmosSettingsRepository in other environments
        var cosmosClient = sp.GetRequiredService<CosmosClient>();
        var databaseId = builder.Configuration["CosmosDB:DatabaseId"] ?? "chatbot";
        return new CosmosSettingsRepository(cosmosClient, databaseId);
    }
});

// Register the SettingProvider
builder.Services.AddSingleton(sp =>
{
    var repository = sp.GetRequiredService<ISettingsRepository>();
    var settingProvider = new SettingProvider(repository);
    settingProvider.RefreshData().GetAwaiter().GetResult();
    return settingProvider;
});

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseResponseCompression();
app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            string authHeader = context.Request.Headers["Authorization"];
            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                // Get credentials from header
                var encodedUsernamePassword = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
                var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
                var username = decodedUsernamePassword.Split(':', 2)[0];
                var password = decodedUsernamePassword.Split(':', 2)[1];

                // Get configured credentials
                var configuredUsername = builder.Configuration["SwaggerUI:Username"];
                var configuredPassword = builder.Configuration["SwaggerUI:Password"];

                if (username == configuredUsername && password == configuredPassword)
                {
                    await next.Invoke();
                    return;
                }
            }

            // Return authentication challenge
            context.Response.Headers["WWW-Authenticate"] = "Basic";
            context.Response.StatusCode = 401;
            return;
        }

        await next.Invoke();
    });

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal API Gateway v1");
    if (!app.Environment.IsDevelopment())
    {
        options.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
        {
            ["activated"] = false
        };
    }
});




// Use CORS
app.UseCors("AllowAll");

// GET: Retrieve Setting by partitionKey
app.MapGet("/settings/{tenantId}", (string tenantId, HttpResponse httpResponse, SettingProvider provider) =>
{
    var setting = provider.settings.Find(s => string.Equals(s.TenantId, tenantId, StringComparison.OrdinalIgnoreCase));
    httpResponse.Headers["Access-Control-Max-Age"] = "public, max-age=86400";
    return Results.Ok(setting);
});


app.MapGet("/settings/refresh", async (HttpResponse httpResponse, SettingProvider provider) =>
{
    await provider.RefreshData();
    return Results.Ok("refreshed!");
});


app.MapGet("/messages/{tenantId}", async (string tenantId, ISettingsRepository settingsRepository) =>
{
    var response = await settingsRepository.GetMessagesAsync(tenantId, databaseId);
    if (response.StatusCode != System.Net.HttpStatusCode.OK)
    {
        return Results.NotFound();
    }

    var messagesResponse = new List<MessageResponse>();

    foreach (var message in response)
    {
        messagesResponse.Add(new MessageResponse
        {
            Id = message.Id,
            Text = message.Payload.Text,
            HideInChat = message.Payload.HideInChat,
            Role = message.Payload.Role
        });
    }

    return Results.Ok(messagesResponse);
});

app.MapGet("/ping", () => Results.Ok("Pong update the message today!"));

// Diagnostic endpoint to check which repository implementation is being used
app.MapGet("/diagnostic/repository-type", (ISettingsRepository repository) =>
{
    return Results.Ok(new
    {
        RepositoryType = repository.GetType().Name,
        Environment = app.Environment.EnvironmentName
    });
});

// POST: gateway
app.MapPost("/gateway/messages/llm/{tenantId}/", async (string tenantId, HttpClient client, HttpContext context, SettingProvider provider) =>
{
    //setting by tenantId
    var setting = provider.settings.Find(s => string.Equals(s.TenantId, tenantId, StringComparison.OrdinalIgnoreCase));

    var baseUrl = setting.Configuration.BaseUrl;
    var url = setting.Configuration.Url;
    var model = setting.Configuration.Model;
    var key = setting.Configuration.Key;

    var content = await new StreamContent(context.Request.Body).ReadAsStringAsync();
    var requestContent = new StringContent(content, System.Text.Encoding.UTF8, context.Request.ContentType);
    var postUrl = $"{baseUrl}{url}".Replace("{0}", model).Replace("{1}", key);

    // Forward the POST request to the backend service
    var response = await client.PostAsync(postUrl, requestContent);
    var responseContent = await response.Content.ReadAsStringAsync();

    return Results.Content(responseContent, response.Content.Headers.ContentType?.ToString(), System.Text.Encoding.UTF8, (int)response.StatusCode);
}).WithName("ForwardPostToGemini");

app.Run();
