using System.Collections.Immutable;
using Microsoft.UI.Reactor.Localization.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Localization;

public class LocSourceGeneratorTests
{
    private static GeneratorDriverRunResult RunGenerator(
        params (string path, string content)[] reswFiles)
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new LocSourceGenerator();

        var additionalTexts = reswFiles
            .Select(f => (AdditionalText)new InMemoryAdditionalText(f.path, f.content))
            .ToImmutableArray();

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            new Dictionary<string, string>
            {
                ["build_property.ReactorLocDefaultLocale"] = "en-US"
            });

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            additionalTexts: additionalTexts,
            optionsProvider: optionsProvider);

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    [Fact]
    public void FlatLayout_SingleReswFile_EmitsLocClassWithoutNesting()
    {
        var resw = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Save"" xml:space=""preserve""><value>Save</value></data>
  <data name=""Cancel"" xml:space=""preserve""><value>Cancel</value></data>
</root>";

        var result = RunGenerator(("Strings/en-US/Resources.resw", resw));

        Assert.Empty(result.Diagnostics);
        var generated = result.GeneratedTrees.Single();
        var source = generated.GetText().ToString();

        // Should have flat keys (no nested class)
        Assert.Contains("public static readonly MessageKey Save", source);
        Assert.Contains("public static readonly MessageKey Cancel", source);
        Assert.DoesNotContain("static class Resources", source);
    }

    [Fact]
    public void NamespacedLayout_MultipleReswFiles_EmitsNestedClasses()
    {
        var common = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Save"" xml:space=""preserve""><value>Save</value></data>
</root>";
        var settings = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Title"" xml:space=""preserve""><value>Settings</value></data>
</root>";

        var result = RunGenerator(
            ("Strings/en-US/Common.resw", common),
            ("Strings/en-US/Settings.resw", settings));

        Assert.Empty(result.Diagnostics);
        var generated = result.GeneratedTrees.Single();
        var source = generated.GetText().ToString();

        Assert.Contains("static class Common", source);
        Assert.Contains("static class Settings", source);
        Assert.Contains("new(\"Common\", \"Save\")", source);
        Assert.Contains("new(\"Settings\", \"Title\")", source);
    }

    [Fact]
    public void MissingKey_EmitsDiagnostic()
    {
        var enResw = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Save"" xml:space=""preserve""><value>Save</value></data>
  <data name=""Cancel"" xml:space=""preserve""><value>Cancel</value></data>
</root>";
        var frResw = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Save"" xml:space=""preserve""><value>Enregistrer</value></data>
</root>";

        var result = RunGenerator(
            ("Strings/en-US/Resources.resw", enResw),
            ("Strings/fr-FR/Resources.resw", frResw));

        // Should emit REACTOR_LOC001 for "Cancel" missing in fr-FR
        Assert.Contains(result.Diagnostics, d => d.Id == "REACTOR_LOC001" && d.GetMessage().Contains("Cancel"));
    }

    [Fact]
    public void XmlDocIncludes_DefaultLocaleValue()
    {
        var resw = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""WelcomeMessage"" xml:space=""preserve""><value>Welcome to Reactor!</value></data>
</root>";

        var result = RunGenerator(("Strings/en-US/Resources.resw", resw));

        var source = result.GeneratedTrees.Single().GetText().ToString();
        Assert.Contains("Welcome to Reactor!", source);
    }

    [Fact]
    public void NoReswFiles_NoOutput()
    {
        var compilation = CSharpCompilation.Create("TestAssembly",
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new LocSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        Assert.Empty(result.GeneratedTrees);
        Assert.Empty(result.Diagnostics);
    }

    // ── Test helpers ────────────────────────────────────────────────

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public override string Path { get; }

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content);
        }

        public override SourceText? GetText(CancellationToken cancellationToken = default) => _text;
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestGlobalOptions _globalOptions;

        public TestAnalyzerConfigOptionsProvider(Dictionary<string, string> globals)
        {
            _globalOptions = new TestGlobalOptions(globals);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestGlobalOptions.Empty;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestGlobalOptions.Empty;

        private sealed class TestGlobalOptions : AnalyzerConfigOptions
        {
            public static readonly TestGlobalOptions Empty = new(new Dictionary<string, string>());
            private readonly Dictionary<string, string> _values;

            public TestGlobalOptions(Dictionary<string, string> values) => _values = values;

            public override bool TryGetValue(string key, out string value)
            {
                if (_values.TryGetValue(key, out var val))
                {
                    value = val;
                    return true;
                }
                value = null!;
                return false;
            }
        }
    }
}
