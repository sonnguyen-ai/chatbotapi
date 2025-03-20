using System.Threading.Tasks;

namespace TelegramBotBackend.Services
{
    public interface ILlmService
    {
        Task<string> GetResponseAsync(string message, long chatId, string tenantId);
    }
}