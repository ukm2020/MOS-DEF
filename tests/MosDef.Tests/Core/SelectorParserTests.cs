using MosDef.Core.Models;
using MosDef.Core.Services;
using Xunit;

namespace MosDef.Tests.Core;

/// <summary>
/// Unit tests for the SelectorParser class.
/// Tests selector parsing, validation, and matching logic.
/// </summary>
public class SelectorParserTests
{
    private readonly List<DisplayInfo> _testDisplays;

    public SelectorParserTests()
    {
        // Create test displays for selector testing
        _testDisplays = new List<DisplayInfo>
        {
            new DisplayInfo
            {
                Id = "M1",
                Name = "DELL U2720Q",
                ConnectionType = "DISPLAYPORT",
                Resolution = "3840x2160",
                RotationDegrees = 0,
                PathKey = "7a1c-2f9e"
            },
            new DisplayInfo
            {
                Id = "M2",
                Name = "LG ULTRAFINE",
                ConnectionType = "HDMI",
                Resolution = "3840x2160",
                RotationDegrees = 90,
                PathKey = "4f2a-9c7e"
            },
            new DisplayInfo
            {
                Id = "M3",
                Name = "ASUS PA279CV",
                ConnectionType = "DISPLAYPORT",
                Resolution = "3840x2160",
                RotationDegrees = 0,
                PathKey = "c3b1-884d"
            }
        };
    }

    [Theory]
    [InlineData("M1,M3", new[] { "M1", "M3" })]
    [InlineData("M1, M2, M3", new[] { "M1", "M2", "M3" })]
    [InlineData("name:DELL,conn:HDMI", new[] { "name:DELL", "conn:HDMI" })]
    [InlineData("path:7a1c-2f9e", new[] { "path:7a1c-2f9e" })]
    [InlineData("", new string[0])]
    [InlineData("   ", new string[0])]
    public void ParseSelectorList_ValidInput_ReturnsExpectedSelectors(string input, string[] expected)
    {
        var result = SelectorParser.ParseSelectorList(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("M1", true)]
    [InlineData("m1", true)] // Case insensitive
    [InlineData("M2", false)]
    [InlineData("name:\"DELL U2720Q\"", true)]
    [InlineData("name:DELL", true)]
    [InlineData("name:ASUS", false)]
    [InlineData("conn:DISPLAYPORT", true)]
    [InlineData("conn:HDMI", false)]
    [InlineData("path:7a1c-2f9e", true)]
    [InlineData("path:4f2a-9c7e", false)]
    public void DisplayInfo_MatchesSelector_ReturnsExpectedResult(string selector, bool expected)
    {
        var display = _testDisplays[0]; // M1 DELL U2720Q DISPLAYPORT
        var result = display.MatchesSelector(selector);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("name:/DELL.*/", true)]
    [InlineData("name:/LG.*/", false)]
    [InlineData("name:/.*U2720.*/", true)]
    [InlineData("name:/^DELL/", true)]
    [InlineData("name:/ASUS$/", false)]
    public void DisplayInfo_MatchesSelector_RegexPattern_ReturnsExpectedResult(string selector, bool expected)
    {
        var display = _testDisplays[0]; // M1 DELL U2720Q DISPLAYPORT
        var result = display.MatchesSelector(selector);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ApplySelectors_OnlyFlag_ReturnsMatchingDisplays()
    {
        var result = SelectorParser.ApplySelectors(
            _testDisplays,
            onlySelectors: new List<string> { "M1", "M3" });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Id == "M1");
        Assert.Contains(result, d => d.Id == "M3");
        Assert.DoesNotContain(result, d => d.Id == "M2");
    }

    [Fact]
    public void ApplySelectors_IncludeAndExcludeFlags_ReturnsCorrectDisplays()
    {
        var result = SelectorParser.ApplySelectors(
            _testDisplays,
            includeSelectors: new List<string> { "conn:DISPLAYPORT" },
            excludeSelectors: new List<string> { "M3" });

        Assert.Single(result);
        Assert.Contains(result, d => d.Id == "M1");
        Assert.DoesNotContain(result, d => d.Id == "M2");
        Assert.DoesNotContain(result, d => d.Id == "M3");
    }

    [Fact]
    public void ApplySelectors_DefaultSelector_UsedWhenNoOtherFlags()
    {
        var result = SelectorParser.ApplySelectors(
            _testDisplays,
            defaultSelector: "M2");

        Assert.Single(result);
        Assert.Contains(result, d => d.Id == "M2");
    }

    [Fact]
    public void ApplySelectors_NoSelectors_ReturnsAllDisplays()
    {
        var result = SelectorParser.ApplySelectors(_testDisplays);

        Assert.Equal(3, result.Count);
        Assert.Equal(_testDisplays.Count, result.Count);
    }

    [Theory]
    [InlineData("M1", true, "")]
    [InlineData("M0", false, "M# selector requires a positive number")]
    [InlineData("M", false, "M# selector requires a number")]
    [InlineData("name:DELL", true, "")]
    [InlineData("name:", false, "name: selector requires a value")]
    [InlineData("conn:HDMI", true, "")]
    [InlineData("conn:INVALID", false, "conn: selector 'INVALID' is not a recognized connection type")]
    [InlineData("path:7a1c-2f9e", true, "")]
    [InlineData("path:", false, "path: selector requires a value")]
    [InlineData("invalid", false, "Unknown selector format")]
    public void ValidateSelector_VariousInputs_ReturnsExpectedResults(string selector, bool expectedValid, string expectedErrorContains)
    {
        var (isValid, errorMessage) = SelectorParser.ValidateSelector(selector);
        
        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.Contains(expectedErrorContains, errorMessage);
        }
    }

