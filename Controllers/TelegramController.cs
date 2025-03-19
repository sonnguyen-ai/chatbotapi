using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using chatminimalapi.DTOs;
using System.Dynamic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace TelegramBotBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly SettingProvider _provider;
        private readonly TelemetryClient _telemetryClient;

        public TelegramController(
            ITelegramBotClient botClient, 
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration, 
            SettingProvider provider,
            TelemetryClient telemetryClient)
        {
            _botClient = botClient;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _provider = provider;
            _telemetryClient = telemetryClient;
        }

        //create ping
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong telegram!");
        }


        [HttpPost]
        public async Task<IActionResult> HandleUpdate([FromBody] Update update)
        {
            if (update?.Message?.Text == null || update.Message.Id == 0)
                return Ok(); // Ignore invalid updates

            string userMessage = update.Message.Text;
            long chatId = update.Message.Chat.Id;

            // Log the incoming message to Azure Application Insights
            var properties = new Dictionary<string, string>
            {
                { "ChatId", chatId.ToString() },
                { "UserMessage", userMessage },
                { "MessageId", update.Message.MessageId.ToString() },
                { "Username", update.Message.From?.Username ?? "unknown" }
            };

            // Log as a custom event
            _telemetryClient.TrackEvent("TelegramMessageReceived", properties);

            try
            {
                // _=await GetLlmResponse(llmResponse);
                // Send response back to Telegram
                await _botClient.SendTextMessageAsync(chatId, $"You said: {userMessage}");
                
                // Log successful response
                _telemetryClient.TrackEvent("TelegramMessageSent", properties);
            }
            catch (Exception ex)
            {
                // Log any errors that occur
                _telemetryClient.TrackException(ex, properties);
                
                // You might want to log additional details about the error
                var errorProperties = new Dictionary<string, string>(properties)
                {
                    { "ErrorMessage", ex.Message },
                    { "StackTrace", ex.StackTrace }
                };
                _telemetryClient.TrackTrace("Error sending Telegram message", SeverityLevel.Error, errorProperties);
            }

            return Ok();
        }

        private async Task<string> GetLlmResponse(string message)
        {
            var tenantId = "voiz";
            //setting by tenantId
            var setting = _provider.settings.Find(s => string.Equals(s.TenantId, tenantId, StringComparison.OrdinalIgnoreCase));

            var baseUrl = setting.Configuration.BaseUrl;
            var url = setting.Configuration.Url;
            var model = setting.Configuration.Model;
            var key = setting.Configuration.Key;
            var prompt = setting.Configuration.Prompt;
            var instruction = setting.Configuration.instruction;
            var client = _httpClientFactory.CreateClient();
            var template = "{\"contents\":[{\"parts\":[{\"text\":\"{0}\"}],\"role\":\"model\"},null,{\"parts\":[{\"text\":\"{1}\"}],\"role\":\"model\"},{\"parts\":[{\"text\":\"hi\"}],\"role\":\"user\"}],\"safetySettings\":[{\"category\":\"HARM_CATEGORY_HARASSMENT\",\"threshold\":\"BLOCK_MEDIUM_AND_ABOVE\"},{\"category\":\"HARM_CATEGORY_HATE_SPEECH\",\"threshold\":\"BLOCK_MEDIUM_AND_ABOVE\"},{\"category\":\"HARM_CATEGORY_SEXUALLY_EXPLICIT\",\"threshold\":\"BLOCK_MEDIUM_AND_ABOVE\"},{\"category\":\"HARM_CATEGORY_DANGEROUS_CONTENT\",\"threshold\":\"BLOCK_MEDIUM_AND_ABOVE\"}],\"generationConfig\":{\"maxOutputTokens\":300,\"temperature\":0.7,\"topK\":1,\"topP\":0.8}}";

            template = template.Replace("{0}", instruction).Replace("{1}", prompt);

            var requestContent = new StringContent(template, System.Text.Encoding.UTF8, Request.ContentType);
            var postUrl = $"{baseUrl}{url}".Replace("{0}", model).Replace("{1}", key);

            // Forward the POST request to the backend service
            var response = await client.PostAsync(postUrl, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            // var client = _httpClientFactory.CreateClient();
            // var llmUrl = _configuration["LlmApi:Url"];
            // var requestBody = new { message }; // Adjust based on your LLM API requirements

            // var response = await client.PostAsJsonAsync(llmUrl, requestBody);
            // response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LlmResponse>();
            return result?.Response ?? "Sorry, something went wrong.";
        }
    }

    // Class to deserialize LLM API response (adjust based on your API)
    public class LlmResponse
    {
        public string Response { get; set; }
    }
}
