using MosDef.Core.Models;
using MosDef.Core.Services;
using Xunit;

namespace MosDef.Tests.Core;

/// <summary>
/// Unit tests for the ConfigManager class.
/// Tests configuration file operations and persistence logic.
/// </summary>
public class ConfigManagerTests : IDisposable
{
    private readonly string _testConfigPath;
    private readonly string _testConfigDir;
    private readonly ConfigManager _configManager;

    public ConfigManagerTests()
    {
        // Create a temporary directory for test config files
        _testConfigDir = Path.Combine(Path.GetTempPath(), "MosDefTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testConfigDir);
        _testConfigPath = Path.Combine(_testConfigDir, "config.json");

        // Create config manager with test path (using internal constructor)
        _configManager = new ConfigManager(_testConfigPath);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testConfigDir))
        {
            Directory.Delete(_testConfigDir, recursive: true);
        }
    }

    [Fact]
    public void LoadConfig_NoConfigFile_ReturnsDefaultConfig()
    {
        var config = _configManager.LoadConfig();

        Assert.NotNull(config);
        Assert.Null(config.DefaultSelector);
        Assert.Null(config.LastAction);
        Assert.Empty(config.SelectorHistory);
    }

    [Fact]
    public void SaveConfig_ValidConfig_WritesFileAndUpdatesCache()
    {
        var config = new MosDefConfig
        {
            DefaultSelector = "M2",
            LastAction = "portrait",
            SelectorHistory = new List<string> { "M1", "M2" }
        };

        _configManager.SaveConfig(config);

        Assert.True(File.Exists(_testConfigPath));
        
        // Verify file contents
        var json = File.ReadAllText(_testConfigPath);
        Assert.Contains("M2", json);
        Assert.Contains("portrait", json);
    }

    [Fact]
    public void LoadConfig_ExistingFile_ReturnsConfigFromFile()
    {
        // Create a config file
        var originalConfig = new MosDefConfig
        {
            DefaultSelector = "M3",
            LastAction = "landscape",
            SelectorHistory = new List<string> { "M3", "name:DELL" }
        };
        _configManager.SaveConfig(originalConfig);

        // Create a new config manager to ensure we're reading from file
        var newConfigManager = new ConfigManager(_testConfigPath);
        var loadedConfig = newConfigManager.LoadConfig();

        Assert.Equal("M3", loadedConfig.DefaultSelector);
        Assert.Equal("landscape", loadedConfig.LastAction);
        Assert.Equal(2, loadedConfig.SelectorHistory.Count);
        Assert.Contains("M3", loadedConfig.SelectorHistory);
        Assert.Contains("name:DELL", loadedConfig.SelectorHistory);
    }

    [Fact]
    public void UpdateConfig_ModificationAction_UpdatesAndSaves()
    {
        _configManager.UpdateConfig(config =>
        {
            config.DefaultSelector = "M1";
            config.SetLastAction("toggle");
        });

        var loadedConfig = _configManager.LoadConfig();
        Assert.Equal("M1", loadedConfig.DefaultSelector);
        Assert.Equal("toggle", loadedConfig.LastAction);
    }

    [Fact]
    public void SetDefaultSelector_ValidSelector_UpdatesConfig()
    {
        _configManager.SetDefaultSelector("M2");

        var config = _configManager.LoadConfig();
        Assert.Equal("M2", config.DefaultSelector);
        Assert.Contains("M2", config.SelectorHistory);
    }

    [Fact]
    public void ClearDefaultSelector_ExistingDefault_ClearsValue()
    {
        _configManager.SetDefaultSelector("M2");
        _configManager.ClearDefaultSelector();

        var config = _configManager.LoadConfig();
        Assert.Null(config.DefaultSelector);
    }

    [Fact]
    public void AddSelectorToHistory_NewSelector_AddsToFront()
    {
        _configManager.AddSelectorToHistory("M1");
        _configManager.AddSelectorToHistory("M2");
        _configManager.AddSelectorToHistory("M3");

        var history = _configManager.GetSelectorHistory();
        Assert.Equal(3, history.Count);
        Assert.Equal("M3", history[0]); // Most recent first
        Assert.Equal("M2", history[1]);
        Assert.Equal("M1", history[2]);
    }

    [Fact]
    public void AddSelectorToHistory_ExistingSelector_MovesToFront()
    {
        _configManager.AddSelectorToHistory("M1");
        _configManager.AddSelectorToHistory("M2");
        _configManager.AddSelectorToHistory("M1"); // Add M1 again

        var history = _configManager.GetSelectorHistory();
        Assert.Equal(2, history.Count);
        Assert.Equal("M1", history[0]); // M1 moved to front
        Assert.Equal("M2", history[1]);
    }

    [Fact]
    public void SetLastAction_ValidAction_UpdatesConfig()
    {
        _configManager.SetLastAction("portrait");

        var config = _configManager.LoadConfig();
        Assert.Equal("portrait", config.LastAction);
    }

    [Fact]
    public void ConfigExists_NoFile_ReturnsFalse()
    {
        Assert.False(_configManager.ConfigExists());
    }

    [Fact]
    public void ConfigExists_FileExists_ReturnsTrue()
    {
        _configManager.SaveConfig(new MosDefConfig());
        Assert.True(_configManager.ConfigExists());
    }

    [Fact]
    public void DeleteConfig_ExistingFile_RemovesFile()
    {
        _configManager.SaveConfig(new MosDefConfig());
        Assert.True(_configManager.ConfigExists());

        _configManager.DeleteConfig();
        Assert.False(_configManager.ConfigExists());
    }

    [Fact]
    public void GetConfigFileSize_ExistingFile_ReturnsSize()
    {
        _configManager.SaveConfig(new MosDefConfig { DefaultSelector = "M1" });
        var size = _configManager.GetConfigFileSize();
        Assert.True(size > 0);
    }

    [Fact]
    public void GetConfigLastModified_ExistingFile_ReturnsTime()
    {
        var beforeSave = DateTime.Now;
        _configManager.SaveConfig(new MosDefConfig());
        var lastModified = _configManager.GetConfigLastModified();
        
        Assert.True(lastModified >= beforeSave.AddSeconds(-1)); // Allow for some timing variance
        Assert.True(lastModified <= DateTime.Now);
    }

    [Fact]
    public void LoadConfig_CorruptedJson_ReturnsDefaultConfig()
    {
        // Write invalid JSON to config file
        File.WriteAllText(_testConfigPath, "{ invalid json }");

        var config = _configManager.LoadConfig();
        
        Assert.NotNull(config);
        Assert.Null(config.DefaultSelector);
        Assert.Empty(config.SelectorHistory);
    }

    [Fact]
    public void SaveConfig_NullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _configManager.SaveConfig(null!));
    }

    [Fact]
    public void UpdateConfig_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _configManager.UpdateConfig(null!));
    }
}
