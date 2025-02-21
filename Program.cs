
using chatminimalapi.DTOs;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration["CosmosDB:ConnectionString"];
string databaseId = builder.Configuration["CosmosDB:DatabaseId"];
string containerMessageId = builder.Configuration["CosmosDB:ContainerMessageId"];

// Register CosmosClient
builder.Services.AddSingleton(s => new CosmosClient(connectionString));
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
builder.Services.AddSwaggerGen(c=>{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minimal API Gateway 123", Version = "v1" });
    c.OperationFilter<AddRequiredHeaderParameter>();
});

builder.Services.AddHttpClient();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal API Gateway v1");
    });
}

// Use CORS
app.UseCors("AllowAll");

// GET: Retrieve Setting by partitionKey
app.MapGet("/settings/{tenantId}", async (string tenantId, CosmosClient cosmosClient, HttpResponse httpResponse) =>
{
    var container = cosmosClient.GetContainer(databaseId, "settings");
    var query = "SELECT * FROM c";

    var iterator = container.GetItemQueryIterator<Setting>(query, requestOptions: new QueryRequestOptions
    {
        PartitionKey = new PartitionKey(tenantId)
    });

    var response = await iterator.ReadNextAsync();
    if (response.StatusCode != System.Net.HttpStatusCode.OK)
    {
        return Results.NotFound();
    }

    var settingsResponse = new List<SettingResponse>();

    foreach (var setting in response)
    {
        settingsResponse.Add(new SettingResponse
        {
            Id = setting.Id,
            TenantId = setting.TenantId,
            Configuration = setting.Configuration
        });
    }


    httpResponse.Headers["Cache-Control"] = "public, max-age=86400";
    return Results.Ok(settingsResponse.First());
});


// GET: Retrieve Product by partitionKey
app.MapGet("/messages/{tenderId}", async (string tenderId, CosmosClient cosmosClient) =>
{
    var container = cosmosClient.GetContainer(databaseId, containerMessageId);
    var query = "SELECT * FROM c";

    var iterator = container.GetItemQueryIterator<Message>(query, requestOptions: new QueryRequestOptions
    {
        PartitionKey = new PartitionKey(tenderId)
    });

    var response = await iterator.ReadNextAsync();
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

app.MapGet("/ping", () => Results.Ok("Pong!"));

// POST: gateway

app.MapPost("/gateway/{*path}", async (string path, HttpClient client, HttpContext context) =>
{
    var baseUrl = context.Request.Headers["BaseUrl"].ToString();
    var model = context.Request.Headers["Model"].ToString();
    var key = context.Request.Headers["Key"].ToString();

    var content = await new StreamContent(context.Request.Body).ReadAsStringAsync();
    var requestContent = new StringContent(content, System.Text.Encoding.UTF8, context.Request.ContentType);
    var postUrl = Path.Combine(baseUrl, path).Replace("{0}", model).Replace("{1}", key);
    // Forward the POST request to the backend service
    var response = await client.PostAsync(postUrl, requestContent);
    var responseContent = await response.Content.ReadAsStringAsync();

    return Results.Content(responseContent, response.Content.Headers.ContentType?.ToString(), System.Text.Encoding.UTF8, (int)response.StatusCode);
}).WithName("ForwardPostToGemini");


app.MapPost("/messages", async (MessageRequest messageRequest, CosmosClient cosmosClient, HttpRequest request) =>
{
    var tenantId = request.Headers["tenantId"];
    var roomId = request.Headers["roomId"];

    if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(roomId))
    {
        return Results.BadRequest();
    }

    var container = cosmosClient.GetContainer(databaseId, containerMessageId);
    var message = new Message
    {
        Id = Guid.NewGuid(),
        RoomId = roomId,
        Payload = new Payload
        {
            Text = messageRequest.parts.First().Text,
            HideInChat = false,
            Role = messageRequest.Role
        }
    };

    var response = await container.CreateItemAsync(message, new PartitionKey(message.RoomId));
    if (response.StatusCode != System.Net.HttpStatusCode.Created)
    {
        return Results.BadRequest();
    }

    return Results.Created($"/messages/{message.RoomId}", message);
});
app.Run();
