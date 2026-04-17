using System.Text;

namespace Microsoft.UI.Reactor.Cli.Loc;

/// <summary>
/// Builds ICU-aware system prompts for AI translation of .resw entries.
/// </summary>
internal static class TranslationPrompt
{
    /// <summary>
    /// Builds the system prompt that teaches the LLM about ICU message format
    /// and the translation requirements.
    /// </summary>
    public static string BuildSystemPrompt(string sourceLocale, string targetLocale)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a professional software localizer. Translate UI strings from the source locale to the target locale.");
        sb.AppendLine();
        sb.AppendLine($"Source locale: {sourceLocale}");
        sb.AppendLine($"Target locale: {targetLocale}");
        sb.AppendLine();
        sb.AppendLine("CRITICAL RULES:");
        sb.AppendLine("1. Preserve ICU Message Format syntax EXACTLY:");
        sb.AppendLine("   - {variableName} placeholders must remain unchanged: {name}, {count}, {date}");
        sb.AppendLine("   - {count, plural, one {# item} other {# items}} — translate text inside braces, keep structure");
        sb.AppendLine("   - {gender, select, male {He} female {She} other {They}} — translate values, keep category names");
        sb.AppendLine("   - # (hash) inside plural/selectordinal is a special token — do not translate it");
        sb.AppendLine("2. Do NOT translate variable names inside curly braces");
        sb.AppendLine("3. Do NOT add or remove curly braces");
        sb.AppendLine("4. Preserve leading/trailing whitespace in the source string");
        sb.AppendLine("5. Match the formality register appropriate for the target locale");
        sb.AppendLine();

        // Locale-specific instructions
        var localeHint = GetLocaleHint(targetLocale);
        if (localeHint != null)
        {
            sb.AppendLine("LOCALE-SPECIFIC INSTRUCTIONS:");
            sb.AppendLine(localeHint);
            sb.AppendLine();
        }

        sb.AppendLine("RESPONSE FORMAT:");
        sb.AppendLine("Respond with one translation per line in the exact format:");
        sb.AppendLine("KEY=TRANSLATED_VALUE");
        sb.AppendLine();
        sb.AppendLine("Do not add any commentary, explanations, or markdown formatting. Only output KEY=VALUE lines.");

        return sb.ToString();
    }

    /// <summary>
    /// Builds the user message containing the strings to translate.
    /// </summary>
    public static string BuildUserMessage(TranslationBatch batch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Translate the following strings:");
        sb.AppendLine();

        foreach (var entry in batch.Entries)
        {
            sb.AppendLine($"{entry.Key}={entry.Value}");
        }

        if (batch.ExistingTranslations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("For consistency context, here are existing translations in this locale:");
            sb.AppendLine();
            foreach (var (key, value) in batch.ExistingTranslations.Take(20))
            {
                sb.AppendLine($"{key}={value}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses the LLM response into key-value translations.
    /// </summary>
    public static Dictionary<string, string> ParseResponse(string response, IEnumerable<string> expectedKeys)
    {
        var result = new Dictionary<string, string>();
        var keySet = new HashSet<string>(expectedKeys, StringComparer.Ordinal);

        foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Find first '=' to split key from value
            var eqIdx = trimmed.IndexOf('=');
            if (eqIdx <= 0) continue;

            var key = trimmed[..eqIdx].Trim();
            var value = trimmed[(eqIdx + 1)..];

            if (keySet.Contains(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string? GetLocaleHint(string targetLocale)
    {
        // Extract base language
        var lang = targetLocale.Split('-')[0].ToLowerInvariant();

        return lang switch
        {
            "de" => "- Use formal register (Sie, not du) for UI text.\n- Compound nouns are written as one word.",
            "fr" => "- Use formal register (vous, not tu) for UI text.\n- Use proper French punctuation (space before : ; ! ?).",
            "ja" => "- Use polite form (desu/masu style).\n- Avoid katakana for words that have common kanji equivalents.\n- Keep translations concise — Japanese UI text should be shorter than English when possible.",
            "ko" => "- Use formal polite speech level (합쇼체/해요체).\n- Keep translations concise.",
            "zh" => targetLocale.Contains("TW", StringComparison.OrdinalIgnoreCase)
                ? "- Use Traditional Chinese characters.\n- Use Taiwan-standard terminology."
                : "- Use Simplified Chinese characters.\n- Use mainland China terminology.",
            "ar" => "- Use Modern Standard Arabic (MSA) for UI text.\n- Note: the UI supports RTL layout.",
            "he" => "- Note: the UI supports RTL layout.",
            "es" => targetLocale.Contains("419", StringComparison.OrdinalIgnoreCase)
                ? "- Use Latin American Spanish conventions."
                : "- Use Castilian Spanish conventions (vosotros form is acceptable).",
            "pt" => targetLocale.Contains("BR", StringComparison.OrdinalIgnoreCase)
                ? "- Use Brazilian Portuguese conventions.\n- Use formal register (você)."
                : "- Use European Portuguese conventions.",
            "ru" => "- Use formal register for UI text.",
            "tr" => "- Use formal register (siz, not sen) for UI text.",
            "hi" => "- Use formal Hindi (आप, not तुम/तू).\n- Use Devanagari script.",
            "th" => "- Use polite particles (ครับ/ค่ะ) where appropriate.\n- Use formal register.",
            _ => null,
        };
    }
}
