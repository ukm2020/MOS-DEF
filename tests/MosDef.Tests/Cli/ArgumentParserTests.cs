using MosDef.Cli.Services;
using Xunit;

namespace MosDef.Tests.Cli;

/// <summary>
/// Unit tests for the ArgumentParser class.
/// Tests command line argument parsing and validation logic.
/// </summary>
public class ArgumentParserTests
{
    [Theory]
    [InlineData(new string[] { "landscape" }, "landscape", false, false, false)]
    [InlineData(new string[] { "portrait" }, "portrait", false, false, false)]
    [InlineData(new string[] { "toggle" }, "toggle", false, false, false)]
    [InlineData(new string[] { "LANDSCAPE" }, "landscape", false, false, false)]
    public void Parse_BasicActions_ParsesCorrectly(string[] args, string expectedAction, bool expectedList, bool expectedVerbose, bool expectedDryRun)
    {
        var options = ArgumentParser.Parse(args);

        Assert.Equal(expectedAction, options.Action);
        Assert.Equal(expectedList, options.ShowList);
        Assert.Equal(expectedVerbose, options.Verbose);
        Assert.Equal(expectedDryRun, options.DryRun);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData(new string[] { "--list" }, true)]
    [InlineData(new string[] { "-l" }, true)]
    [InlineData(new string[] { "--verbose" }, false)]
    [InlineData(new string[] { "-v" }, false)]
    public void Parse_ListFlags_ParsesCorrectly(string[] args, bool expectedList)
    {
        var options = ArgumentParser.Parse(args);

        Assert.Equal(expectedList, options.ShowList);
        if (expectedList)
        {
            Assert.True(options.IsSpecialCommand);
        }
    }

    [Theory]
    [InlineData(new string[] { "--verbose" }, true)]
    [InlineData(new string[] { "-v" }, true)]
    [InlineData(new string[] { "portrait", "--verbose" }, true)]
    [InlineData(new string[] { "landscape" }, false)]
    public void Parse_VerboseFlags_ParsesCorrectly(string[] args, bool expectedVerbose)
    {
        var options = ArgumentParser.Parse(args);
        Assert.Equal(expectedVerbose, options.Verbose);
    }

    [Theory]
    [InlineData(new string[] { "--dry-run" }, true)]
    [InlineData(new string[] { "-n" }, true)]
    [InlineData(new string[] { "toggle", "--dry-run" }, true)]
    [InlineData(new string[] { "portrait" }, false)]
    public void Parse_DryRunFlags_ParsesCorrectly(string[] args, bool expectedDryRun)
    {
        var options = ArgumentParser.Parse(args);
        Assert.Equal(expectedDryRun, options.DryRun);
    }

    [Theory]
    [InlineData(new string[] { "portrait", "--only", "M1" }, new string[] { "M1" })]
    [InlineData(new string[] { "landscape", "--only", "M1,M3" }, new string[] { "M1", "M3" })]
    [InlineData(new string[] { "toggle", "--only=M2" }, new string[] { "M2" })]
    [InlineData(new string[] { "portrait", "--only=name:DELL,conn:HDMI" }, new string[] { "name:DELL", "conn:HDMI" })]
    public void Parse_OnlySelectors_ParsesCorrectly(string[] args, string[] expectedSelectors)
    {
        var options = ArgumentParser.Parse(args);

        Assert.Equal(expectedSelectors, options.OnlySelectors);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData(new string[] { "portrait", "--include", "M1" }, new string[] { "M1" })]
    [InlineData(new string[] { "landscape", "--include=M2,M3" }, new string[] { "M2", "M3" })]
    public void Parse_IncludeSelectors_ParsesCorrectly(string[] args, string[] expectedSelectors)
    {
        var options = ArgumentParser.Parse(args);

        Assert.Equal(expectedSelectors, options.IncludeSelectors);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData(new string[] { "portrait", "--exclude", "M1" }, new string[] { "M1" })]
    [InlineData(new string[] { "landscape", "--exclude=M2,M3" }, new string[] { "M2", "M3" })]
    public void Parse_ExcludeSelectors_ParsesCorrectly(string[] args, string[] expectedSelectors)
    {
        var options = ArgumentParser.Parse(args);

        Assert.Equal(expectedSelectors, options.ExcludeSelectors);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData(new string[] { "portrait", "--save-default", "M2" }, "M2")]
    [InlineData(new string[] { "toggle", "--save-default=name:DELL" }, "name:DELL")]
    public void Parse_SaveDefaultSelector_ParsesCorrectly(string[] args, string expectedSelector)
    {
        var options = ArgumentParser.Parse(args);

        Assert.Equal(expectedSelector, options.SaveDefaultSelector);
        Assert.True(options.IsValid);
    }

    [Fact]
    public void Parse_ClearDefault_ParsesCorrectly()
    {
        var options = ArgumentParser.Parse(new string[] { "--clear-default" });

        Assert.True(options.ClearDefault);
        Assert.True(options.IsSpecialCommand);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData(new string[] { "--help" })]
    [InlineData(new string[] { "-h" })]
    [InlineData(new string[] { "/?" })]
    public void Parse_HelpFlags_ParsesCorrectly(string[] args)
    {
        var options = ArgumentParser.Parse(args);

        Assert.True(options.ShowHelp);
        Assert.True(options.IsSpecialCommand);
    }

