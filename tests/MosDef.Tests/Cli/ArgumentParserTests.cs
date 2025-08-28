using MosDef.Cli.Services;
using Xunit;
using System.Linq;
using System.Collections.Generic;

namespace MosDef.Tests.Cli;

/// <summary>
/// Unit tests for the ArgumentParser class.
/// Tests command line argument parsing and validation logic.
/// </summary>
public class ArgumentParserTests
{
    [Theory]
    [InlineData("landscape", "landscape", false, false, false)]
    [InlineData("portrait", "portrait", false, false, false)]
    [InlineData("toggle", "toggle", false, false, false)]
    [InlineData("LANDSCAPE", "landscape", false, false, false)]
    public void Parse_BasicActions_ParsesCorrectly(string action, string expectedAction, bool expectedList, bool expectedVerbose, bool expectedDryRun)
    {
        var options = ArgumentParser.Parse(new[] { action });

        Assert.Equal(expectedAction, options.Action);
        Assert.Equal(expectedList, options.ShowList);
        Assert.Equal(expectedVerbose, options.Verbose);
        Assert.Equal(expectedDryRun, options.DryRun);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData("--list", true)]
    [InlineData("-l", true)]
    [InlineData("--verbose", false)]
    [InlineData("-v", false)]
    public void Parse_ListFlags_ParsesCorrectly(string arg, bool expectedList)
    {
        var options = ArgumentParser.Parse(new[] { arg });

        Assert.Equal(expectedList, options.ShowList);
        if (expectedList)
        {
            Assert.True(options.IsSpecialCommand);
        }
    }

    [Theory]
    [InlineData("--verbose", null, true)]
    [InlineData("-v", null, true)]
    [InlineData("portrait", "--verbose", true)]
    [InlineData("landscape", null, false)]
    public void Parse_VerboseFlags_ParsesCorrectly(string arg1, string? arg2, bool expectedVerbose)
    {
        var args = arg2 is null ? new[] { arg1 } : new[] { arg1, arg2 };
        var options = ArgumentParser.Parse(args);
        Assert.Equal(expectedVerbose, options.Verbose);
    }

    [Theory]
    [InlineData("--dry-run", null, true)]
    [InlineData("-n", null, true)]
    [InlineData("toggle", "--dry-run", true)]
    [InlineData("portrait", null, false)]
    public void Parse_DryRunFlags_ParsesCorrectly(string arg1, string? arg2, bool expectedDryRun)
    {
        var args = arg2 is null ? new[] { arg1 } : new[] { arg1, arg2 };
        var options = ArgumentParser.Parse(args);
        Assert.Equal(expectedDryRun, options.DryRun);
    }

    [Theory]
    [InlineData("portrait", "--only", "M1", "M1")]
    [InlineData("landscape", "--only", "M1,M3", "M1,M3")]
    [InlineData("toggle", "--only=M2", null, "M2")]
    [InlineData("portrait", "--only=name:DELL,conn:HDMI", null, "name:DELL,conn:HDMI")]
    public void Parse_OnlySelectors_ParsesCorrectly(string arg1, string arg2, string? arg3, string expectedSelectorsCsv)
    {
        var argsList = new List<string> { arg1 };
        if (!string.IsNullOrEmpty(arg2)) argsList.Add(arg2!);
        if (!string.IsNullOrEmpty(arg3)) argsList.Add(arg3!);
        var args = argsList.ToArray();
        var options = ArgumentParser.Parse(args);

        var expectedSelectors = expectedSelectorsCsv.Split(',');
        Assert.Equal(expectedSelectors, options.OnlySelectors);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData("portrait", "--include", "M1", "M1")]
    [InlineData("landscape", "--include=M2,M3", null, "M2,M3")]
    public void Parse_IncludeSelectors_ParsesCorrectly(string arg1, string arg2, string? arg3, string expectedSelectorsCsv)
    {
        var argsList = new List<string> { arg1 };
        if (!string.IsNullOrEmpty(arg2)) argsList.Add(arg2!);
        if (!string.IsNullOrEmpty(arg3)) argsList.Add(arg3!);
        var args = argsList.ToArray();
        var options = ArgumentParser.Parse(args);

        var expectedSelectors = expectedSelectorsCsv.Split(',');
        Assert.Equal(expectedSelectors, options.IncludeSelectors);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData("portrait", "--exclude", "M1", "M1")]
    [InlineData("landscape", "--exclude=M2,M3", null, "M2,M3")]
    public void Parse_ExcludeSelectors_ParsesCorrectly(string arg1, string arg2, string? arg3, string expectedSelectorsCsv)
    {
        var argsList = new List<string> { arg1 };
        if (!string.IsNullOrEmpty(arg2)) argsList.Add(arg2!);
        if (!string.IsNullOrEmpty(arg3)) argsList.Add(arg3!);
        var args = argsList.ToArray();
        var options = ArgumentParser.Parse(args);

        var expectedSelectors = expectedSelectorsCsv.Split(',');
        Assert.Equal(expectedSelectors, options.ExcludeSelectors);
        Assert.True(options.IsValid);
    }

    [Theory]
    [InlineData("portrait", "--save-default", "M2", "M2")]
    [InlineData("toggle", "--save-default=name:DELL", null, "name:DELL")]
    public void Parse_SaveDefaultSelector_ParsesCorrectly(string arg1, string arg2, string? arg3, string expectedSelector)
    {
        var argsList = new List<string> { arg1 };
        if (!string.IsNullOrEmpty(arg2)) argsList.Add(arg2!);
        if (!string.IsNullOrEmpty(arg3)) argsList.Add(arg3!);
        var args = argsList.ToArray();
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
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void Parse_HelpFlags_ParsesCorrectly(string arg)
    {
        var options = ArgumentParser.Parse(new[] { arg });

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
    [InlineData("invalid_action", null)]
    [InlineData("landscape", "portrait")]
    public void Parse_InvalidActions_AddsValidationError(string arg1, string? arg2)
    {
        var argsList = new List<string> { arg1 };
        if (!string.IsNullOrEmpty(arg2)) argsList.Add(arg2!);
        var args = argsList.ToArray();
        var options = ArgumentParser.Parse(args);

        Assert.False(options.IsValid);
        Assert.NotEmpty(options.ValidationErrors);
    }

    [Theory]
    [InlineData("--only")]
    [InlineData("--include")]
    [InlineData("--exclude")]
    [InlineData("--save-default")]
    public void Parse_FlagsWithoutValues_AddsValidationError(string arg)
    {
        var options = ArgumentParser.Parse(new[] { arg });

        Assert.False(options.IsValid);
        Assert.NotEmpty(options.ValidationErrors);
    }

    [Theory]
    [InlineData("--only=")]
    [InlineData("--include=")]
    [InlineData("--exclude=")]
    [InlineData("--save-default=")]
    public void Parse_FlagsWithEmptyValues_AddsValidationError(string arg)
    {
        var options = ArgumentParser.Parse(new[] { arg });

        Assert.False(options.IsValid);
        Assert.NotEmpty(options.ValidationErrors);
    }

    [Theory]
    [InlineData("--unknown-flag", null)]
    [InlineData("-x", null)]
    [InlineData("portrait", "unknown_arg")]
    public void Parse_UnknownFlags_AddsValidationError(string arg1, string? arg2)
    {
        var argsList = new List<string> { arg1 };
        if (!string.IsNullOrEmpty(arg2)) argsList.Add(arg2!);
        var args = argsList.ToArray();
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
