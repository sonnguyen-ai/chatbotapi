using chatminimalapi.DTOs;

namespace chatminimalapi.Data;

public static class DevelopmentSettings
{
    public static List<Setting> GetSampleSettings()
    {
        /*
        "max_token": "300",
        "model": "gemini-1.5-flash",
        "key": "AIzaSyDNPlChIuTRovT1BlHwwvTepVmgoJiDg1g",
        "baseUrl": "https://generativelanguage.googleapis.com",
        "url": "/v1beta/models/{0}:generateContent?key={1}",
        "cors": "seysolutions.com",
        */
        return new List<Setting>
        {
            new Setting
            {
                Id = "1",
                TenantId = "tenant1",
                Configuration = new Configuration
                {
                    Title = "Development Chat",
                    WelcomeMessage = "Welcome to Development Environment",
                    Max_token = "1000",
                    Model = "gemini-1.5-flash",
                    Key = "AIzaSyDNPlChIuTRovT1BlHwwvTepVmgoJiDg1g",
                    BaseUrl = "https://generativelanguage.googleapis.com",
                    Url = "/v1beta/models/{0}:generateContent?key={1}",
                    Cors = "*",
                    instruction = "You are in development mode",
                    Colors = new Colors
                    {
                        MainColor = "#007bff",
                        SecondColor = "#6c757d"
                    },
                    Prompt = "This is a development prompt"
                }
            },
            new Setting
            {
                Id = "2",
                TenantId = "seysolutions",
                Configuration = new Configuration
                {
                    Title = "Development Chat 2",
                    WelcomeMessage = "Welcome to Development Environment 2",
                    Max_token = "1000",
                    Model = "gemini-1.5-flash",
                    Key = "AIzaSyDNPlChIuTRovT1BlHwwvTepVmgoJiDg1g",
                    BaseUrl = "https://generativelanguage.googleapis.com",
                    Url = "/v1beta/models/{0}:generateContent?key={1}",
                    Cors = "*",
                    instruction = "You are in development mode - tenant 2",
                    Colors = new Colors
                    {
                        MainColor = "#28a745",
                        SecondColor = "#dc3545"
                    },
                    Prompt = "This is a development prompt for tenant 2"
                }
            }
        };
    }
}