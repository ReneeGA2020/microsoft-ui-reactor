// C# port of Meta's Yoga layout engine enums.
// Ported from yoga/enums/*.h

namespace Duct.Yoga;

public enum YogaAlign
{
    Auto = 0,
    FlexStart = 1,
    Center = 2,
    FlexEnd = 3,
    Stretch = 4,
    Baseline = 5,
    SpaceBetween = 6,
    SpaceAround = 7,
    SpaceEvenly = 8,
    Start = 9,
    End = 10,
}

public enum YogaBoxSizing
{
    BorderBox = 0,
    ContentBox = 1,
}

public enum YogaDimension
{
    Width = 0,
    Height = 1,
}

public enum YogaDirection
{
    Inherit = 0,
    LTR = 1,
    RTL = 2,
}

public enum YogaDisplay
{
    Flex = 0,
    None = 1,
    Contents = 2,
    /// <summary>
    /// Grid layout is not yet implemented in this C# port.
    /// Setting this value will throw <see cref="NotImplementedException"/>.
    /// </summary>
    Grid = 3,
}

public enum YogaEdge
{
    Left = 0,
    Top = 1,
    Right = 2,
    Bottom = 3,
    Start = 4,
    End = 5,
    Horizontal = 6,
    Vertical = 7,
    All = 8,
}

public enum YogaPhysicalEdge
{
    Left = 0,
    Top = 1,
    Right = 2,
    Bottom = 3,
}

[Flags]
public enum YogaErrata : uint
{
    None = 0,
    StretchFlexBasis = 1,
    AbsolutePositionWithoutInsetsExcludesPadding = 2,
    AbsolutePercentAgainstInnerSize = 4,
    All = 2147483647,
    Classic = 2147483646,
}

public enum YogaExperimentalFeature
{
    WebFlexBasis = 0,
    FixFlexBasisFitContent = 1,
}

public enum YogaFlexDirection
{
    Column = 0,
    ColumnReverse = 1,
    Row = 2,
    RowReverse = 3,
}

public enum YogaGutter
{
    Column = 0,
    Row = 1,
    All = 2,
}

public enum YogaJustify
{
    Auto = 0,
    FlexStart = 1,
    Center = 2,
    FlexEnd = 3,
    SpaceBetween = 4,
    SpaceAround = 5,
    SpaceEvenly = 6,
    Stretch = 7,
    Start = 8,
    End = 9,
}

public enum YogaLogLevel
{
    Error = 0,
    Warn = 1,
    Info = 2,
    Debug = 3,
    Verbose = 4,
    Fatal = 5,
}

public enum YogaMeasureMode
{
    Undefined = 0,
    Exactly = 1,
    AtMost = 2,
}

public enum YogaNodeType
{
    Default = 0,
    Text = 1,
}

public enum YogaOverflow
{
    Visible = 0,
    Hidden = 1,
    Scroll = 2,
}

public enum YogaPositionType
{
    Static = 0,
    Relative = 1,
    Absolute = 2,
}

public enum YogaUnit
{
    Undefined = 0,
    Point = 1,
    Percent = 2,
    Auto = 3,
    MaxContent = 4,
    FitContent = 5,
    Stretch = 6,
}

public enum YogaWrap
{
    NoWrap = 0,
    Wrap = 1,
    WrapReverse = 2,
}

/// <summary>
/// Internal sizing mode used by the algorithm, maps to/from YogaMeasureMode.
/// </summary>
internal enum SizingMode
{
    StretchFit = 0,
    MaxContent = 1,
    FitContent = 2,
}
