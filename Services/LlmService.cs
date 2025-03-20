using chatminimalapi.DTOs;
using Newtonsoft.Json;

namespace TelegramBotBackend.Services
{
    public class LlmService : ILlmService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SettingProvider _provider;

        public LlmService(IHttpClientFactory httpClientFactory, SettingProvider provider)
        {
            _httpClientFactory = httpClientFactory;
            _provider = provider;
        }

        public async Task<string> GetResponseAsync(string message, long chatId, string tenantId)
        {
            var setting = _provider.settings.Find(s =>
                string.Equals(s.TenantId, tenantId, StringComparison.OrdinalIgnoreCase));

            if (setting == null)
                throw new ArgumentException("Setting not found for tenant" + tenantId, nameof(tenantId));

            var baseUrl = setting.Configuration.BaseUrl;
            var url = setting.Configuration.Url;
            var model = setting.Configuration.Model;
            var key = setting.Configuration.Key;
            var prompt = setting.Configuration.Prompt;
            var instruction = setting.Configuration.instruction;
            var client = _httpClientFactory.CreateClient();
            var maxToken = setting.Configuration.Max_token;

            //TODO: should use memcache , and service to store message history
            ChatHistory.AddMessage(chatId, new ChatMessage
            {
                Content = message,
                Timestamp = DateTime.Now,
                Role = "user"
            });

            var history = ChatHistory.GetHistory(chatId);
            var llmRequest = CreateLlmRequest(instruction, prompt, history, maxToken);

            var stringObj = JsonConvert.SerializeObject(llmRequest, new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            });

            var requestContent = new StringContent(stringObj, System.Text.Encoding.UTF8, "application/json");
            var postUrl = $"{baseUrl}{url}".Replace("{0}", model).Replace("{1}", key);

            var response = await client.PostAsync(postUrl, requestContent);
            if (!response.IsSuccessStatusCode)
                return "Error getting LLM response";

            var llmApiResponses = JsonConvert.DeserializeObject<LlmApiResponse>(
                await response.Content.ReadAsStringAsync());

            return llmApiResponses.Candidates[0].Content.Parts[0].Text;
        }

        private LlmRequest CreateLlmRequest(string instruction, string prompt, List<ChatMessage> history, string maxToken)
        {
            var request = new LlmRequest
            {
                SystemInstruction = new SystemInstruction()
                {
                    Parts = new List<Part> { new Part { Text = instruction }, new Part { Text = prompt } },
                },
                Contents = new List<Content>(),
                SafetySettings = new List<SafetySetting>
                {
                    new SafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new SafetySetting { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new SafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                    new SafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_MEDIUM_AND_ABOVE" }
                },
                GenerationConfig = new GenerationConfig
                {
                    MaxOutputTokens = int.Parse(maxToken),
                    Temperature = 0.7,
                    TopK = 1,
                    TopP = 0.8
                }
            };

            history.ForEach(x =>
            {
                request.Contents.Add(new Content
                {
                    Parts = new List<Part> { new Part { Text = x.Content } },
                    Role = x.Role
                });
            });

            return request;
        }
    }
}