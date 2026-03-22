#!/usr/bin/env node
// Converts Yoga C++ generated tests to C# xUnit tests for Duct.Yoga
// Usage: node convert-yoga-tests.js <input-dir> <output-dir>

const fs = require('fs');
const path = require('path');

const inputDir = process.argv[2] || 'C:/Users/andersonch/Code/yoga/tests/generated';
const outputDir = process.argv[3] || 'C:/Users/andersonch/Code/patch/Duct.Tests/YogaGenerated';

// Enum mappings: C++ enum value → C# expression
const enumMap = {
  'YGFlexDirectionRow': 'YogaFlexDirection.Row',
  'YGFlexDirectionRowReverse': 'YogaFlexDirection.RowReverse',
  'YGFlexDirectionColumn': 'YogaFlexDirection.Column',
  'YGFlexDirectionColumnReverse': 'YogaFlexDirection.ColumnReverse',
  'YGJustifyFlexStart': 'YogaJustify.FlexStart',
  'YGJustifyCenter': 'YogaJustify.Center',
  'YGJustifyFlexEnd': 'YogaJustify.FlexEnd',
  'YGJustifySpaceBetween': 'YogaJustify.SpaceBetween',
  'YGJustifySpaceAround': 'YogaJustify.SpaceAround',
  'YGJustifySpaceEvenly': 'YogaJustify.SpaceEvenly',
  'YGAlignAuto': 'YogaAlign.Auto',
  'YGAlignFlexStart': 'YogaAlign.FlexStart',
  'YGAlignCenter': 'YogaAlign.Center',
  'YGAlignFlexEnd': 'YogaAlign.FlexEnd',
  'YGAlignStretch': 'YogaAlign.Stretch',
  'YGAlignBaseline': 'YogaAlign.Baseline',
  'YGAlignSpaceBetween': 'YogaAlign.SpaceBetween',
  'YGAlignSpaceAround': 'YogaAlign.SpaceAround',
  'YGAlignSpaceEvenly': 'YogaAlign.SpaceEvenly',
  'YGWrapNoWrap': 'YogaWrap.NoWrap',
  'YGWrapWrap': 'YogaWrap.Wrap',
  'YGWrapWrapReverse': 'YogaWrap.WrapReverse',
  'YGPositionTypeAbsolute': 'YogaPositionType.Absolute',
  'YGPositionTypeRelative': 'YogaPositionType.Relative',
  'YGPositionTypeStatic': 'YogaPositionType.Static',
  'YGDisplayFlex': 'YogaDisplay.Flex',
  'YGDisplayNone': 'YogaDisplay.None',
  'YGDisplayContents': 'YogaDisplay.Contents',
  'YGOverflowVisible': 'YogaOverflow.Visible',
  'YGOverflowHidden': 'YogaOverflow.Hidden',
  'YGOverflowScroll': 'YogaOverflow.Scroll',
  'YGDirectionLTR': 'YogaDirection.LTR',
  'YGDirectionRTL': 'YogaDirection.RTL',
  'YGDirectionInherit': 'YogaDirection.Inherit',
  'YGEdgeLeft': 'YogaEdge.Left',
  'YGEdgeTop': 'YogaEdge.Top',
  'YGEdgeRight': 'YogaEdge.Right',
  'YGEdgeBottom': 'YogaEdge.Bottom',
  'YGEdgeStart': 'YogaEdge.Start',
  'YGEdgeEnd': 'YogaEdge.End',
  'YGEdgeHorizontal': 'YogaEdge.Horizontal',
  'YGEdgeVertical': 'YogaEdge.Vertical',
  'YGEdgeAll': 'YogaEdge.All',
  'YGGutterColumn': 'YogaGutter.Column',
  'YGGutterRow': 'YogaGutter.Row',
  'YGGutterAll': 'YogaGutter.All',
  'YGBoxSizingContentBox': 'YogaBoxSizing.ContentBox',
  'YGBoxSizingBorderBox': 'YogaBoxSizing.BorderBox',
  'YGUndefined': 'float.NaN',
  'YGExperimentalFeatureFixFlexBasisFitContent': 'YogaExperimentalFeature.FixFlexBasisFitContent',
};

