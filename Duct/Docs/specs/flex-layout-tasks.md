# Flex Layout Implementation Tasks

## Layer 1: Duct.Yoga — Pure C# Yoga Port

- [x] **1.1** YogaEnums.cs — All enum definitions (Direction, FlexDirection, Justify, Align, Wrap, PositionType, Display, Overflow, Edge, Unit, MeasureMode, BoxSizing, Gutter, Dimension, NodeType, Errata, ExperimentalFeature)
- [x] **1.2** YogaValue.cs — YogaValue record struct, FloatOptional equivalents, comparison helpers (inexactEquals, isUndefined/isDefined, maxOrDefined/minOrDefined)
- [x] **1.3** YogaConfig.cs — Config class (pointScaleFactor, useWebDefaults, experimentalFeatures, errata)
- [x] **1.4** LayoutResults.cs — Layout output data (dimensions, position, padding, margin, border, cached measurements, direction)
- [x] **1.5** YogaStyle.cs — Style properties storage (simplified from C++ StyleValuePool to plain fields)
- [x] **1.6** YogaNode.cs — Core node (tree structure, style, layout results, measure/baseline callbacks, dirty tracking, child iteration)
- [x] **1.7** AlgorithmUtils.cs — FlexDirection helpers, BoundAxis, Align resolution, TrailingPosition, SizingMode, FlexLine, PixelGrid, Cache, Baseline utilities
- [x] **1.8** YogaAlgorithm.cs — Main CalculateLayout algorithm + AbsoluteLayout (~2,100 lines)

## Layer 2: Duct.Flex — Standalone FlexPanel

- [x] **2.1** FlexPanel.cs — Panel subclass with DPs (Direction, JustifyContent, AlignItems, AlignContent, Wrap, ColumnGap, RowGap, FlexPadding) + attached DPs (Grow, Shrink, Basis, AlignSelf, Position, Left, Top, Right, Bottom) + MeasureOverride/ArrangeOverride bridging to Yoga

## Layer 3: Duct Integration

- [x] **3.1** Element.cs — Add FlexElement record + FlexAttached record
- [x] **3.2** Reconciler.Mount.cs — Add MountFlex method + switch case
- [x] **3.3** Reconciler.Update.cs — Add UpdateFlex method + switch case
- [x] **3.4** FlexExtensions.cs — .Flex() extension method
- [x] **3.5** Dsl.cs — Flex(), FlexRow(), FlexColumn() factory methods

## Layer 4: FlexPanelGallery — Standalone XAML Test App

- [x] **4.1** Project setup (csproj, App.xaml, MainWindow shell with NavigationView)
- [x] **4.2** OverviewPage, DirectionPage, WrapPage
- [x] **4.3** JustifyContentPage, AlignItemsPage, GrowShrinkPage
- [x] **4.4** GapPage

## Layer 5: Validation & Polish (future session)

- [x] **5.1** Port key Yoga fixture tests to validate algorithm correctness
- [X] **5.2** Build and run FlexPanelGallery, fix visual issues
- [x] **5.3** Edge cases: RTL, zero-size, overflow, display:none
- [x] **5.4** AbsolutePositionPage and NestedFlexPage gallery pages
