#!/usr/bin/env node
// Converts md4c spec test fixtures (.txt) to C# xUnit tests for Duct.Markdown
// Usage: node convert-md4c-tests.js [input-dir] [output-dir]

const fs = require('fs');
const path = require('path');

const inputDir = process.argv[2] || 'C:/Users/andersonch/Code/patch/Duct.Tests/Md4cFixtures';
const outputDir = process.argv[3] || 'C:/Users/andersonch/Code/patch/Duct.Tests/Md4cGenerated';

// Map md4c CLI flags to C# MdParserFlags expressions
const flagMap = {
    '--fcollapse-whitespace': 'MdParserFlags.CollapseWhitespace',
    '--fpermissive-atx-headers': 'MdParserFlags.PermissiveAtxHeaders',
    '--fpermissive-url-autolinks': 'MdParserFlags.PermissiveUrlAutolinks',
    '--fpermissive-email-autolinks': 'MdParserFlags.PermissiveEmailAutolinks',
    '--fno-indented-code': 'MdParserFlags.NoIndentedCodeBlocks',
    '--fno-html-blocks': 'MdParserFlags.NoHtmlBlocks',
    '--fno-html-spans': 'MdParserFlags.NoHtmlSpans',
    '--fno-html': 'MdParserFlags.NoHtml',
    '--ftables': 'MdParserFlags.Tables',
    '--fstrikethrough': 'MdParserFlags.Strikethrough',
    '--fpermissive-www-autolinks': 'MdParserFlags.PermissiveWwwAutolinks',
    '--ftasklists': 'MdParserFlags.TaskLists',
    '--flatex-math': 'MdParserFlags.LatexMathSpans',
    '--fwiki-links': 'MdParserFlags.WikiLinks',
    '--funderline': 'MdParserFlags.Underline',
    '--fhard-soft-breaks': 'MdParserFlags.HardSoftBreaks',
    '--fpermissive-autolinks': 'MdParserFlags.PermissiveAutolinks',
    '--fgithub': 'MdParserFlags.DialectGitHub',
};

// Map spec filenames to class names and default flags
const fileConfig = {
    'spec.txt':                      { className: 'CommonMarkSpec',          defaultFlags: [] },
    'coverage.txt':                  { className: 'Coverage',               defaultFlags: [] },
    'regressions.txt':               { className: 'Regressions',            defaultFlags: [] },
    'spec-tables.txt':               { className: 'Tables',                 defaultFlags: ['--ftables'] },
    'spec-strikethrough.txt':        { className: 'Strikethrough',          defaultFlags: ['--fstrikethrough'] },
    'spec-tasklists.txt':            { className: 'TaskLists',              defaultFlags: ['--ftasklists'] },
    'spec-wiki-links.txt':           { className: 'WikiLinks',              defaultFlags: ['--fwiki-links'] },
    'spec-latex-math.txt':           { className: 'LatexMath',              defaultFlags: ['--flatex-math'] },
    'spec-underline.txt':            { className: 'Underline',              defaultFlags: ['--funderline'] },
    'spec-permissive-autolinks.txt': { className: 'PermissiveAutolinks',    defaultFlags: ['--fpermissive-autolinks'] },
    'spec-hard-soft-breaks.txt':     { className: 'HardSoftBreaks',         defaultFlags: ['--fhard-soft-breaks'] },
};

