// C# port of Meta's Yoga layout engine Style.
// Ported from yoga/style/Style.h
// Simplifies C++ StyleValuePool to plain fields (memory compaction less important in managed code).

namespace Duct.Yoga;

/// <summary>
/// Stores all CSS-like style properties for a YogaNode.
/// Simplified from C++ version: replaces StyleValuePool with direct fields.
/// </summary>
/// <remarks>
/// NOTE: CSS Grid properties (gridTemplateColumns, gridTemplateRows, gridAutoColumns,
/// gridAutoRows, gridColumnStart/End, gridRowStart/End) present in the C++ Yoga
/// implementation are intentionally omitted — grid layout is not yet supported in
/// this C# port.
/// </remarks>
internal sealed class YogaStyle
{
    public const float DefaultFlexGrow = 0.0f;
    public const float DefaultFlexShrink = 0.0f;
    public const float WebDefaultFlexShrink = 1.0f;

    // Enum properties
    public YogaDirection Direction = YogaDirection.Inherit;
    public YogaFlexDirection FlexDirection = YogaFlexDirection.Column;
    public YogaJustify JustifyContent = YogaJustify.FlexStart;
    public YogaJustify JustifyItems = YogaJustify.Stretch;
    public YogaJustify JustifySelf = YogaJustify.Auto;
    public YogaAlign AlignContent = YogaAlign.FlexStart;
    public YogaAlign AlignItems = YogaAlign.Stretch;
    public YogaAlign AlignSelf = YogaAlign.Auto;
    public YogaPositionType PositionType = YogaPositionType.Relative;
    public YogaWrap FlexWrap = YogaWrap.NoWrap;
    public YogaOverflow Overflow = YogaOverflow.Visible;
    public YogaDisplay Display = YogaDisplay.Flex;
    public YogaBoxSizing BoxSizing = YogaBoxSizing.BorderBox;

    // Flex properties (NaN = undefined)
    public float Flex = float.NaN;
    public float FlexGrow = float.NaN;
    public float FlexShrink = float.NaN;
    public YogaValue FlexBasis = YogaValue.Auto;

    // Edge-indexed arrays (indexed by YogaEdge: Left=0..All=8)
    public readonly YogaValue[] Margin = CreateUndefinedEdges();
    public readonly YogaValue[] Position = CreateUndefinedEdges();
    public readonly YogaValue[] Padding = CreateUndefinedEdges();
    public readonly YogaValue[] Border = CreateUndefinedEdges();

    // Gutter-indexed array (Column=0, Row=1, All=2)
    public readonly YogaValue[] Gap = new YogaValue[3]
    {
        YogaValue.Undefined, YogaValue.Undefined, YogaValue.Undefined
    };

    // Dimension-indexed arrays (Width=0, Height=1)
    public readonly YogaValue[] Dimensions = new YogaValue[2]
    {
        YogaValue.Auto, YogaValue.Auto
    };
    public readonly YogaValue[] MinDimensions = new YogaValue[2]
    {
        YogaValue.Undefined, YogaValue.Undefined
    };
    public readonly YogaValue[] MaxDimensions = new YogaValue[2]
    {
        YogaValue.Undefined, YogaValue.Undefined
    };

    // Aspect ratio (NaN = undefined)
    public float AspectRatio = float.NaN;

    private static YogaValue[] CreateUndefinedEdges()
    {
        var arr = new YogaValue[9];
        for (int i = 0; i < 9; i++)
            arr[i] = YogaValue.Undefined;
        return arr;
    }

    // ── Edge computation (resolves Start/End/Horizontal/Vertical/All fallbacks) ──

    public YogaValue ComputeColumnGap()
    {
        var col = Gap[(int)YogaGutter.Column];
        return col.IsDefined ? col : Gap[(int)YogaGutter.All];
    }

    public YogaValue ComputeRowGap()
    {
        var row = Gap[(int)YogaGutter.Row];
        return row.IsDefined ? row : Gap[(int)YogaGutter.All];
    }

