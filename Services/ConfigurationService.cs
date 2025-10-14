using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FehlzeitApp.Services
{
    public class ConfigurationService
    {
        private AppConfiguration? _config;

        public ApiSettings ApiSettings => _config?.ApiSettings ?? GetDefaultConfiguration().ApiSettings;
        public AppSettings AppSettings => _config?.AppSettings ?? GetDefaultConfiguration().AppSettings;

        private ConfigurationService() { }

        public static async Task<ConfigurationService> CreateAsync()
        {
            var service = new ConfigurationService();
            await service.LoadConfigurationAsync();
            return service;
        }

        public static ConfigurationService CreateSync()
        {
            var service = new ConfigurationService();
            service.LoadConfigurationSync();
            return service;
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (File.Exists(configPath))
                {
                    string json = await File.ReadAllTextAsync(configPath);
                    _config = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (_config == null)
                    {
                        _config = GetDefaultConfiguration();
                    }
                }
                else
                {
                    _config = GetDefaultConfiguration();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                _config = GetDefaultConfiguration();
            }
        }

        private void LoadConfigurationSync()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    _config = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (_config == null)
                    {
                        _config = GetDefaultConfiguration();
                    }
                }
                else
                {
                    _config = GetDefaultConfiguration();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                _config = GetDefaultConfiguration();
            }
        }

        private AppConfiguration GetDefaultConfiguration()
        {
            return new AppConfiguration
            {
                ApiSettings = new ApiSettings
                {
                    BaseUrl = "http://localhost:5033/api",
                    Timeout = 30,
                    EnableLogging = true
                },
                AppSettings = new AppSettings
                {
                    ApplicationName = "Hama Fehlzeit",
                    Version = "1.0.0",
                    Theme = "Light",
                    Language = "de-DE"
                }
            };
        }
    }
    
    public class AppConfiguration
    {
        public ApiSettings ApiSettings { get; set; } = new();
        public AppSettings AppSettings { get; set; } = new();
    }
    
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int Timeout { get; set; } = 30;
        public bool EnableLogging { get; set; } = true;
    }
    
    public class AppSettings
    {
        public string ApplicationName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}
