using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using chatminimalapi.DTOs;
using System.Dynamic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Net;
using Newtonsoft.Json;

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
        public async Task<IActionResult> HandleUpdate([FromBody] TelegramUpdate update, string tenantId)
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
            };

            // Log as a custom event
            _telemetryClient.TrackEvent("TelegramMessageReceived", properties);

            //send to bot
            try
            {
                var messageToBot = await GetLlmResponse(userMessage, chatId, tenantId);

                var response = await _botClient.SendMessage(chatId, messageToBot);
                //log information for response

                _telemetryClient.TrackEvent("TelegramMessageSent", new Dictionary<string, string>{
                    { "Payload", JsonConvert.SerializeObject(response) },
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                //log ex and trace
                var newproperties = new Dictionary<string, string>{
                    { "Exception", ex.Message },
                    { "StackTrace", ex.StackTrace },
                };
                _telemetryClient.TrackException(ex, newproperties);
                return BadRequest(ex.Message);
            }
        }

        private async Task<string> GetLlmResponse(string message, long chatId, string tenantId)
        {
            //setting by tenantId
            var setting = _provider.settings.Find(s => string.Equals(s.TenantId, tenantId, StringComparison.OrdinalIgnoreCase));

            var baseUrl = setting.Configuration.BaseUrl;
            var url = setting.Configuration.Url;
            var model = setting.Configuration.Model;
            var key = setting.Configuration.Key;
            var prompt = setting.Configuration.Prompt;
            var instruction = setting.Configuration.instruction;
            var client = _httpClientFactory.CreateClient();

            ChatHistory.AddMessage(chatId, new ChatMessage
            {
                Content = message,
                Timestamp = DateTime.Now,
                Role = "user"
            });

            var history = ChatHistory.GetHistory(chatId);

            var llmRequest = new LlmRequest
            {
                Contents = new List<Content>
                {
                    new Content
                    {
                        Parts = new List<Part> { new Part { Text = instruction } },
                        Role = "model"
                    },
                    new Content
                    {
                        Parts = new List<Part> { new Part { Text = prompt } },
                        Role = "model"
                    }
                },
                SafetySettings = new List<SafetySetting>
                {
                    new SafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new SafetySetting { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new SafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new SafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" }
                },
                GenerationConfig = new GenerationConfig
                {
                    MaxOutputTokens = 300,
                    Temperature = 0.7,
                    TopK = 1,
                    TopP = 0.8
                }
            };

            history.ForEach(x =>
            {
                llmRequest.Contents.Add(new Content
                {
                    Parts = new List<Part> { new Part { Text = x.Content } },
                    Role = x.Role
                });
            });

            var stringObj = JsonConvert.SerializeObject(llmRequest, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });

            var requestContent = new StringContent(stringObj, System.Text.Encoding.UTF8, Request.ContentType);
            var postUrl = $"{baseUrl}{url}".Replace("{0}", model).Replace("{1}", key);

            // Forward the POST request to the backend service
            var response = await client.PostAsync(postUrl, requestContent);
            if (!response.IsSuccessStatusCode)
                return "Error getting LLM response";

            var llmApiResponses = JsonConvert.DeserializeObject<LlmApiResponse>(await response.Content.ReadAsStringAsync());
            return llmApiResponses.Candidates[0].Content.Parts[0].Text;
        }
    }
}
