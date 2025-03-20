using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using chatminimalapi.DTOs;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using TelegramBotBackend.Services;

namespace TelegramBotBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly SettingProvider _provider;
        private readonly TelemetryClient _telemetryClient;
        private readonly ILlmService _llmService;

        public TelegramController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            SettingProvider provider,
            TelemetryClient telemetryClient,
            ILlmService llmService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _provider = provider;
            _telemetryClient = telemetryClient;
            _llmService = llmService;
        }

        //create ping
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong telegram!");
        }


        [HttpPost]
        public async Task<IActionResult> HandleUpdate([FromBody] TelegramUpdate update, [FromQuery] string tenantId)
        {
            if (update?.Message?.Text == null || update.Message.Id == 0)
                return Ok(); // Ignore invalid updates

            var informationKeyParam = tenantId.Split('-');
            var botKey = informationKeyParam[1]; // Extract botKey from tenantId
            var companyId = informationKeyParam[0]; // Extract companyId from tenantId
            var userMessage = update.Message.Text;
            long chatId = update.Message.Chat.Id;
            var client = _httpClientFactory.CreateClient();

            // Log the incoming message to Azure Application Insights
            var properties = new Dictionary<string, string>
            {
                { "ChatId", chatId.ToString() },
                { "UserMessage", userMessage },
                { "TenantId", tenantId },
                { "BotKey", botKey }
            };

            // Log as a custom event
            _telemetryClient.TrackEvent("TelegramMessageReceived", properties);

            //send to bot
            try
            {
                var messageToBot = await _llmService.GetResponseAsync(userMessage, chatId, companyId);

                var _botClient = new TelegramBotClient(botKey, client);

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
    }
}