    /// <summary>Resolve the left edge value considering Start/End/Left/Horizontal/All fallbacks.</summary>
    private YogaValue ComputeLeftEdge(YogaValue[] edges, YogaDirection layoutDirection)
    {
        if (layoutDirection == YogaDirection.LTR && edges[(int)YogaEdge.Start].IsDefined)
            return edges[(int)YogaEdge.Start];
        if (layoutDirection == YogaDirection.RTL && edges[(int)YogaEdge.End].IsDefined)
            return edges[(int)YogaEdge.End];
        if (edges[(int)YogaEdge.Left].IsDefined)
            return edges[(int)YogaEdge.Left];
        if (edges[(int)YogaEdge.Horizontal].IsDefined)
            return edges[(int)YogaEdge.Horizontal];
        return edges[(int)YogaEdge.All];
    }

    private YogaValue ComputeTopEdge(YogaValue[] edges)
    {
        if (edges[(int)YogaEdge.Top].IsDefined) return edges[(int)YogaEdge.Top];
        if (edges[(int)YogaEdge.Vertical].IsDefined) return edges[(int)YogaEdge.Vertical];
        return edges[(int)YogaEdge.All];
    }

    private YogaValue ComputeRightEdge(YogaValue[] edges, YogaDirection layoutDirection)
    {
        if (layoutDirection == YogaDirection.LTR && edges[(int)YogaEdge.End].IsDefined)
            return edges[(int)YogaEdge.End];
        if (layoutDirection == YogaDirection.RTL && edges[(int)YogaEdge.Start].IsDefined)
            return edges[(int)YogaEdge.Start];
        if (edges[(int)YogaEdge.Right].IsDefined)
            return edges[(int)YogaEdge.Right];
        if (edges[(int)YogaEdge.Horizontal].IsDefined)
            return edges[(int)YogaEdge.Horizontal];
        return edges[(int)YogaEdge.All];
    }

    private YogaValue ComputeBottomEdge(YogaValue[] edges)
    {
        if (edges[(int)YogaEdge.Bottom].IsDefined) return edges[(int)YogaEdge.Bottom];
        if (edges[(int)YogaEdge.Vertical].IsDefined) return edges[(int)YogaEdge.Vertical];
        return edges[(int)YogaEdge.All];
    }

    private YogaValue ComputeEdge(YogaValue[] edges, YogaPhysicalEdge edge, YogaDirection direction)
    {
        return edge switch
        {
            YogaPhysicalEdge.Left => ComputeLeftEdge(edges, direction),
            YogaPhysicalEdge.Top => ComputeTopEdge(edges),
            YogaPhysicalEdge.Right => ComputeRightEdge(edges, direction),
            YogaPhysicalEdge.Bottom => ComputeBottomEdge(edges),
            _ => YogaValue.Undefined,
        };
    }

    // ── Position queries ──

    public YogaValue ComputePosition(YogaPhysicalEdge edge, YogaDirection direction)
        => ComputeEdge(Position, edge, direction);

    public YogaValue ComputeMargin(YogaPhysicalEdge edge, YogaDirection direction)
        => ComputeEdge(Margin, edge, direction);

    public YogaValue ComputePadding(YogaPhysicalEdge edge, YogaDirection direction)
        => ComputeEdge(Padding, edge, direction);

    public YogaValue ComputeBorder(YogaPhysicalEdge edge, YogaDirection direction)
        => ComputeEdge(Border, edge, direction);

    // ── Inset queries ──

    public bool HorizontalInsetsDefined =>
        Position[(int)YogaEdge.Left].IsDefined ||
        Position[(int)YogaEdge.Right].IsDefined ||
        Position[(int)YogaEdge.All].IsDefined ||
        Position[(int)YogaEdge.Horizontal].IsDefined ||
        Position[(int)YogaEdge.Start].IsDefined ||
        Position[(int)YogaEdge.End].IsDefined;

    public bool VerticalInsetsDefined =>
        Position[(int)YogaEdge.Top].IsDefined ||
        Position[(int)YogaEdge.Bottom].IsDefined ||
        Position[(int)YogaEdge.All].IsDefined ||
        Position[(int)YogaEdge.Vertical].IsDefined;

    // ── Flex-direction-aware computed values ──

    public bool IsFlexStartPositionDefined(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.FlexStartEdge(axis), direction).IsDefined;

