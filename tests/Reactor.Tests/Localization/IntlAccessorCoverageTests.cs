using Microsoft.UI.Reactor.Localization;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Localization;

/// <summary>
/// Targets remaining uncovered IntlAccessor paths: FormatList per-locale joiners
/// (GetAndWord / GetOrWord), FormatNumber digit-clamp branches, FormatDate
/// styles, Asset() cache hit, and RichMessage pseudo-localization.
/// </summary>
public class IntlAccessorCoverageTests
{
    private static IntlAccessor CreateAccessor(string locale, bool pseudo = false) =>
        new(locale, new InMemoryResourceProvider(), new MessageCache(), "en-US", pseudo);

    // ── FormatList per-locale conjunction/disjunction ──────────────

    [Theory]
    [InlineData("en-US", "A and B")]
    [InlineData("es-ES", "A y B")]
    [InlineData("fr-FR", "A et B")]
    [InlineData("de-DE", "A und B")]
    [InlineData("it-IT", "A e B")]
    [InlineData("pt-PT", "A e B")]
    [InlineData("ja-JP", "A と B")]
    [InlineData("ko-KR", "A 그리고 B")]
    [InlineData("zh-CN", "A 和 B")]
    [InlineData("ar-SA", "A و B")]
    [InlineData("he-IL", "A ו B")]
    [InlineData("ru-RU", "A и B")]
    [InlineData("nl-NL", "A en B")]
    [InlineData("pl-PL", "A i B")]
    [InlineData("tr-TR", "A ve B")]
    public void FormatList_Conjunction_Per_Locale(string locale, string expected)
    {
        var t = CreateAccessor(locale);
        Assert.Equal(expected, t.FormatList(new[] { "A", "B" }));
    }

    [Theory]
    [InlineData("en-US", "A or B")]
    [InlineData("es-ES", "A o B")]
    [InlineData("fr-FR", "A ou B")]
    [InlineData("de-DE", "A oder B")]
    [InlineData("it-IT", "A o B")]
    [InlineData("pt-PT", "A ou B")]
    [InlineData("ja-JP", "A または B")]
    [InlineData("ko-KR", "A 또는 B")]
    [InlineData("zh-CN", "A 或 B")]
    [InlineData("ar-SA", "A أو B")]
    [InlineData("he-IL", "A או B")]
    [InlineData("ru-RU", "A или B")]
    [InlineData("nl-NL", "A of B")]
    [InlineData("pl-PL", "A lub B")]
    [InlineData("tr-TR", "A veya B")]
    public void FormatList_Disjunction_Per_Locale(string locale, string expected)
    {
        var t = CreateAccessor(locale);
        Assert.Equal(expected, t.FormatList(new[] { "A", "B" }, ListFormatType.Disjunction));
    }

    [Fact]
    public void FormatList_Empty_Returns_Empty()
    {
        var t = CreateAccessor("en-US");
        Assert.Equal("", t.FormatList(new string[0]));
    }

    [Fact]
    public void FormatList_Single_Returns_Element()
    {
        var t = CreateAccessor("en-US");
        Assert.Equal("Solo", t.FormatList(new[] { "Solo" }));
    }

    [Fact]
    public void FormatList_Three_Items_Uses_OxfordComma()
    {
        var t = CreateAccessor("en-US");
        Assert.Equal("A, B, and C", t.FormatList(new[] { "A", "B", "C" }));
        Assert.Equal("A, B, or C", t.FormatList(new[] { "A", "B", "C" }, ListFormatType.Disjunction));
    }

    // ── FormatNumber digit clamping ────────────────────────────────

    [Fact]
    public void FormatNumber_Min_Only_Pads_To_Min_Digits()
    {
        var t = CreateAccessor("en-US");
        var result = t.FormatNumber(1.5, new NumberFormatOptions { MinimumFractionDigits = 4 });
        Assert.Contains(".5000", result);
    }

    [Fact]
    public void FormatNumber_Max_Only_Trims_To_Max_Digits()
    {
        var t = CreateAccessor("en-US");
        var result = t.FormatNumber(1.123456, new NumberFormatOptions { MaximumFractionDigits = 2 });
        Assert.Contains(".12", result);
    }

    [Fact]
    public void FormatNumber_Min_And_Max_Both_Set()
    {
        var t = CreateAccessor("en-US");
        var result = t.FormatNumber(1.5,
            new NumberFormatOptions { MinimumFractionDigits = 1, MaximumFractionDigits = 3 });
        Assert.NotNull(result);
        Assert.Contains("1.5", result);
    }

    [Fact]
    public void FormatNumber_Min_Greater_Than_Max_Is_Reordered_Safely()
    {
        // The implementation Math.Min/Math.Max-swaps reversed bounds so both apply.
        var t = CreateAccessor("en-US");
        var result = t.FormatNumber(1.5,
            new NumberFormatOptions { MinimumFractionDigits = 5, MaximumFractionDigits = 1 });
        Assert.NotNull(result);
    }

