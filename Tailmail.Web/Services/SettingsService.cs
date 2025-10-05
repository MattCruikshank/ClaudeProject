using System.Text.Json;
using Tailmail.Protos;
using Google.Protobuf;

namespace Tailmail.Web.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private Settings _settings;
    public event Action? OnSettingsChanged;

    public SettingsService()
    {
        _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tailmail", "settings.json");
        _settings = new Settings();
        LoadSettings();
    }

    public Settings GetSettings()
    {
        return _settings;
    }

    public void SaveSettings(Settings settings)
    {
        _settings = settings;

        var directory = Path.GetDirectoryName(_settingsPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonFormatter.Default.Format(_settings);
        File.WriteAllText(_settingsPath, json);

        OnSettingsChanged?.Invoke();
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonParser.Default.Parse<Settings>(json);
            }
            catch
            {
                _settings = new Settings();
            }
        }
        else
        {
            _settings = new Settings();
        }
    }
}