    public bool IsFlexStartPositionAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.FlexStartEdge(axis), direction).IsAuto;

    public bool IsInlineStartPositionDefined(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.InlineStartEdge(axis, direction), direction).IsDefined;

    public bool IsInlineStartPositionAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.InlineStartEdge(axis, direction), direction).IsAuto;

    public bool IsFlexEndPositionDefined(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.FlexEndEdge(axis), direction).IsDefined;

    public bool IsFlexEndPositionAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.FlexEndEdge(axis), direction).IsAuto;

    public bool IsInlineEndPositionDefined(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.InlineEndEdge(axis, direction), direction).IsDefined;

    public bool IsInlineEndPositionAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputePosition(FlexDirectionHelper.InlineEndEdge(axis, direction), direction).IsAuto;

    // ── Computed position values ──

    public float ComputeFlexStartPosition(YogaFlexDirection axis, YogaDirection direction, float axisSize)
        => YogaFloat.UnwrapOrDefault(ComputePosition(FlexDirectionHelper.FlexStartEdge(axis), direction).Resolve(axisSize));

    public float ComputeInlineStartPosition(YogaFlexDirection axis, YogaDirection direction, float axisSize)
        => YogaFloat.UnwrapOrDefault(ComputePosition(FlexDirectionHelper.InlineStartEdge(axis, direction), direction).Resolve(axisSize));

    public float ComputeFlexEndPosition(YogaFlexDirection axis, YogaDirection direction, float axisSize)
        => YogaFloat.UnwrapOrDefault(ComputePosition(FlexDirectionHelper.FlexEndEdge(axis), direction).Resolve(axisSize));

    public float ComputeInlineEndPosition(YogaFlexDirection axis, YogaDirection direction, float axisSize)
        => YogaFloat.UnwrapOrDefault(ComputePosition(FlexDirectionHelper.InlineEndEdge(axis, direction), direction).Resolve(axisSize));

    // ── Computed margin values ──

    public float ComputeFlexStartMargin(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.UnwrapOrDefault(ComputeMargin(FlexDirectionHelper.FlexStartEdge(axis), direction).Resolve(widthSize));

    public float ComputeInlineStartMargin(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.UnwrapOrDefault(ComputeMargin(FlexDirectionHelper.InlineStartEdge(axis, direction), direction).Resolve(widthSize));

    public float ComputeFlexEndMargin(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.UnwrapOrDefault(ComputeMargin(FlexDirectionHelper.FlexEndEdge(axis), direction).Resolve(widthSize));

    public float ComputeInlineEndMargin(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.UnwrapOrDefault(ComputeMargin(FlexDirectionHelper.InlineEndEdge(axis, direction), direction).Resolve(widthSize));

    // ── Computed border values (clamped to >= 0) ──

    public float ComputeFlexStartBorder(YogaFlexDirection axis, YogaDirection direction)
        => YogaFloat.MaxOrDefined(ComputeBorder(FlexDirectionHelper.FlexStartEdge(axis), direction).Resolve(0), 0);

    public float ComputeInlineStartBorder(YogaFlexDirection axis, YogaDirection direction)
        => YogaFloat.MaxOrDefined(ComputeBorder(FlexDirectionHelper.InlineStartEdge(axis, direction), direction).Resolve(0), 0);

    public float ComputeFlexEndBorder(YogaFlexDirection axis, YogaDirection direction)
        => YogaFloat.MaxOrDefined(ComputeBorder(FlexDirectionHelper.FlexEndEdge(axis), direction).Resolve(0), 0);

    public float ComputeInlineEndBorder(YogaFlexDirection axis, YogaDirection direction)
        => YogaFloat.MaxOrDefined(ComputeBorder(FlexDirectionHelper.InlineEndEdge(axis, direction), direction).Resolve(0), 0);

    // ── Computed padding values (clamped to >= 0) ──

    public float ComputeFlexStartPadding(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.MaxOrDefined(ComputePadding(FlexDirectionHelper.FlexStartEdge(axis), direction).Resolve(widthSize), 0);

    public float ComputeInlineStartPadding(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.MaxOrDefined(ComputePadding(FlexDirectionHelper.InlineStartEdge(axis, direction), direction).Resolve(widthSize), 0);

    public float ComputeFlexEndPadding(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.MaxOrDefined(ComputePadding(FlexDirectionHelper.FlexEndEdge(axis), direction).Resolve(widthSize), 0);

    public float ComputeInlineEndPadding(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => YogaFloat.MaxOrDefined(ComputePadding(FlexDirectionHelper.InlineEndEdge(axis, direction), direction).Resolve(widthSize), 0);

    // ── Combined padding + border ──

    public float ComputeInlineStartPaddingAndBorder(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => ComputeInlineStartPadding(axis, direction, widthSize) + ComputeInlineStartBorder(axis, direction);

    public float ComputeFlexStartPaddingAndBorder(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => ComputeFlexStartPadding(axis, direction, widthSize) + ComputeFlexStartBorder(axis, direction);

    public float ComputeInlineEndPaddingAndBorder(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => ComputeInlineEndPadding(axis, direction, widthSize) + ComputeInlineEndBorder(axis, direction);

    public float ComputeFlexEndPaddingAndBorder(YogaFlexDirection axis, YogaDirection direction, float widthSize)
        => ComputeFlexEndPadding(axis, direction, widthSize) + ComputeFlexEndBorder(axis, direction);

    public float ComputePaddingAndBorderForDimension(YogaDirection direction, YogaDimension dimension, float widthSize)
    {
        var flexDir = dimension == YogaDimension.Width ? YogaFlexDirection.Row : YogaFlexDirection.Column;
        return ComputeFlexStartPaddingAndBorder(flexDir, direction, widthSize)
             + ComputeFlexEndPaddingAndBorder(flexDir, direction, widthSize);
    }

    public float ComputeBorderForAxis(YogaFlexDirection axis)
        => ComputeInlineStartBorder(axis, YogaDirection.LTR) + ComputeInlineEndBorder(axis, YogaDirection.LTR);

    public float ComputeMarginForAxis(YogaFlexDirection axis, float widthSize)
        => ComputeInlineStartMargin(axis, YogaDirection.LTR, widthSize) + ComputeInlineEndMargin(axis, YogaDirection.LTR, widthSize);

    public float ComputeGapForAxis(YogaFlexDirection axis, float ownerSize)
    {
        var gap = FlexDirectionHelper.IsRow(axis) ? ComputeColumnGap() : ComputeRowGap();
        return YogaFloat.MaxOrDefined(gap.Resolve(ownerSize), 0);
    }

    // ── Auto margin queries ──

    public bool FlexStartMarginIsAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputeMargin(FlexDirectionHelper.FlexStartEdge(axis), direction).IsAuto;

    public bool FlexEndMarginIsAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputeMargin(FlexDirectionHelper.FlexEndEdge(axis), direction).IsAuto;

    public bool InlineStartMarginIsAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputeMargin(FlexDirectionHelper.InlineStartEdge(axis, direction), direction).IsAuto;

    public bool InlineEndMarginIsAuto(YogaFlexDirection axis, YogaDirection direction)
        => ComputeMargin(FlexDirectionHelper.InlineEndEdge(axis, direction), direction).IsAuto;

    // ── Resolved min/max dimensions (accounting for box-sizing) ──

    public float ResolvedMinDimension(YogaDirection direction, YogaDimension axis, float referenceLength, float ownerWidth)
    {
        float value = MinDimensions[(int)axis].Resolve(referenceLength);
        if (BoxSizing == YogaBoxSizing.BorderBox)
            return value;

        // Match C++ FloatOptional addition: always add padding+border in content-box mode,
        // even when value is undefined — the padding+border itself forms a minimum.
        float paddingAndBorder = ComputePaddingAndBorderForDimension(direction, axis, ownerWidth);
        float pb = YogaFloat.IsDefined(paddingAndBorder) ? paddingAndBorder : 0;
        return value + pb;
    }

    public float ResolvedMaxDimension(YogaDirection direction, YogaDimension axis, float referenceLength, float ownerWidth)
    {
        float value = MaxDimensions[(int)axis].Resolve(referenceLength);
        if (BoxSizing == YogaBoxSizing.BorderBox)
            return value;

        // Match C++ FloatOptional addition: always add padding+border in content-box mode.
        float paddingAndBorder = ComputePaddingAndBorderForDimension(direction, axis, ownerWidth);
        float pb = YogaFloat.IsDefined(paddingAndBorder) ? paddingAndBorder : 0;
        return value + pb;
    }
}
