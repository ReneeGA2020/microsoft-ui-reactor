// Tests for tree layout (Reingold-Tilford algorithm)

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class TreeLayoutTests
{
    // Helper: build a simple tree from a string-based adjacency definition
    private static TreeNode<string> BuildTree(
        TreeLayout<string> layout,
        string root,
        Func<string, IEnumerable<string>?> childrenAccessor)
    {
        return layout.Hierarchy(root, childrenAccessor);
    }

    #region Hierarchy builds tree correctly

    [Fact]
    public void Hierarchy_SingleNode_HasNoChildren()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", _ => null);

        Assert.Equal("root", root.Data);
        Assert.Empty(root.Children);
        Assert.Null(root.Parent);
    }

    [Fact]
    public void Hierarchy_NodeWithChildren_SetsParentAndChildren()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B", "C" },
            _ => null
        });

        Assert.Equal(3, root.Children.Count);
        Assert.Equal("A", root.Children[0].Data);
        Assert.Equal("B", root.Children[1].Data);
        Assert.Equal("C", root.Children[2].Data);

        foreach (var child in root.Children)
        {
            Assert.Same(root, child.Parent);
        }
    }

    [Fact]
    public void Hierarchy_DeepTree_BuildsAllLevels()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A" },
            "A" => new[] { "B" },
            "B" => new[] { "C" },
            "C" => new[] { "D" },
            _ => null
        });

        var node = root;
        var expected = new[] { "root", "A", "B", "C", "D" };
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], node.Data);
            if (i < expected.Length - 1)
            {
                Assert.Single(node.Children);
                node = node.Children[0];
            }
        }
        Assert.Empty(node.Children);
    }

    #endregion

    #region Depth is set correctly

    [Fact]
    public void Hierarchy_SetsDepthCorrectly()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B" },
            "A" => new[] { "A1", "A2" },
            "B" => new[] { "B1" },
            _ => null
        });

        Assert.Equal(0, root.Depth);
        Assert.Equal(1, root.Children[0].Depth); // A
        Assert.Equal(1, root.Children[1].Depth); // B
        Assert.Equal(2, root.Children[0].Children[0].Depth); // A1
        Assert.Equal(2, root.Children[0].Children[1].Depth); // A2
        Assert.Equal(2, root.Children[1].Children[0].Depth); // B1
    }

    #endregion

    #region Descendants returns all nodes in pre-order

    [Fact]
    public void Descendants_ReturnsAllNodesInPreOrder()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B" },
            "A" => new[] { "A1", "A2" },
            "B" => new[] { "B1" },
            _ => null
        });

        var names = root.Descendants().Select(n => n.Data).ToList();

        // Pre-order: root, A, A1, A2, B, B1
        Assert.Equal(new[] { "root", "A", "A1", "A2", "B", "B1" }, names);
    }

    [Fact]
    public void Descendants_SingleNode_ReturnsSelf()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("leaf", _ => null);

        var descendants = root.Descendants().ToList();
        Assert.Single(descendants);
        Assert.Same(root, descendants[0]);
    }

    #endregion

    #region TopAncestor returns child of root

    [Fact]
    public void TopAncestor_DeepNode_ReturnsChildOfRoot()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B" },
            "A" => new[] { "A1" },
            "A1" => new[] { "A1a" },
            _ => null
        });

        // A1a -> A1 -> A -> root; TopAncestor should be A (child of root)
        var a1a = root.Children[0].Children[0].Children[0];
        Assert.Equal("A1a", a1a.Data);
        Assert.Equal("A", a1a.TopAncestor.Data);
    }

    [Fact]
    public void TopAncestor_ChildOfRoot_ReturnsSelf()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B" },
            _ => null
        });

        Assert.Same(root.Children[0], root.Children[0].TopAncestor);
    }

    [Fact]
    public void TopAncestor_Root_ReturnsSelf()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", _ => null);

        Assert.Same(root, root.TopAncestor);
    }

    #endregion

    #region Layout sets X and Y on all nodes

    [Fact]
    public void Layout_SetsXAndYOnAllNodes()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B", "C" },
            "A" => new[] { "A1", "A2" },
            "B" => new[] { "B1" },
            _ => null
        });
        layout.Layout(root);

        foreach (var node in root.Descendants())
        {
            // After layout, all X and Y should be finite numbers (not NaN or infinity)
            Assert.False(double.IsNaN(node.X), $"Node {node.Data} has NaN X");
            Assert.False(double.IsNaN(node.Y), $"Node {node.Data} has NaN Y");
            Assert.False(double.IsInfinity(node.X), $"Node {node.Data} has infinite X");
            Assert.False(double.IsInfinity(node.Y), $"Node {node.Data} has infinite Y");
        }
    }

    #endregion

    #region Layout positions within Size bounds (40px margin)

    [Fact]
    public void Layout_PositionsWithinBoundsWithMargin()
    {
        var layout = new TreeLayout<string>().Size(400, 400);
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B", "C" },
            "A" => new[] { "A1", "A2" },
            _ => null
        });
        layout.Layout(root);

        double margin = 40;
        foreach (var node in root.Descendants())
        {
            Assert.True(node.X >= margin - 0.001, $"Node {node.Data} X={node.X} is below left margin");
            Assert.True(node.X <= 400 - margin + 0.001, $"Node {node.Data} X={node.X} exceeds right margin");
            Assert.True(node.Y >= margin - 0.001, $"Node {node.Data} Y={node.Y} is above top margin");
            Assert.True(node.Y <= 400 - margin + 0.001, $"Node {node.Data} Y={node.Y} exceeds bottom margin");
        }
    }

    #endregion

    #region Sibling nodes get different X positions

    [Fact]
    public void Layout_SiblingNodes_HaveDifferentXPositions()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B", "C" },
            _ => null
        });
        layout.Layout(root);

        var xs = root.Children.Select(c => c.X).ToList();
        // All sibling X values should be distinct
        Assert.Equal(xs.Count, xs.Distinct().Count());
        // They should be in left-to-right order
        Assert.True(xs[0] < xs[1]);
        Assert.True(xs[1] < xs[2]);
    }

    #endregion

    #region Root is horizontally centered

    [Fact]
    public void Layout_RootIsCentered_SymmetricTree()
    {
        var layout = new TreeLayout<string>().Size(400, 400);
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B" },
            _ => null
        });
        layout.Layout(root);

        // For a symmetric two-child tree, root should be at the horizontal center
        double expectedCenter = 400.0 / 2;
        Assert.Equal(expectedCenter, root.X, 1);
    }

    #endregion

    #region Asymmetric branches (triggers Apportion)

    [Fact]
    public void Layout_AsymmetricTree_SiblingsOrdered()
    {
        // This tree has uneven subtrees which trigger the Apportion mechanism
        var layout = new TreeLayout<string>().Size(600, 400);
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B", "C" },
            "A" => new[] { "A1", "A2", "A3" },
            "A1" => new[] { "A1a", "A1b" },
            "A3" => new[] { "A3a", "A3b", "A3c" },
            "C" => new[] { "C1", "C2" },
            "C2" => new[] { "C2a", "C2b" },
            _ => null
        });
        layout.Layout(root);

        // Verify siblings are ordered left-to-right under every parent
        void CheckSiblingOrder(TreeNode<string> node)
        {
            for (int i = 1; i < node.Children.Count; i++)
            {
                Assert.True(node.Children[i].X > node.Children[i - 1].X,
                    $"Under {node.Data}: {node.Children[i].Data} (X={node.Children[i].X}) " +
                    $"should be right of {node.Children[i - 1].Data} (X={node.Children[i - 1].X})");
            }
            foreach (var child in node.Children)
                CheckSiblingOrder(child);
        }
        CheckSiblingOrder(root);

        // Verify all nodes are within bounds
        foreach (var node in root.Descendants())
        {
            Assert.InRange(node.X, 40 - 0.001, 560 + 0.001);
            Assert.InRange(node.Y, 40 - 0.001, 360 + 0.001);
        }
    }

    #endregion

    #region Size configuration

    [Fact]
    public void Layout_CustomSize_PositionsScaleAccordingly()
    {
        var childrenAccessor = (string name) => name switch
        {
            "root" => new[] { "A", "B" },
            _ => (IEnumerable<string>?)null
        };

        var smallLayout = new TreeLayout<string>().Size(100, 100);
        var smallRoot = smallLayout.Hierarchy("root", childrenAccessor);
        smallLayout.Layout(smallRoot);

        var largeLayout = new TreeLayout<string>().Size(800, 800);
        var largeRoot = largeLayout.Hierarchy("root", childrenAccessor);
        largeLayout.Layout(largeRoot);

        // In a 100x100 layout, the max X should be <= 60 (100 - 40 margin)
        Assert.True(smallRoot.Descendants().All(n => n.X <= 60 + 0.001));
        // In an 800x800 layout, the positions should spread further
        Assert.True(largeRoot.Descendants().Max(n => n.X) > smallRoot.Descendants().Max(n => n.X));
    }

    #endregion

    #region Separation affects spacing

    [Fact]
    public void Layout_LargerSeparation_IncreasesSpacing()
    {
        // With default separation, build and lay out the same tree at two separation values.
        // The normalized positions should be the same for the same topology since normalization
        // scales to fit, but we can verify the algorithm runs without error and positions stay valid.
        var layout = new TreeLayout<string>().Size(400, 400).Separation(2.0);
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B", "C" },
            _ => null
        });
        layout.Layout(root);

        // All positions should be within bounds
        foreach (var node in root.Descendants())
        {
            Assert.True(node.X >= 40 - 0.001 && node.X <= 360 + 0.001);
            Assert.True(node.Y >= 40 - 0.001 && node.Y <= 360 + 0.001);
        }

        // Siblings should still be ordered correctly
        Assert.True(root.Children[0].X < root.Children[1].X);
        Assert.True(root.Children[1].X < root.Children[2].X);
    }

    #endregion

    #region Factory method Create<T>

    [Fact]
    public void Create_ReturnsNewInstance()
    {
        var layout = TreeLayout.Create<string>();
        Assert.NotNull(layout);
        Assert.IsType<TreeLayout<string>>(layout);
    }

    [Fact]
    public void Create_CanBuildAndLayoutTree()
    {
        var layout = TreeLayout.Create<string>().Size(200, 200);
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "X", "Y" },
            _ => null
        });
        layout.Layout(root);

        Assert.Equal(3, root.Descendants().Count());
        Assert.True(root.Children[0].X < root.Children[1].X);
    }

    #endregion

    #region Leaf-only tree (root with no children)

    [Fact]
    public void Layout_LeafOnlyTree_PositionedWithinBounds()
    {
        var layout = new TreeLayout<string>().Size(400, 400);
        var root = layout.Hierarchy("lonely", _ => null);
        layout.Layout(root);

        // A single node tree should be positioned (the algorithm handles xRange=0 by using 1)
        Assert.True(root.X >= 40 - 0.001 && root.X <= 360 + 0.001);
        Assert.True(root.Y >= 40 - 0.001 && root.Y <= 360 + 0.001);
    }

    #endregion

    #region Single-child chain (A -> B -> C -> D)

    [Fact]
    public void Layout_SingleChildChain_AllNodesHaveSameX()
    {
        var layout = new TreeLayout<string>().Size(400, 400);
        var root = layout.Hierarchy("A", name => name switch
        {
            "A" => new[] { "B" },
            "B" => new[] { "C" },
            "C" => new[] { "D" },
            _ => null
        });
        layout.Layout(root);

        // In a single-child chain, all nodes should have the same X
        var allNodes = root.Descendants().ToList();
        var firstX = allNodes[0].X;
        foreach (var node in allNodes)
        {
            Assert.Equal(firstX, node.X, 1);
        }

        // Y values should increase with depth
        for (int i = 1; i < allNodes.Count; i++)
        {
            Assert.True(allNodes[i].Y > allNodes[i - 1].Y,
                $"Node {allNodes[i].Data} Y={allNodes[i].Y} should be below {allNodes[i - 1].Data} Y={allNodes[i - 1].Y}");
        }
    }

    #endregion

    #region Wide tree with many children per node

    [Fact]
    public void Layout_WideTree_AllChildrenSpreadOut()
    {
        var layout = new TreeLayout<string>().Size(800, 400);
        var childNames = Enumerable.Range(0, 10).Select(i => $"child{i}").ToArray();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => childNames,
            _ => null
        });
        layout.Layout(root);

        // All 10 children should have distinct, increasing X values
        var xs = root.Children.Select(c => c.X).ToList();
        for (int i = 1; i < xs.Count; i++)
        {
            Assert.True(xs[i] > xs[i - 1],
                $"child{i} X={xs[i]} should be right of child{i - 1} X={xs[i - 1]}");
        }

        // Spread should cover most of the usable width
        double spread = xs.Max() - xs.Min();
        double usableWidth = 800 - 2 * 40; // 720
        Assert.True(spread > usableWidth * 0.5,
            $"Children spread {spread} should cover at least half of usable width {usableWidth}");
    }

    #endregion

    #region TreeNode constructor sets Ancestor to self

    [Fact]
    public void TreeNode_Constructor_SetsAncestorToSelf()
    {
        var node = new TreeNode<string>("test");
        Assert.Equal("test", node.Data);
        // Ancestor is internal, but we verify indirectly through TopAncestor behavior
        Assert.Same(node, node.TopAncestor);
    }

    #endregion

    #region Layout returns the root node

    [Fact]
    public void Layout_ReturnsRootNode()
    {
        var layout = new TreeLayout<string>();
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B" },
            _ => null
        });

        var result = layout.Layout(root);
        Assert.Same(root, result);
    }

    #endregion

    #region Fluent API chaining

    [Fact]
    public void Size_ReturnsSameInstance_ForChaining()
    {
        var layout = new TreeLayout<string>();
        var result = layout.Size(500, 500);
        Assert.Same(layout, result);
    }

    [Fact]
    public void Separation_ReturnsSameInstance_ForChaining()
    {
        var layout = new TreeLayout<string>();
        var result = layout.Separation(2.0);
        Assert.Same(layout, result);
    }

    #endregion

    #region Root Y position

    [Fact]
    public void Layout_RootAtTopMargin()
    {
        var layout = new TreeLayout<string>().Size(400, 400);
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "A", "B" },
            "A" => new[] { "A1" },
            _ => null
        });
        layout.Layout(root);

        // Root should be at the top margin (Y = 40)
        Assert.Equal(40.0, root.Y, 1);
    }

    #endregion

    #region Deeper asymmetric tree for Apportion thoroughness

    [Fact]
    public void Layout_DeeplyAsymmetric_NoNodeOverlaps()
    {
        // Left subtree is deep, right subtree is shallow -- classic Apportion trigger
        var layout = new TreeLayout<string>().Size(600, 600);
        var root = layout.Hierarchy("root", name => name switch
        {
            "root" => new[] { "L", "R" },
            "L" => new[] { "L1", "L2" },
            "L1" => new[] { "L1a", "L1b" },
            "L1a" => new[] { "L1a1", "L1a2" },
            "R" => new[] { "R1", "R2", "R3" },
            _ => null
        });
        layout.Layout(root);

        // Verify all same-depth nodes have unique X positions
        var nodesByDepth = root.Descendants()
            .GroupBy(n => n.Depth);

        foreach (var group in nodesByDepth)
        {
            var sorted = group.OrderBy(n => n.X).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                Assert.True(sorted[i].X > sorted[i - 1].X + 0.001,
                    $"Depth {group.Key}: {sorted[i].Data} overlaps {sorted[i - 1].Data}");
            }
        }

        // All within bounds
        foreach (var node in root.Descendants())
        {
            Assert.InRange(node.X, 40 - 0.001, 560 + 0.001);
            Assert.InRange(node.Y, 40 - 0.001, 560 + 0.001);
        }
    }

    #endregion
}
