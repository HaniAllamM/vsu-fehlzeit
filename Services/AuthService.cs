using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FehlzeitApp.Models;
using FehlzeitApp.Helpers;

namespace FehlzeitApp.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ConfigurationService _configService;
        private readonly string _baseUrl;

        public User? CurrentUser { get; private set; }
        public string? Token { get; private set; }
        public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(Token);

        public AuthService(ConfigurationService configService)
        {
            _httpClient = new HttpClient();
            _configService = configService;
            _baseUrl = _configService.ApiSettings.BaseUrl;
            _httpClient.Timeout = TimeSpan.FromSeconds(2); // Very short timeout for immediate feedback
        }

        public static async Task<AuthService> CreateAsync()
        {
            var configService = await ConfigurationService.CreateAsync();
            return new AuthService(configService);
        }

        public static AuthService CreateSync()
        {
            var configService = ConfigurationService.CreateSync();
            return new AuthService(configService);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var webApiResponse = JsonSerializer.Deserialize<WebApiLoginResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (webApiResponse != null && !string.IsNullOrEmpty(webApiResponse.Token) && webApiResponse.User != null)
                    {
                        CurrentUser = new User
                        {
                            UserId = webApiResponse.User.Id,
                            Username = webApiResponse.User.Username,
                            Role = webApiResponse.User.IsAdmin ? "Admin" : "User",
                        };
                        Token = webApiResponse.Token;
                        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
                        return new LoginResponse { Success = true, Token = Token, User = CurrentUser };
                    }
                    return new LoginResponse { Success = false, Message = "Invalid response format" };
                }
                return new LoginResponse { Success = false, Message = $"Login failed: {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                return new LoginResponse { Success = false, Message = $"Connection error: {ex.Message}" };
            }
        }

        public void Logout()
        {
            CurrentUser = null;
            Token = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public async Task<bool> LoginOfflineAsync(string username, string password)
        {
            // Offline login is disabled.
            await Task.CompletedTask;
            return false;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