// Style setter patterns → C# code
// Each regex captures node variable + args
const styleSetters = [
  // Simple property setters (node, value)
  [/YGNodeStyleSetFlexDirection\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.FlexDirection = ${mapEnum(v)}`],
  [/YGNodeStyleSetJustifyContent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.JustifyContent = ${mapEnum(v)}`],
  [/YGNodeStyleSetAlignContent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.AlignContent = ${mapEnum(v)}`],
  [/YGNodeStyleSetAlignItems\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.AlignItems = ${mapEnum(v)}`],
  [/YGNodeStyleSetAlignSelf\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.AlignSelf = ${mapEnum(v)}`],
  [/YGNodeStyleSetFlexWrap\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.FlexWrap = ${mapEnum(v)}`],
  [/YGNodeStyleSetPositionType\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.PositionType = ${mapEnum(v)}`],
  [/YGNodeStyleSetDisplay\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.Display = ${mapEnum(v)}`],
  [/YGNodeStyleSetOverflow\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.Overflow = ${mapEnum(v)}`],
  [/YGNodeStyleSetBoxSizing\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.Style.BoxSizing = ${mapEnum(v)}`],

  // Float property setters
  [/YGNodeStyleSetFlexGrow\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.FlexGrow = ${mapVal(v)}`],
  [/YGNodeStyleSetFlexShrink\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.FlexShrink = ${mapVal(v)}`],
  [/YGNodeStyleSetAspectRatio\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.AspectRatio = ${mapVal(v)}`],

  // Dimension setters (point)
  [/YGNodeStyleSetWidth\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.Width = YogaValue.Point(${mapVal(v)})`],
  [/YGNodeStyleSetHeight\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.Height = YogaValue.Point(${mapVal(v)})`],
  [/YGNodeStyleSetMinWidth\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MinWidth = YogaValue.Point(${mapVal(v)})`],
  [/YGNodeStyleSetMinHeight\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MinHeight = YogaValue.Point(${mapVal(v)})`],
  [/YGNodeStyleSetMaxWidth\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MaxWidth = YogaValue.Point(${mapVal(v)})`],
  [/YGNodeStyleSetMaxHeight\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MaxHeight = YogaValue.Point(${mapVal(v)})`],

  // Dimension setters (percent)
  [/YGNodeStyleSetWidthPercent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.Width = YogaValue.Percent(${mapVal(v)})`],
  [/YGNodeStyleSetHeightPercent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.Height = YogaValue.Percent(${mapVal(v)})`],
  [/YGNodeStyleSetMinWidthPercent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MinWidth = YogaValue.Percent(${mapVal(v)})`],
  [/YGNodeStyleSetMinHeightPercent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MinHeight = YogaValue.Percent(${mapVal(v)})`],
  [/YGNodeStyleSetMaxWidthPercent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MaxWidth = YogaValue.Percent(${mapVal(v)})`],
  [/YGNodeStyleSetMaxHeightPercent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.MaxHeight = YogaValue.Percent(${mapVal(v)})`],

  // Dimension setters (auto)
  [/YGNodeStyleSetWidthAuto\((\w+)\)/g, (_, n) => `${n}.Width = YogaValue.Auto`],
  [/YGNodeStyleSetHeightAuto\((\w+)\)/g, (_, n) => `${n}.Height = YogaValue.Auto`],

  // Dimension setters (special sizing keywords)
  [/YGNodeStyleSetWidthFitContent\((\w+)\)/g, (_, n) => `${n}.Width = new YogaValue(0, YogaUnit.FitContent)`],
  [/YGNodeStyleSetHeightFitContent\((\w+)\)/g, (_, n) => `${n}.Height = new YogaValue(0, YogaUnit.FitContent)`],
  [/YGNodeStyleSetWidthMaxContent\((\w+)\)/g, (_, n) => `${n}.Width = new YogaValue(0, YogaUnit.MaxContent)`],
  [/YGNodeStyleSetHeightMaxContent\((\w+)\)/g, (_, n) => `${n}.Height = new YogaValue(0, YogaUnit.MaxContent)`],
  [/YGNodeStyleSetWidthStretch\((\w+)\)/g, (_, n) => `${n}.Width = new YogaValue(0, YogaUnit.Stretch)`],
  [/YGNodeStyleSetHeightStretch\((\w+)\)/g, (_, n) => `${n}.Height = new YogaValue(0, YogaUnit.Stretch)`],
  [/YGNodeStyleSetMinWidthFitContent\((\w+)\)/g, (_, n) => `${n}.MinWidth = new YogaValue(0, YogaUnit.FitContent)`],
  [/YGNodeStyleSetMinHeightFitContent\((\w+)\)/g, (_, n) => `${n}.MinHeight = new YogaValue(0, YogaUnit.FitContent)`],
  [/YGNodeStyleSetMinWidthMaxContent\((\w+)\)/g, (_, n) => `${n}.MinWidth = new YogaValue(0, YogaUnit.MaxContent)`],
  [/YGNodeStyleSetMinHeightMaxContent\((\w+)\)/g, (_, n) => `${n}.MinHeight = new YogaValue(0, YogaUnit.MaxContent)`],
  [/YGNodeStyleSetMinWidthStretch\((\w+)\)/g, (_, n) => `${n}.MinWidth = new YogaValue(0, YogaUnit.Stretch)`],
  [/YGNodeStyleSetMinHeightStretch\((\w+)\)/g, (_, n) => `${n}.MinHeight = new YogaValue(0, YogaUnit.Stretch)`],
  [/YGNodeStyleSetMaxWidthFitContent\((\w+)\)/g, (_, n) => `${n}.MaxWidth = new YogaValue(0, YogaUnit.FitContent)`],
  [/YGNodeStyleSetMaxHeightFitContent\((\w+)\)/g, (_, n) => `${n}.MaxHeight = new YogaValue(0, YogaUnit.FitContent)`],
  [/YGNodeStyleSetMaxWidthMaxContent\((\w+)\)/g, (_, n) => `${n}.MaxWidth = new YogaValue(0, YogaUnit.MaxContent)`],
  [/YGNodeStyleSetMaxHeightMaxContent\((\w+)\)/g, (_, n) => `${n}.MaxHeight = new YogaValue(0, YogaUnit.MaxContent)`],
  [/YGNodeStyleSetMaxWidthStretch\((\w+)\)/g, (_, n) => `${n}.MaxWidth = new YogaValue(0, YogaUnit.Stretch)`],
  [/YGNodeStyleSetMaxHeightStretch\((\w+)\)/g, (_, n) => `${n}.MaxHeight = new YogaValue(0, YogaUnit.Stretch)`],

  // FlexBasis
  [/YGNodeStyleSetFlexBasis\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.FlexBasis = YogaValue.Point(${mapVal(v)})`],
  [/YGNodeStyleSetFlexBasisPercent\((\w+),\s*(.+?)\)/g, (_, n, v) => `${n}.FlexBasis = YogaValue.Percent(${mapVal(v)})`],
  [/YGNodeStyleSetFlexBasisAuto\((\w+)\)/g, (_, n) => `${n}.FlexBasis = YogaValue.Auto`],
  [/YGNodeStyleSetFlexBasisFitContent\((\w+)\)/g, (_, n) => `${n}.FlexBasis = new YogaValue(0, YogaUnit.FitContent)`],
  [/YGNodeStyleSetFlexBasisMaxContent\((\w+)\)/g, (_, n) => `${n}.FlexBasis = new YogaValue(0, YogaUnit.MaxContent)`],
  [/YGNodeStyleSetFlexBasisStretch\((\w+)\)/g, (_, n) => `${n}.FlexBasis = new YogaValue(0, YogaUnit.Stretch)`],

  // Edge-indexed setters (node, edge, value)
  [/YGNodeStyleSetMargin\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, e, v) => `${n}.SetMargin(${mapEnum(e)}, YogaValue.Point(${mapVal(v)}))`],
  [/YGNodeStyleSetMarginPercent\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, e, v) => `${n}.SetMargin(${mapEnum(e)}, YogaValue.Percent(${mapVal(v)}))`],
  [/YGNodeStyleSetMarginAuto\((\w+),\s*(\w+)\)/g, (_, n, e) => `${n}.SetMargin(${mapEnum(e)}, YogaValue.Auto)`],
  [/YGNodeStyleSetPadding\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, e, v) => `${n}.SetPadding(${mapEnum(e)}, YogaValue.Point(${mapVal(v)}))`],
  [/YGNodeStyleSetPaddingPercent\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, e, v) => `${n}.SetPadding(${mapEnum(e)}, YogaValue.Percent(${mapVal(v)}))`],
  [/YGNodeStyleSetBorder\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, e, v) => `${n}.SetBorder(${mapEnum(e)}, ${mapVal(v)})`],
  [/YGNodeStyleSetPosition\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, e, v) => `${n}.SetPosition(${mapEnum(e)}, YogaValue.Point(${mapVal(v)}))`],
  [/YGNodeStyleSetPositionPercent\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, e, v) => `${n}.SetPosition(${mapEnum(e)}, YogaValue.Percent(${mapVal(v)}))`],
  [/YGNodeStyleSetPositionAuto\((\w+),\s*(\w+)\)/g, (_, n, e) => `${n}.SetPosition(${mapEnum(e)}, YogaValue.Auto)`],

  // Gap setters
  [/YGNodeStyleSetGap\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, g, v) => `${n}.SetGap(${mapEnum(g)}, ${mapVal(v)})`],
  [/YGNodeStyleSetGapPercent\((\w+),\s*(\w+),\s*(.+?)\)/g, (_, n, g, v) => `${n}.SetGap(${mapEnum(g)}, YogaValue.Percent(${mapVal(v)}))`],

  // Config
  [/YGConfigSetExperimentalFeatureEnabled\((\w+),\s*(\w+),\s*(\w+)\)/g, (_, c, f, v) => `${c}.SetExperimentalFeatureEnabled(${mapEnum(f)}, ${v})`],
];

