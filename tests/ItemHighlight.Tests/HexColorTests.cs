using ItemHighlight;
using Xunit;

public class HexColorTests
{
    [Theory]
    [InlineData("#FFD91A", 1f, 0.85098f, 0.10196f, 1f)]    // "#" and 6 digits
    [InlineData("FFD91A", 1f, 0.85098f, 0.10196f, 1f)]     // "#" is optional
    [InlineData("#ff0000", 1f, 0f, 0f, 1f)]                // lowercase
    [InlineData("#00000000", 0f, 0f, 0f, 0f)]              // 8 digits, alpha 0
    [InlineData("#FFFFFF80", 1f, 1f, 1f, 0.50196f)]        // 8 digits, half alpha
    [InlineData("  #FF0000  ", 1f, 0f, 0f, 1f)]            // surrounding whitespace trimmed
    public void TryParseHexColor_parses_valid_hex(string s, float r, float g, float b, float a)
    {
        Assert.True(HighlightLogic.TryParseHexColor(s, out float pr, out float pg, out float pb, out float pa));
        Assert.Equal(r, pr, 4);
        Assert.Equal(g, pg, 4);
        Assert.Equal(b, pb, 4);
        Assert.Equal(a, pa, 4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]         // nothing left after the trim
    [InlineData("#")]           // no digits
    [InlineData("#FFF")]      // 3-digit shorthand not supported
    [InlineData("#FFD91")]      // 5 digits
    [InlineData("#FFD91AFF00")] // 10 digits
    [InlineData("#GGGGGG")]     // non-hex digit
    [InlineData("purple")]      // named color not supported
    public void TryParseHexColor_rejects_malformed(string s)
    {
        Assert.False(HighlightLogic.TryParseHexColor(s, out _, out _, out _, out _));
    }
}