    [Fact]
    public void ValidateSelectors_ListWithErrors_ReturnsAllErrors()
    {
        var selectors = new List<string> { "M1", "invalid", "name:", "M0" };
        var errors = SelectorParser.ValidateSelectors(selectors);

        Assert.True(errors.Count >= 3); // Should have at least 3 errors
        Assert.Contains(errors, e => e.Contains("invalid"));
        Assert.Contains(errors, e => e.Contains("name:"));
        Assert.Contains(errors, e => e.Contains("M0"));
    }

    [Fact]
    public void ApplyPrecedenceRules_PathSelectorHasHighestPrecedence()
    {
        var displays = new List<DisplayInfo> { _testDisplays[0] }; // M1
        var selectors = new List<string> { "name:DELL", "M1", "path:7a1c-2f9e" };

        var result = SelectorParser.ApplyPrecedenceRules(displays, selectors);

        Assert.Single(result);
        Assert.Equal("M1", result[0].Id);
    }

    [Fact]
    public void SuggestSelectors_NoMatches_ProvidesSuggestions()
    {
        var suggestions = SelectorParser.SuggestSelectors(_testDisplays, new List<string> { "invalid" });

        Assert.Contains("Available displays", suggestions);
        Assert.Contains("M1", suggestions);
        Assert.Contains("DELL U2720Q", suggestions);
        Assert.Contains("path:7a1c-2f9e", suggestions);
        Assert.Contains("invalid", suggestions);
    }

    [Theory]
    [InlineData(0, 90)] // landscape → portrait
    [InlineData(90, 0)] // portrait → landscape
    [InlineData(180, 90)] // upside down landscape → portrait
    [InlineData(270, 0)] // upside down portrait → landscape
    [InlineData(45, 90)] // unknown → portrait
    public void DisplayInfo_GetNextToggleRotation_ReturnsExpectedRotation(int currentRotation, int expectedNext)
    {
        var display = new DisplayInfo { RotationDegrees = currentRotation };
        var result = display.GetNextToggleRotation();
        Assert.Equal(expectedNext, result);
    }
}
