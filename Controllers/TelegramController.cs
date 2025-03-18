using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using chatminimalapi.DTOs;

namespace TelegramBotBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TelegramController(ITelegramBotClient botClient, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _botClient = botClient;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        //create ping
        [HttpGet("ping")]
        public IActionResult Ping(){
            return Ok("Pong telegram!");
        }


        [HttpPost]
        public async Task<IActionResult> HandleUpdate([FromBody] TelegramUpdate update)
        {
            if (update?.Message?.Text == null || update.Message.ChatId == 0)
                return Ok(); // Ignore invalid updates

            string userMessage = update.Message.Text;
            long chatId = update.Message.ChatId;

            // Send message to LLM API
            string llmResponse = "hello from backend";

            // Send response back to Telegram
            await _botClient.SendMessage(chatId, llmResponse);

            return Ok();
        }

        private async Task<string> GetLlmResponse(string message)
        {
            var client = _httpClientFactory.CreateClient();
            var llmUrl = _configuration["LlmApi:Url"];
            var requestBody = new { message }; // Adjust based on your LLM API requirements

            var response = await client.PostAsJsonAsync(llmUrl, requestBody);
            response.EnsureSuccessStatusCode();

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