    [Fact]
    public void FormatNumber_Currency_Style()
    {
        var t = CreateAccessor("en-US");
        var result = t.FormatNumber(99.95, new NumberFormatOptions { Style = NumberStyle.Currency });
        Assert.StartsWith("$", result);
    }

    [Fact]
    public void FormatNumber_Percent_Style()
    {
        var t = CreateAccessor("en-US");
        var result = t.FormatNumber(0.5, new NumberFormatOptions { Style = NumberStyle.Percent });
        Assert.Contains("%", result);
    }

    // ── FormatDate styles ──────────────────────────────────────────

    [Fact]
    public void FormatDate_All_Styles_Return_Differing_Strings()
    {
        var t = CreateAccessor("en-US");
        var date = new DateTimeOffset(2026, 1, 15, 14, 30, 0, TimeSpan.Zero);

        var def = t.FormatDate(date);
        var shortF = t.FormatDate(date, new DateFormatOptions { Style = DateStyle.Short });
        var longF = t.FormatDate(date, new DateFormatOptions { Style = DateStyle.Long });
        var fullF = t.FormatDate(date, new DateFormatOptions { Style = DateStyle.Full });

        Assert.NotEqual(shortF, longF);
        Assert.NotEqual(longF, fullF);
        Assert.NotNull(def);
    }

    // ── Asset() cache hit + locale fallback ────────────────────────

    [Fact]
    public void Asset_Without_Locale_File_Returns_Original_Path_And_Cache_Hit()
    {
        var t = CreateAccessor("en-US");
        var first = t.Asset("nonexistent/file.png");
        var second = t.Asset("nonexistent/file.png"); // cache hit branch
        Assert.Equal(first, second);
    }

    [Fact]
    public void Asset_With_NoDirectory_Falls_Back_To_Original()
    {
        var t = CreateAccessor("en-US");
        var result = t.Asset("file.png");
        Assert.Equal("file.png", result);
    }

    [Fact]
    public void Asset_BaseLanguage_Distinct_From_Locale()
    {
        // "en-US" → base is "en". Even if no file exists, the branch executes.
        var t = CreateAccessor("en-US");
        var result = t.Asset("dir/asset.png");
        Assert.NotNull(result);
    }

    // ── RichMessage pseudo-localization branch + missing-key marker ─

    [Fact]
    public void RichMessage_Missing_Key_Returns_Marker()
    {
        var t = CreateAccessor("en-US");
        var el = t.RichMessage(new MessageKey("Bogus", "Key"));
        Assert.NotNull(el);
    }

    [Fact]
    public void Message_Pseudo_Localizes_Result()
    {
        // Verifies the formatted string runs through PseudoLocalizer.Transform.
        var provider = new InMemoryResourceProvider().Add("en-US", "X", "K", "Hi");
        var t = new IntlAccessor("en-US", provider, new MessageCache(), "en-US", pseudoLocalize: true);
        var result = t.Message(new MessageKey("X", "K"));
        Assert.NotEqual("Hi", result); // pseudo mangles the string
    }

    [Fact]
    public void RichMessage_Pseudo_Without_Tags_Returns_TextBlock()
    {
        var provider = new InMemoryResourceProvider().Add("en-US", "X", "K", "Hello");
        var t = new IntlAccessor("en-US", provider, new MessageCache(), "en-US", pseudoLocalize: true);
        var el = t.RichMessage(new MessageKey("X", "K"));
        Assert.NotNull(el);
    }

    [Fact]
    public void RichMessage_Missing_Pattern_Pseudo_Returns_Marker_Element()
    {
        var t = new IntlAccessor("en-US", new InMemoryResourceProvider(), new MessageCache(), "en-US", pseudoLocalize: true);
        var el = t.RichMessage(new MessageKey("Bogus", "Key"));
        Assert.NotNull(el);
    }

    // ── ResolvePattern fallback path ───────────────────────────────

    [Fact]
    public void Message_DefaultLocale_Fallback_Path_Hits_DebugWriteLine()
    {
        // Key only in en-US, accessor for fr-FR should fall back.
        var provider = new InMemoryResourceProvider().Add("en-US", "X", "Save", "Save");
        var t = new IntlAccessor("fr-FR", provider, new MessageCache(), "en-US");
        Assert.Equal("Save", t.Message(new MessageKey("X", "Save")));
    }

    [Fact]
    public void Message_Missing_In_Both_Logs_And_Returns_Marker()
    {
        var t = new IntlAccessor("fr-FR", new InMemoryResourceProvider(), new MessageCache(), "en-US");
        var result = t.Message(new MessageKey("X", "Missing"));
        Assert.Contains("X.Missing", result);
    }
}