function mapEnum(val) {
  val = val.trim();
  return enumMap[val] || val;
}

function mapVal(val) {
  val = val.trim();
  if (val === 'YGUndefined') return 'float.NaN';
  // Ensure float suffix for integer literals
  if (/^-?\d+$/.test(val)) return val + 'f';
  if (/^-?\d+\.\d+f?$/.test(val)) return val.endsWith('f') ? val : val + 'f';
  return val;
}

function toPascalCase(snakeCase) {
  return snakeCase.split('_').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join('_');
}

function convertFile(inputPath) {
  const content = fs.readFileSync(inputPath, 'utf-8');
  const fileName = path.basename(inputPath, '.cpp');
  // Class name: remove "YG" prefix, keep "Test" suffix
  const className = fileName.replace(/^YG/, 'Yoga');

  // Extract individual tests
  const testRegex = /TEST\(YogaTest,\s*(\w+)\)\s*\{([\s\S]*?)^\}/gm;
  const tests = [];
  let match;

  while ((match = testRegex.exec(content)) !== null) {
    tests.push({ name: match[1], body: match[2] });
  }

  if (tests.length === 0) return null;

  let output = `using Duct.Yoga;\nusing Xunit;\n\nnamespace Duct.Tests.YogaGenerated;\n\n`;
  output += `/// <summary>\n/// Ported from yoga/tests/generated/${fileName}.cpp\n/// </summary>\n`;
  output += `public class ${className}\n{\n`;

  for (const test of tests) {
    const methodName = toPascalCase(test.name);
    const isSkipped = test.body.includes('GTEST_SKIP()');
    if (isSkipped) {
      output += `    [Fact(Skip = "Skipped in upstream Yoga (GTEST_SKIP)")]\n`;
    } else {
      output += `    [Fact]\n`;
    }
    output += `    public void ${methodName}()\n    {\n`;
    output += convertTestBody(test.body);
    output += `    }\n\n`;
  }

  output += `}\n`;
  return output;
}