function parseExamples(content, defaultFlags) {
    const examples = [];
    // Normalize line endings
    content = content.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
    // Match blocks delimited by 32 backticks with 'example' keyword
    const fence = '````````````````````````````````';
    const parts = content.split(fence);

    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];
        if (!part.trimStart().startsWith(' example') && !part.trimStart().startsWith('example'))
            continue;

        // Remove the ' example\n' or ' example [no-normalize]\n' prefix
        let body = part.replace(/^\s*example(?:\s+\[no-normalize\])?\n/, '');

        // Split on the separator line (a line containing just '.')
        // The separator is a '.' on its own line
        const sections = [];
        let currentSection = [];
        const lines = body.split('\n');
        for (const line of lines) {
            if (line === '.') {
                sections.push(currentSection.join('\n'));
                currentSection = [];
            } else {
                currentSection.push(line);
            }
        }
        // Last section (leftover after last '.')
        if (currentSection.length > 0) {
            const remainder = currentSection.join('\n').trim();
            if (remainder) {
                sections.push(remainder);
            }
        }

        if (sections.length < 2) continue;

        // The CommonMark spec uses → (U+2192) to represent tab characters.
        const markdown = sections[0].replace(/→/g, '\t');
        // The HTML output from md4c always ends with \n for block-level elements.
        // Preserve a trailing newline in expected HTML if the section ends with one.
        let html = sections[1].replace(/→/g, '\t');
        // The HTML section text has already been split at '\n' boundaries.
        // We need to add back the trailing '\n' since md4c output always ends that way.
        if (html.length > 0)
            html += '\n';
        let testFlags = [...defaultFlags];

        // Third section (if present) contains flags
        if (sections.length >= 3) {
            const flagSection = sections[2].trim();
            const flagLines = flagSection.split('\n');
            for (const line of flagLines) {
                const trimmed = line.trim();
                // Flags may be space-separated on a single line (e.g. "--fwiki-links --ftables")
                const parts = trimmed.split(/\s+/);
                for (const part of parts) {
                    if (part.startsWith('--')) {
                        testFlags.push(part);
                    }
                }
            }
        }

        examples.push({ markdown, html, flags: testFlags });
    }

    return examples;
}

function escapeCS(str) {
    return str
        .replace(/\\/g, '\\\\')
        .replace(/"/g, '\\"')
        .replace(/\r/g, '\\r')
        .replace(/\n/g, '\\n')
        .replace(/\t/g, '\\t')
        .replace(/\0/g, '\\0');
}

function flagsExpr(flags) {
    if (flags.length === 0) return 'MdParserFlags.None';
    const csFlags = [...new Set(flags.map(f => flagMap[f]).filter(Boolean))];
    if (csFlags.length === 0) return 'MdParserFlags.None';
    return csFlags.join(' | ');
}

function makeMethodName(index, markdown) {
    // Create a method name from the first non-empty line of markdown
    let name = `Example_${(index + 1).toString().padStart(4, '0')}`;
    return name;
}

function generateTestFile(filename, className, examples, sourceFile) {
    let output = '';
    output += 'using Duct.Markdown;\n';
    output += 'using System.Text;\n';
    output += 'using Xunit;\n';
    output += '\n';
    output += 'namespace Duct.Tests.Md4cGenerated;\n';
    output += '\n';
    output += `/// <summary>\n`;
    output += `/// Ported from md4c/test/${sourceFile}\n`;
    output += `/// </summary>\n`;
    output += `public class Md4c${className}Test\n`;
    output += '{\n';

    for (let i = 0; i < examples.length; i++) {
        const ex = examples[i];
        const methodName = makeMethodName(i, ex.markdown);
        const csFlags = flagsExpr(ex.flags);

        output += '    [Fact]\n';
        output += `    public void ${methodName}()\n`;
        output += '    {\n';
        output += `        var md = "${escapeCS(ex.markdown)}";\n`;
        output += `        var expected = "${escapeCS(ex.html)}";\n`;
        output += `        var actual = Md4cHtml.ToHtml(md, ${csFlags});\n`;
        output += '        Assert.Equal(Md4cTestHelper.NormalizeHtml(expected), Md4cTestHelper.NormalizeHtml(actual));\n';
        output += '    }\n';
        output += '\n';
    }

    output += '}\n';
    return output;
}

// Main
if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir, { recursive: true });
}

let totalTests = 0;

for (const [filename, config] of Object.entries(fileConfig)) {
    const inputPath = path.join(inputDir, filename);
    if (!fs.existsSync(inputPath)) {
        console.log(`Skipping ${filename} (not found)`);
        continue;
    }

    const content = fs.readFileSync(inputPath, 'utf8');
    const examples = parseExamples(content, config.defaultFlags);
    totalTests += examples.length;

    const outputPath = path.join(outputDir, `Md4c${config.className}Test.cs`);
    const csCode = generateTestFile(filename, config.className, examples, filename);
    fs.writeFileSync(outputPath, csCode, 'utf8');

    console.log(`${filename}: ${examples.length} tests -> ${path.basename(outputPath)}`);
}

console.log(`\nTotal: ${totalTests} tests generated`);