    [Fact]
    public void Parse_VersionFlag_ParsesCorrectly()
    {
        var options = ArgumentParser.Parse(new string[] { "--version" });

        Assert.True(options.ShowVersion);
        Assert.True(options.IsSpecialCommand);
    }

    [Fact]
    public void Parse_EmptyArgs_ShowsHelp()
    {
        var options = ArgumentParser.Parse(new string[0]);

        Assert.True(options.ShowHelp);
        Assert.True(options.IsSpecialCommand);
    }

    [Fact]
    public void Parse_NullArgs_ShowsHelp()
    {
        var options = ArgumentParser.Parse(null);

        Assert.True(options.ShowHelp);
        Assert.True(options.IsSpecialCommand);
    }

    [Theory]
    [InlineData(new string[] { "invalid_action" })]
    [InlineData(new string[] { "landscape", "portrait" })]
    public void Parse_InvalidActions_AddsValidationError(string[] args)
    {
        var options = ArgumentParser.Parse(args);

        Assert.False(options.IsValid);
        Assert.NotEmpty(options.ValidationErrors);
    }

    [Theory]
    [InlineData(new string[] { "--only" })]
    [InlineData(new string[] { "--include" })]
    [InlineData(new string[] { "--exclude" })]
    [InlineData(new string[] { "--save-default" })]
    public void Parse_FlagsWithoutValues_AddsValidationError(string[] args)
    {
        var options = ArgumentParser.Parse(args);

        Assert.False(options.IsValid);
        Assert.NotEmpty(options.ValidationErrors);
    }

    [Theory]
    [InlineData(new string[] { "--only=" })]
    [InlineData(new string[] { "--include=" })]
    [InlineData(new string[] { "--exclude=" })]
    [InlineData(new string[] { "--save-default=" })]
    public void Parse_FlagsWithEmptyValues_AddsValidationError(string[] args)
    {
        var options = ArgumentParser.Parse(args);

        Assert.False(options.IsValid);
        Assert.NotEmpty(options.ValidationErrors);
    }

    [Theory]
    [InlineData(new string[] { "--unknown-flag" })]
    [InlineData(new string[] { "-x" })]
    [InlineData(new string[] { "portrait", "unknown_arg" })]
    public void Parse_UnknownFlags_AddsValidationError(string[] args)
    {
        var options = ArgumentParser.Parse(args);

        Assert.False(options.IsValid);
        Assert.NotEmpty(options.ValidationErrors);
    }

    [Fact]
    public void Parse_OnlyAndIncludeTogether_AddsValidationError()
    {
        var options = ArgumentParser.Parse(new string[] { "portrait", "--only", "M1", "--include", "M2" });

        Assert.False(options.IsValid);
        Assert.Contains(options.ValidationErrors, e => e.Contains("--only") && e.Contains("--include"));
    }

    [Fact]
    public void Parse_OnlyAndExcludeTogether_AddsValidationError()
    {
        var options = ArgumentParser.Parse(new string[] { "landscape", "--only", "M1", "--exclude", "M2" });

        Assert.False(options.IsValid);
        Assert.Contains(options.ValidationErrors, e => e.Contains("--only") && e.Contains("--exclude"));
    }

    [Fact]
    public void Parse_ClearDefaultWithAction_AddsValidationError()
    {
        var options = ArgumentParser.Parse(new string[] { "portrait", "--clear-default" });

        Assert.False(options.IsValid);
        Assert.Contains(options.ValidationErrors, e => e.Contains("--clear-default"));
    }

    [Fact]
    public void Parse_DryRunWithSpecialCommand_AddsValidationError()
    {
        var options = ArgumentParser.Parse(new string[] { "--list", "--dry-run" });

        Assert.False(options.IsValid);
        Assert.Contains(options.ValidationErrors, e => e.Contains("--dry-run"));
    }

    [Fact]
    public void Parse_ComplexValidCommand_ParsesCorrectly()
    {
        var args = new string[] { "toggle", "--include", "M1,M2", "--exclude", "M3", "--verbose", "--save-default", "M1" };
        var options = ArgumentParser.Parse(args);

        Assert.Equal("toggle", options.Action);
        Assert.Equal(new string[] { "M1", "M2" }, options.IncludeSelectors);
        Assert.Equal(new string[] { "M3" }, options.ExcludeSelectors);
        Assert.True(options.Verbose);
        Assert.Equal("M1", options.SaveDefaultSelector);
        Assert.True(options.IsValid);
    }

    [Fact]
    public void GetHelpText_ReturnsNonEmptyString()
    {
        var helpText = ArgumentParser.GetHelpText();
        Assert.False(string.IsNullOrWhiteSpace(helpText));
        Assert.Contains("MOS-DEF", helpText);
        Assert.Contains("landscape", helpText);
        Assert.Contains("portrait", helpText);
        Assert.Contains("toggle", helpText);
    }

    [Fact]
    public void GetVersionText_ReturnsVersionString()
    {
        var versionText = ArgumentParser.GetVersionText();
        Assert.False(string.IsNullOrWhiteSpace(versionText));
        Assert.Contains("MOS-DEF", versionText);
        Assert.Contains("v", versionText);
    }
}
