using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.UI.Reactor.Cli.Loc;

/// <summary>
/// Converts C# interpolated string expressions to ICU Message Format strings.
/// Example: $"Hello, {user.Name}" → "Hello, {name}"
/// Example: $"Total: {price:C}" → "Total: {price, number, currency}"
/// </summary>
internal static class InterpolationConverter
{
    // C# format specifiers → ICU formatter types
    private static readonly Dictionary<string, string> FormatSpecifierMap = new(StringComparer.Ordinal)
    {
        ["C"] = "number, currency",
        ["C0"] = "number, currency",
        ["C2"] = "number, currency",
        ["N"] = "number",
        ["N0"] = "number",
        ["N2"] = "number",
        ["P"] = "number, percent",
        ["P0"] = "number, percent",
        ["P1"] = "number, percent",
        ["P2"] = "number, percent",
        ["F"] = "number",
        ["F0"] = "number",
        ["F1"] = "number",
        ["F2"] = "number",
        ["d"] = "date, short",
        ["D"] = "date, long",
        ["f"] = "date, full",
    };

    // Variable names that suggest quantities (for plural hint comments)
    private static readonly HashSet<string> QuantityHintNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "count", "total", "length", "size", "amount", "quantity",
    };

    public static (string? icuMessage, Dictionary<string, string>? argumentMap, List<string> warnings)
        Convert(InterpolatedStringExpressionSyntax interpolated)
    {
        var icuParts = new List<string>();
        var argumentMap = new Dictionary<string, string>();
        var warnings = new List<string>();
        var usedNames = new HashSet<string>();

        foreach (var content in interpolated.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax text:
                    // Escape ICU-special chars in literal text
                    icuParts.Add(EscapeForIcu(text.TextToken.ValueText));
                    break;

                case InterpolationSyntax hole:
                    var expr = hole.Expression;

                    // Unwrap parentheses: {(x ? "a" : "b")} → {x ? "a" : "b"}
                    while (expr is ParenthesizedExpressionSyntax parens)
                        expr = parens.Expression;

                    // Ternary with string-literal branches → ICU select
                    if (TryConvertTernarySelect(expr, usedNames, argumentMap, out var selectIcu))
                    {
                        icuParts.Add(selectIcu!);
                        break;
                    }

                    var (paramName, exprText, isComplex) = AnalyzeExpression(expr);

                    if (isComplex)
                    {
                        warnings.Add($"Complex expression '{expr}' cannot be extracted; consider extracting to a variable first");
                        // Still include it as a placeholder with a generated name
                        paramName ??= $"arg{usedNames.Count}";
                    }

                    // Ensure unique param names
                    var baseName = paramName ?? $"arg{usedNames.Count}";
                    var uniqueName = baseName;
                    var suffix = 2;
                    while (!usedNames.Add(uniqueName))
                    {
                        uniqueName = $"{baseName}{suffix++}";
                    }

                    // Map back to original expression
                    if (exprText != uniqueName)
                    {
                        argumentMap[uniqueName] = exprText;
                    }

                    // Convert format specifier to ICU formatter
                    var formatClause = hole.FormatClause;
                    if (formatClause != null)
                    {
                        var specifier = formatClause.FormatStringToken.ValueText;
                        // Strip precision digits for lookup (e.g., "C2" → "C")
                        var baseSpec = new string(specifier.TakeWhile(c => !char.IsDigit(c)).ToArray());
                        if (baseSpec.Length == 0) baseSpec = specifier;

                        if (FormatSpecifierMap.TryGetValue(baseSpec, out var icuFormat))
                        {
                            icuParts.Add($"{{{uniqueName}, {icuFormat}}}");
                        }
                        else
                        {
                            // Unknown format specifier — just use plain interpolation
                            warnings.Add($"Unknown format specifier ':{specifier}' on '{exprText}'");
                            icuParts.Add($"{{{uniqueName}}}");
                        }
                    }
                    else
                    {
                        icuParts.Add($"{{{uniqueName}}}");
                    }
                    break;
            }
        }

        if (icuParts.Count == 0) return (null, null, warnings);

        var icuMessage = string.Join("", icuParts);
        return (icuMessage, argumentMap.Count > 0 ? argumentMap : null, warnings);
    }

    /// <summary>
    /// Checks if a parameter name suggests a quantity (for plural hint comments).
    /// </summary>
    public static bool IsQuantityName(string name)
    {
        if (QuantityHintNames.Contains(name)) return true;
        // Check prefix patterns like "numItems", "totalCount"
        return name.StartsWith("num", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("total", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("count", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to convert a ternary with string-literal branches into an ICU select.
    /// Example: {(darkMode ? "Yes" : "No")} → {darkMode, select, true {Yes} false {No}}
    /// </summary>
    private static bool TryConvertTernarySelect(
        ExpressionSyntax expr,
        HashSet<string> usedNames,
        Dictionary<string, string> argumentMap,
        out string? icuSelect)
    {
        icuSelect = null;
        if (!(expr is ConditionalExpressionSyntax ternary))
            return false;

        // Both branches must be string literals
        if (!(ternary.WhenTrue is LiteralExpressionSyntax whenTrueLit)
            || !whenTrueLit.IsKind(SyntaxKind.StringLiteralExpression))
            return false;
        if (!(ternary.WhenFalse is LiteralExpressionSyntax whenFalseLit)
            || !whenFalseLit.IsKind(SyntaxKind.StringLiteralExpression))
            return false;

        // Resolve a parameter name from the condition
        var (condName, condExpr, _) = AnalyzeExpression(ternary.Condition);
        var baseName = condName ?? $"arg{usedNames.Count}";
        var uniqueName = baseName;
        var suffix = 2;
        while (!usedNames.Add(uniqueName))
            uniqueName = $"{baseName}{suffix++}";

        if (condExpr != uniqueName)
            argumentMap[uniqueName] = condExpr;

        var trueText = EscapeForIcu(whenTrueLit.Token.ValueText);
        var falseText = EscapeForIcu(whenFalseLit.Token.ValueText);

        icuSelect = $"{{{uniqueName}, select, true {{{trueText}}} false {{{falseText}}}}}";
        return true;
    }

    private static (string? name, string exprText, bool isComplex) AnalyzeExpression(ExpressionSyntax expr)
    {
        switch (expr)
        {
            case ParenthesizedExpressionSyntax parens:
                return AnalyzeExpression(parens.Expression);

            case IdentifierNameSyntax id:
                // Simple variable: {count} → {count}
                return (id.Identifier.Text, id.Identifier.Text, false);

            case MemberAccessExpressionSyntax member:
                // Dotted expression: {user.Name} → {name} with mapping name=user.Name
                var lastSegment = member.Name.Identifier.Text;
                var camelCase = char.ToLowerInvariant(lastSegment[0]) + lastSegment.Substring(1);
                return (camelCase, member.ToString(), false);

            case InvocationExpressionSyntax:
                // Method call: {GetTotal()} → complex, warn
                return (null, expr.ToString(), true);

            case ElementAccessExpressionSyntax:
                // Indexer: {items[0]} → complex
                return (null, expr.ToString(), true);

            case BinaryExpressionSyntax:
                // Binary op: {a + b} → complex
                return (null, expr.ToString(), true);

            case ConditionalExpressionSyntax:
                // Ternary with non-literal branches inside interpolation → complex
                return (null, expr.ToString(), true);

            default:
                return (null, expr.ToString(), true);
        }
    }

    private static string EscapeForIcu(string text)
    {
        // ICU uses single quotes for escaping and { } as syntax
        // We need to escape literal { } that aren't our placeholders
        // and single quotes
        return text.Replace("'", "''").Replace("{", "'{'").Replace("}", "'}'");
    }
}
