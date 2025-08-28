using MosDef.Core.Models;

namespace MosDef.Core.Services;

/// <summary>
/// Manages MOS-DEF configuration file operations.
/// Handles loading, saving, and managing the %AppData%\MOS-DEF\config.json file.
/// </summary>
public class ConfigManager
{
    private readonly string _configDirectory;
    private readonly string _configFilePath;

    private MosDefConfig? _cachedConfig;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(30); // Reload config if older than 30 seconds

    /// <summary>
    /// Gets the path to the configuration file.
    /// </summary>
    public string ConfigPath => _configFilePath;

    /// <summary>
    /// Gets the path to the configuration directory.
    /// </summary>
    public string ConfigDirectoryPath => _configDirectory;

    /// <summary>
    /// Loads the configuration from disk or returns a cached copy if recently loaded.
    /// </summary>
    /// <returns>MosDefConfig instance (never null)</returns>
    /// <exception cref="InvalidOperationException">Thrown when config directory cannot be created</exception>
    public MosDefConfig LoadConfig()
    {
        // Return cached config if still fresh
        if (_cachedConfig != null && DateTime.UtcNow - _lastLoadTime < _cacheTimeout)
        {
            return _cachedConfig;
        }

        try
        {
            // Ensure config directory exists
            EnsureConfigDirectoryExists();

            // Load from file if it exists
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                _cachedConfig = MosDefConfig.FromJson(json);
            }
            else
            {
                _cachedConfig = new MosDefConfig();
            }

            _lastLoadTime = DateTime.UtcNow;
            return _cachedConfig;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is DirectoryNotFoundException)
        {
            // If we can't read the config file, return a default config
            // Log the error but don't fail the application
            Console.WriteLine($"Warning: Could not load config file '{_configFilePath}': {ex.Message}");
            _cachedConfig = new MosDefConfig();
            _lastLoadTime = DateTime.UtcNow;
            return _cachedConfig;
        }
    }

    /// <summary>
    /// Saves the configuration to disk and updates the cache.
    /// </summary>
    /// <param name="config">Configuration to save</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when the config cannot be saved</exception>
    public void SaveConfig(MosDefConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            // Ensure config directory exists
            EnsureConfigDirectoryExists();

            // Validate config before saving
            config.Validate();

            // Write to file
            var json = config.ToJson();
            File.WriteAllText(_configFilePath, json);

            // Update cache
            _cachedConfig = config.Clone();
            _lastLoadTime = DateTime.UtcNow;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is DirectoryNotFoundException)
        {
            throw new InvalidOperationException($"Could not save config file '{_configFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates the configuration using a modification function and saves it.
    /// </summary>
    /// <param name="updateAction">Function to modify the configuration</param>
    /// <exception cref="ArgumentNullException">Thrown when updateAction is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when the config cannot be saved</exception>
    public void UpdateConfig(Action<MosDefConfig> updateAction)
    {
        if (updateAction == null)
            throw new ArgumentNullException(nameof(updateAction));

        var config = LoadConfig();
        updateAction(config);
        SaveConfig(config);
    }

    /// <summary>
    /// Gets the current default selector from configuration.
    /// </summary>
    /// <returns>Default selector string, or null if not set</returns>
    public string? GetDefaultSelector()
    {
        var config = LoadConfig();
        return config.DefaultSelector;
    }

    /// <summary>
    /// Sets the default selector in configuration.
    /// </summary>
    /// <param name="selector">Selector to set as default, or null to clear</param>
    /// <exception cref="InvalidOperationException">Thrown when the config cannot be saved</exception>
    public void SetDefaultSelector(string? selector)
    {
        UpdateConfig(config =>
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                config.ClearDefaultSelector();
            }
            else
            {
                config.SetDefaultSelector(selector);
            }
        });
    }

    /// <summary>
    /// Clears the default selector from configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the config cannot be saved</exception>
    public void ClearDefaultSelector()
    {
        SetDefaultSelector(null);
    }

    /// <summary>
    /// Adds a selector to the usage history.
    /// </summary>
    /// <param name="selector">Selector to add to history</param>
    /// <exception cref="InvalidOperationException">Thrown when the config cannot be saved</exception>
    public void AddSelectorToHistory(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return;

        UpdateConfig(config => config.AddToHistory(selector));
    }

    /// <summary>
    /// Updates the last action in configuration.
    /// </summary>
    /// <param name="action">Action name (landscape, portrait, toggle)</param>
    /// <exception cref="InvalidOperationException">Thrown when the config cannot be saved</exception>
    public void SetLastAction(string action)
    {
        UpdateConfig(config => config.SetLastAction(action));
    }

    /// <summary>
    /// Gets the selector history from configuration.
    /// </summary>
    /// <returns>List of recently used selectors</returns>
    public List<string> GetSelectorHistory()
    {
        var config = LoadConfig();
        return new List<string>(config.SelectorHistory);
    }

    /// <summary>
    /// Deletes the configuration file from disk.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the config file cannot be deleted</exception>
    public void DeleteConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }

            // Clear cache
            _cachedConfig = null;
            _lastLoadTime = DateTime.MinValue;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Could not delete config file '{_configFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if the configuration file exists.
    /// </summary>
    /// <returns>True if the config file exists</returns>
    public bool ConfigExists()
    {
        return File.Exists(_configFilePath);
    }

    /// <summary>
    /// Gets the size of the configuration file in bytes.
    /// </summary>
    /// <returns>File size in bytes, or 0 if file doesn't exist</returns>
    public long GetConfigFileSize()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var fileInfo = new FileInfo(_configFilePath);
                return fileInfo.Length;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the last modification time of the configuration file.
    /// </summary>
    /// <returns>Last write time, or DateTime.MinValue if file doesn't exist</returns>
    public DateTime GetConfigLastModified()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                return File.GetLastWriteTime(_configFilePath);
            }
            return DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Ensures the configuration directory exists, creating it if necessary.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the directory cannot be created</exception>
    private void EnsureConfigDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Could not create config directory '{_configDirectory}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a new ConfigManager instance using the default AppData location.
    /// </summary>
    public ConfigManager()
    {
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _configDirectory = Path.Combine(appDataFolder, "MOS-DEF");
        _configFilePath = Path.Combine(_configDirectory, "config.json");
    }

    /// <summary>
    /// Creates a ConfigManager instance for testing with a custom config path.
    /// </summary>
    /// <param name="configFilePath">Custom path to config file</param>
    internal ConfigManager(string configFilePath)
    {
        if (string.IsNullOrWhiteSpace(configFilePath))
            throw new ArgumentException("Config file path cannot be null or empty", nameof(configFilePath));

        _configFilePath = Path.GetFullPath(configFilePath);
        _configDirectory = Path.GetDirectoryName(_configFilePath) ?? throw new ArgumentException("Invalid config file path", nameof(configFilePath));
    }
}