function convertTestBody(body) {
  let lines = body.split('\n');
  let result = [];
  let nodeDecls = new Map(); // track variable declarations
  let hasIntrinsicMeasure = false;

  for (let line of lines) {
    line = line.trimEnd();
    let trimmed = line.trim();

    // Skip empty lines, comments, free calls
    if (trimmed === '' || trimmed === '{' || trimmed === '}') continue;
    if (trimmed.startsWith('//')) { result.push(`        ${trimmed}`); continue; }
    if (trimmed.startsWith('YGNodeFreeRecursive')) continue;
    if (trimmed.startsWith('YGConfigFree')) continue;

    // Config creation
    if (trimmed.match(/YGConfigRef\s+(\w+)\s*=\s*YGConfigNew\(\)/)) {
      const m = trimmed.match(/YGConfigRef\s+(\w+)\s*=\s*YGConfigNew\(\)/);
      result.push(`        var ${m[1]} = new YogaConfig();`);
      continue;
    }

    // Node creation
    if (trimmed.match(/YGNodeRef\s+(\w+)\s*=\s*YGNodeNewWithConfig\((\w+)\)/)) {
      const m = trimmed.match(/YGNodeRef\s+(\w+)\s*=\s*YGNodeNewWithConfig\((\w+)\)/);
      result.push(`        var ${m[1]} = new YogaNode(${m[2]});`);
      nodeDecls.set(m[1], true);
      continue;
    }

    // Insert child
    if (trimmed.match(/YGNodeInsertChild\((\w+),\s*(\w+),\s*(\d+)\)/)) {
      const m = trimmed.match(/YGNodeInsertChild\((\w+),\s*(\w+),\s*(\d+)\)/);
      result.push(`        ${m[1]}.InsertChild(${m[2]}, ${m[3]});`);
      continue;
    }

    // Calculate layout
    if (trimmed.match(/YGNodeCalculateLayout\((\w+),\s*(.+?),\s*(.+?),\s*(.+?)\)/)) {
      const m = trimmed.match(/YGNodeCalculateLayout\((\w+),\s*(.+?),\s*(.+?),\s*(.+?)\)/);
      result.push(`        ${m[1]}.CalculateLayout(${mapVal(m[2])}, ${mapVal(m[3])}, ${mapEnum(m[4])});`);
      continue;
    }

    // Context + measure function for intrinsic size tests
    if (trimmed.match(/YGNodeSetContext\((\w+),\s*\(void\*\)"(.+?)"\)/)) {
      const m = trimmed.match(/YGNodeSetContext\((\w+),\s*\(void\*\)"(.+?)"\)/);
      result.push(`        ${m[1]}.Context = "${m[2]}";`);
      hasIntrinsicMeasure = true;
      continue;
    }

    if (trimmed.match(/YGNodeSetMeasureFunc\((\w+),\s*&facebook::yoga::test::IntrinsicSizeMeasure\)/)) {
      const m = trimmed.match(/YGNodeSetMeasureFunc\((\w+)/);
      result.push(`        ${m[1]}.MeasureFunction = IntrinsicSizeMeasureFunc;`);
      continue;
    }

    // Assertions
    if (trimmed.match(/ASSERT_FLOAT_EQ\((.+?),\s*YGNodeLayoutGet(\w+)\((\w+)\)\)/)) {
      const m = trimmed.match(/ASSERT_FLOAT_EQ\((.+?),\s*YGNodeLayoutGet(\w+)\((\w+)\)\)/);
      const expected = mapVal(m[1]);
      const prop = m[2]; // Left, Top, Width, Height
      const node = m[3];
      let csProp;
      switch (prop) {
        case 'Left': csProp = 'LayoutX'; break;
        case 'Top': csProp = 'LayoutY'; break;
        case 'Width': csProp = 'LayoutWidth'; break;
        case 'Height': csProp = 'LayoutHeight'; break;
        default: csProp = `Layout${prop}`;
      }
      result.push(`        Assert.Equal(${expected}, ${node}.${csProp});`);
      continue;
    }

    // Apply style setters
    let handled = false;
    for (const [regex, replacer] of styleSetters) {
      regex.lastIndex = 0;
      if (regex.test(trimmed)) {
        regex.lastIndex = 0;
        let converted = trimmed.replace(regex, replacer);
        // Remove trailing semicolon from C++ and add C# one
        converted = converted.replace(/;\s*$/, '');
        result.push(`        ${converted};`);
        handled = true;
        break;
      }
    }

    if (!handled) {
      // Fallback: add as comment
      if (trimmed.length > 0 && !trimmed.startsWith('//')) {
        result.push(`        // TODO: ${trimmed}`);
      }
    }
  }

  return result.join('\n') + '\n';
}

// Main
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

const files = fs.readdirSync(inputDir).filter(f => f.endsWith('.cpp'));
let totalTests = 0;
let totalFiles = 0;

// Check which files need IntrinsicSizeMeasure
const filesWithMeasure = new Set();
for (const file of files) {
  const content = fs.readFileSync(path.join(inputDir, file), 'utf-8');
  if (content.includes('IntrinsicSizeMeasure')) {
    filesWithMeasure.add(file);
  }
}

for (const file of files) {
  const inputPath = path.join(inputDir, file);
  const result = convertFile(inputPath);
  if (result) {
    let finalResult = result;

    // If this file uses IntrinsicSizeMeasure, add the helper
    if (filesWithMeasure.has(file)) {
      // Insert the helper method and using statement at the right place
      finalResult = finalResult.replace(
        'public class ',
        'public class '
      );
      // Add the measure function as a static method in the class
      const classEnd = finalResult.lastIndexOf('}');
      const helperMethod = `
    private static YogaSize IntrinsicSizeMeasureFunc(
        YogaNode node, float width, YogaMeasureMode widthMode,
        float height, YogaMeasureMode heightMode)
    {
        string text = (string)node.Context!;
        float widthPerChar = 10f;
        float heightPerChar = 10f;

        float measuredWidth;
        if (widthMode == YogaMeasureMode.Exactly)
            measuredWidth = width;
        else if (widthMode == YogaMeasureMode.AtMost)
            measuredWidth = Math.Min(text.Length * widthPerChar, width);
        else
            measuredWidth = text.Length * widthPerChar;

        float measuredHeight;
        float effectiveWidth = node.FlexDirection == YogaFlexDirection.Column
            ? measuredWidth
            : Math.Max(LongestWordWidth(text, widthPerChar), measuredWidth);

        if (heightMode == YogaMeasureMode.Exactly)
            measuredHeight = height;
        else
        {
            float calcHeight = CalculateTextHeight(text, effectiveWidth, widthPerChar, heightPerChar);
            measuredHeight = heightMode == YogaMeasureMode.AtMost
                ? Math.Min(calcHeight, height)
                : calcHeight;
        }

        return new YogaSize(measuredWidth, measuredHeight);
    }

    private static float LongestWordWidth(string text, float widthPerChar)
    {
        int maxLen = 0, curLen = 0;
        foreach (char c in text)
        {
            if (c == ' ') { maxLen = Math.Max(curLen, maxLen); curLen = 0; }
            else curLen++;
        }
        return Math.Max(curLen, maxLen) * widthPerChar;
    }

    private static float CalculateTextHeight(string text, float measuredWidth, float widthPerChar, float heightPerChar)
    {
        if (text.Length * widthPerChar <= measuredWidth) return heightPerChar;
        var words = text.Split(' ');
        float lines = 1, curLineLen = 0;
        foreach (var word in words)
        {
            float wordWidth = word.Length * widthPerChar;
            if (wordWidth > measuredWidth)
            {
                if (curLineLen > 0) lines++;
                lines++;
                curLineLen = 0;
            }
            else if (curLineLen + wordWidth <= measuredWidth)
            {
                curLineLen += wordWidth + widthPerChar;
            }
            else
            {
                lines++;
                curLineLen = wordWidth + widthPerChar;
            }
        }
        return (curLineLen == 0 ? lines - 1 : lines) * heightPerChar;
    }
`;
      finalResult = finalResult.slice(0, classEnd) + helperMethod + finalResult.slice(classEnd);
    }

    const outputFileName = file.replace(/^YG/, 'Yoga').replace('.cpp', '.cs');
    const outputPath = path.join(outputDir, outputFileName);
    fs.writeFileSync(outputPath, finalResult);

    const testCount = (result.match(/\[Fact\]/g) || []).length;
    totalTests += testCount;
    totalFiles++;
    console.log(`${file} → ${outputFileName} (${testCount} tests)`);
  }
}

console.log(`\nConverted ${totalTests} tests across ${totalFiles} files.`);
