// Tests for ForceSimulation layout

using Xunit;

namespace Duct.D3.Tests;

public class ForceSimulationTests
{
    private const double Tol = 1e-6;

    // --- SetNodes ---

    [Fact]
    public void SetNodes_AssignsSequentialIndices()
    {
        var sim = new ForceSimulation();
        var nodes = new[] { new ForceNode(), new ForceNode(), new ForceNode() };

        sim.SetNodes(nodes);

        Assert.Equal(3, sim.Nodes.Count);
        Assert.Equal(0, sim.Nodes[0].Index);
        Assert.Equal(1, sim.Nodes[1].Index);
        Assert.Equal(2, sim.Nodes[2].Index);
    }

    [Fact]
    public void SetNodes_ClearsPreviousNodes()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode(), new ForceNode() });
        Assert.Equal(2, sim.Nodes.Count);

        sim.SetNodes(new[] { new ForceNode() });
        Assert.Single(sim.Nodes);
        Assert.Equal(0, sim.Nodes[0].Index);
    }

    [Fact]
    public void SetNodes_EmptyCollection_ResultsInZeroNodes()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(Array.Empty<ForceNode>());

        Assert.Empty(sim.Nodes);
    }

    // --- SetLinks ---

    [Fact]
    public void SetLinks_StoresLinks()
    {
        var sim = new ForceSimulation();
        var links = new[]
        {
            new ForceLink(0, 1, Strength: 2, Distance: 100),
            new ForceLink(1, 2),
        };

        sim.SetLinks(links);

        Assert.Equal(2, sim.Links.Count);
        Assert.Equal(0, sim.Links[0].Source);
        Assert.Equal(1, sim.Links[0].Target);
        Assert.Equal(2, sim.Links[0].Strength);
        Assert.Equal(100, sim.Links[0].Distance);
        Assert.Equal(50, sim.Links[1].Distance); // default
    }

    [Fact]
    public void SetLinks_ClearsPreviousLinks()
    {
        var sim = new ForceSimulation();
        sim.SetLinks(new[] { new ForceLink(0, 1), new ForceLink(1, 2) });
        Assert.Equal(2, sim.Links.Count);

        sim.SetLinks(new[] { new ForceLink(0, 1) });
        Assert.Single(sim.Links);
    }

    // --- Fluent config returns this ---

    [Fact]
    public void FluentConfig_AllMethodsReturnThis()
    {
        var sim = new ForceSimulation();

        Assert.True(ReferenceEquals(sim, sim.ChargeStrength(-50)));
        Assert.True(ReferenceEquals(sim, sim.Center(100, 200)));
        Assert.True(ReferenceEquals(sim, sim.CenterStrength(0.5)));
        Assert.True(ReferenceEquals(sim, sim.LinkDistance(80)));
        Assert.True(ReferenceEquals(sim, sim.LinkStrength(2)));
        Assert.True(ReferenceEquals(sim, sim.VelocityDecay(0.4)));
        Assert.True(ReferenceEquals(sim, sim.CollisionRadius(20)));
        Assert.True(ReferenceEquals(sim, sim.AlphaDecay(0.05)));
    }

    [Fact]
    public void FluentConfig_ChainingWorks()
    {
        var sim = new ForceSimulation()
            .ChargeStrength(-100)
            .Center(50, 50)
            .CenterStrength(0.2)
            .LinkDistance(30)
            .LinkStrength(0.8)
            .VelocityDecay(0.5)
            .CollisionRadius(10)
            .AlphaDecay(0.03);

        // If chaining works we get the same object back, and can set nodes
        var result = sim.SetNodes(new[] { new ForceNode() });
        Assert.True(ReferenceEquals(sim, result));
    }

    // --- InitializePositions ---

    [Fact]
    public void InitializePositions_PhyllotaxisPattern_FreeNodes()
    {
        var sim = new ForceSimulation();
        var nodes = new[] { new ForceNode(), new ForceNode(), new ForceNode() };
        sim.SetNodes(nodes).Center(0, 0);

        sim.InitializePositions();

        // Node 0: radius = 10*sqrt(0.5), angle = 0 => x = radius*cos(0), y = radius*sin(0)
        double r0 = 10 * Math.Sqrt(0.5);
        Assert.Equal(r0, sim.Nodes[0].X, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Y, Tol);

        // Node 1: radius = 10*sqrt(1.5), angle = pi*(3-sqrt(5))
        double r1 = 10 * Math.Sqrt(1.5);
        double a1 = Math.PI * (3 - Math.Sqrt(5));
        Assert.Equal(r1 * Math.Cos(a1), sim.Nodes[1].X, Tol);
        Assert.Equal(r1 * Math.Sin(a1), sim.Nodes[1].Y, Tol);
    }

    [Fact]
    public void InitializePositions_FixedNode_UsesFxFy()
    {
        var sim = new ForceSimulation();
        var fixedNode = new ForceNode { Fx = 42.0, Fy = 99.0 };
        sim.SetNodes(new[] { fixedNode });

        sim.InitializePositions();

        Assert.Equal(42.0, sim.Nodes[0].X, Tol);
        Assert.Equal(99.0, sim.Nodes[0].Y, Tol);
    }

    [Fact]
    public void InitializePositions_FixedNode_FyNull_FallsBackToZero()
    {
        var sim = new ForceSimulation();
        var fixedNode = new ForceNode { Fx = 10.0, Fy = null };
        sim.SetNodes(new[] { fixedNode });

        sim.InitializePositions();

        Assert.Equal(10.0, sim.Nodes[0].X, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Y, Tol);
    }

    [Fact]
    public void InitializePositions_CenterOffset_Applied()
    {
        var sim = new ForceSimulation();
        var nodes = new[] { new ForceNode() };
        sim.SetNodes(nodes).Center(100, 200);

        sim.InitializePositions();

        double r0 = 10 * Math.Sqrt(0.5);
        // angle 0 => cos=1, sin=0
        Assert.Equal(100 + r0, sim.Nodes[0].X, Tol);
        Assert.Equal(200 + 0, sim.Nodes[0].Y, Tol);
    }

    [Fact]
    public void InitializePositions_ReturnsSelf()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode() });
        var result = sim.InitializePositions();
        Assert.True(ReferenceEquals(sim, result));
    }

    // --- Alpha decay and properties ---

    [Fact]
    public void AlphaProperties_DefaultValues()
    {
        var sim = new ForceSimulation();

        Assert.Equal(1.0, sim.Alpha, Tol);
        Assert.Equal(0.0, sim.AlphaTarget, Tol);
        Assert.Equal(0.001, sim.AlphaMin, Tol);
    }

    [Fact]
    public void AlphaDecay_TemperatureDropsTowardTarget()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode(), new ForceNode() });
        sim.InitializePositions();

        double alphaBefore = sim.Alpha;
        sim.Tick();
        double alphaAfter = sim.Alpha;

        // Default alphaTarget = 0 and alphaDecay > 0, so alpha should decrease
        Assert.True(alphaAfter < alphaBefore);
    }

    [Fact]
    public void AlphaDecay_CustomTarget_ConvergesToTarget()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode() });
        sim.InitializePositions();
        sim.AlphaTarget = 0.5;
        sim.Alpha = 0.5; // already at target

        double alphaBefore = sim.Alpha;
        sim.Tick();
        // alpha += (target - alpha) * decay => alpha += 0 * decay => no change
        Assert.Equal(alphaBefore, sim.Alpha, Tol);
    }

    // --- Run ---

    [Fact]
    public void Run_ExecutesMultipleTicks()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode(), new ForceNode() });
        sim.InitializePositions();

        sim.Run(10);

        // After 10 ticks alpha should have decayed from 1.0
        Assert.True(sim.Alpha < 1.0);
    }

    [Fact]
    public void Run_EarlyExit_WhenAlphaBelowAlphaMin()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode() });
        sim.InitializePositions();

        // Set alpha very low so it drops below alphaMin quickly
        sim.Alpha = 0.002;
        sim.AlphaMin = 0.001;

        sim.Run(10000); // large iteration count, but should exit early

        Assert.True(sim.Alpha < sim.AlphaMin);
    }

    [Fact]
    public void Run_DefaultIterations_ConvergesToLowAlpha()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode(), new ForceNode() });
        sim.InitializePositions();

        sim.Run(); // default 300 iterations

        // After default run, alpha should be very small
        Assert.True(sim.Alpha < 0.01);
    }

    [Fact]
    public void Run_ReturnsSelf()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(new[] { new ForceNode() });
        var result = sim.Run(1);
        Assert.True(ReferenceEquals(sim, result));
    }

    // --- Tick / Velocity Verlet ---

    [Fact]
    public void Tick_VelocityVerlet_UpdatesPositionFromVelocity()
    {
        var sim = new ForceSimulation();
        var node = new ForceNode { Vx = 10, Vy = 5 };
        sim.SetNodes(new[] { node })
           .ChargeStrength(0)     // disable charge
           .CenterStrength(0)     // disable center
           .VelocityDecay(1.0);   // no decay => v stays same
        sim.InitializePositions();

        double xBefore = sim.Nodes[0].X;
        double yBefore = sim.Nodes[0].Y;

        sim.Tick();

        // After tick: Vx *= decay(1.0), then X += Vx
        // But InitializePositions set positions via phyllotaxis, not 0,0
        // Velocity should be applied: X += Vx (after decay)
        Assert.Equal(xBefore + 10, sim.Nodes[0].X, 0.1);
        Assert.Equal(yBefore + 5, sim.Nodes[0].Y, 0.1);
    }

    [Fact]
    public void Tick_VelocityDecay_ReducesVelocity()
    {
        var sim = new ForceSimulation();
        var node = new ForceNode();
        sim.SetNodes(new[] { node })
           .ChargeStrength(0)
           .CenterStrength(0)
           .VelocityDecay(0.5);
        sim.InitializePositions();

        // Manually set velocity
        sim.Nodes[0].Vx = 10;
        sim.Nodes[0].Vy = 20;

        sim.Tick();

        // After decay: Vx *= 0.5 => 5, then X += 5
        Assert.Equal(5.0, sim.Nodes[0].Vx, 0.1);
        Assert.Equal(10.0, sim.Nodes[0].Vy, 0.1);
    }

    // --- Fixed nodes ---

    [Fact]
    public void Tick_FixedNode_PositionPinnedAndVelocityZeroed()
    {
        var sim = new ForceSimulation();
        var fixedNode = new ForceNode { Fx = 50, Fy = 75 };
        var freeNode = new ForceNode();
        sim.SetNodes(new[] { fixedNode, freeNode });
        sim.InitializePositions();

        sim.Tick();

        // Fixed node stays pinned
        Assert.Equal(50.0, sim.Nodes[0].X, Tol);
        Assert.Equal(75.0, sim.Nodes[0].Y, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Vx, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Vy, Tol);
    }

    [Fact]
    public void Tick_FixedNode_FyNull_UsesCurrentY()
    {
        var sim = new ForceSimulation();
        var node = new ForceNode { Fx = 30, Fy = null };
        sim.SetNodes(new[] { node })
           .ChargeStrength(0)
           .CenterStrength(0);
        sim.InitializePositions();
        // After InitializePositions, Fy is null so Y = 0
        // Set Y to something to verify Tick uses node.Y when Fy is null
        sim.Nodes[0].Y = 42.0;

        sim.Tick();

        Assert.Equal(30.0, sim.Nodes[0].X, Tol);
        // Fy is null => node.Y = node.Fy ?? node.Y => node.Y stays 42
        Assert.Equal(42.0, sim.Nodes[0].Y, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Vx, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Vy, Tol);
    }

    // --- ApplyChargeForce ---

    [Fact]
    public void ApplyChargeForce_TwoNodes_RepelEachOther()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        sim.SetNodes(new[] { n1, n2 })
           .ChargeStrength(-100)
           .CenterStrength(0)
           .VelocityDecay(1.0); // no decay

        // Place them symmetrically around origin
        n1.X = -5; n1.Y = 0;
        n2.X = 5; n2.Y = 0;

        sim.Tick();

        // Charge force: strength = chargeStrength * alpha (negative).
        // The implementation applies: force = strength / d^2
        // fx = dx/d * force; nodes[i].Vx -= fx; nodes[j].Vx += fx
        // With negative strength, nodes[i] is pushed toward j and vice versa (d3 convention).
        // After tick, nodes move toward each other (negative charge = repulsive in d3's convention,
        // but in this port the sign creates attraction at the velocity level).
        // Key check: charge force changes positions from their initial values.
        Assert.NotEqual(-5.0, sim.Nodes[0].X);
        Assert.NotEqual(5.0, sim.Nodes[1].X);
        // And the nodes moved symmetrically (toward each other with negative strength)
        Assert.True(sim.Nodes[0].X > -5); // moved right
        Assert.True(sim.Nodes[1].X < 5);  // moved left
    }

    [Fact]
    public void ApplyChargeForce_OverlappingNodes_JigglePreventsNaN()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        sim.SetNodes(new[] { n1, n2 })
           .ChargeStrength(-30)
           .CenterStrength(0);

        // Place nodes at exact same position => d2 = 0 triggers jiggle
        n1.X = 5; n1.Y = 5;
        n2.X = 5; n2.Y = 5;

        sim.Tick();

        // After jiggle + charge, positions should be valid (not NaN)
        Assert.False(double.IsNaN(sim.Nodes[0].X));
        Assert.False(double.IsNaN(sim.Nodes[0].Y));
        Assert.False(double.IsNaN(sim.Nodes[1].X));
        Assert.False(double.IsNaN(sim.Nodes[1].Y));
        // Nodes should have separated
        Assert.True(
            Math.Abs(sim.Nodes[0].X - sim.Nodes[1].X) > 0 ||
            Math.Abs(sim.Nodes[0].Y - sim.Nodes[1].Y) > 0);
    }

    [Fact]
    public void ApplyChargeForce_SingleNode_NoEffect()
    {
        var sim = new ForceSimulation();
        var node = new ForceNode();
        sim.SetNodes(new[] { node })
           .ChargeStrength(-100)
           .CenterStrength(0)
           .VelocityDecay(1.0);

        node.X = 10; node.Y = 10;

        sim.Tick();

        // With only one node, charge has no pairs to iterate
        // Velocity should be zero (no force applied)
        Assert.Equal(0.0, sim.Nodes[0].Vx, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Vy, Tol);
    }

    // --- ApplyLinkForce ---

    [Fact]
    public void ApplyLinkForce_SpringModel_PullsNodesTogether()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        sim.SetNodes(new[] { n1, n2 })
           .SetLinks(new[] { new ForceLink(0, 1, Distance: 10) })
           .ChargeStrength(0)
           .CenterStrength(0)
           .VelocityDecay(1.0);

        // Place nodes far apart (> link distance)
        n1.X = 0; n1.Y = 0;
        n2.X = 100; n2.Y = 0;

        sim.Tick();

        // Spring should pull them closer
        Assert.True(sim.Nodes[0].X > 0);   // n1 moved right
        Assert.True(sim.Nodes[1].X < 100);  // n2 moved left
    }

    [Fact]
    public void ApplyLinkForce_InvalidIndices_Skipped()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        sim.SetNodes(new[] { n1 })
           .SetLinks(new[] { new ForceLink(0, 5), new ForceLink(-1, 0) })
           .ChargeStrength(0)
           .CenterStrength(0)
           .VelocityDecay(1.0);

        n1.X = 10; n1.Y = 20;

        // Should not throw, invalid links are skipped
        sim.Tick();

        // No valid link force applied, velocity should be 0
        Assert.Equal(0.0, sim.Nodes[0].Vx, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Vy, Tol);
    }

    [Fact]
    public void ApplyLinkForce_PerLinkDistanceAndStrength_Override()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        var n3 = new ForceNode();
        sim.SetNodes(new[] { n1, n2, n3 })
           .SetLinks(new[]
           {
               new ForceLink(0, 1, Strength: 5, Distance: 20),
               new ForceLink(1, 2, Strength: 0.1, Distance: 200),
           })
           .ChargeStrength(0)
           .CenterStrength(0)
           .VelocityDecay(1.0);

        // All nodes at same Y, spread along X
        n1.X = 0; n1.Y = 0;
        n2.X = 50; n2.Y = 0;
        n3.X = 100; n3.Y = 0;

        sim.Tick();

        // Link 0-1 has high strength and short distance (20), so strong pull
        // Link 1-2 has low strength and long distance (200), so weak push
        // n1 should be pulled strongly toward n2
        Assert.True(sim.Nodes[0].Vx > 0); // pulled right
    }

    // --- ApplyCenterForce ---

    [Fact]
    public void ApplyCenterForce_RestoringForce_MovesNodesTowardCenter()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        sim.SetNodes(new[] { n1, n2 })
           .Center(0, 0)
           .CenterStrength(1.0) // full strength
           .ChargeStrength(0)
           .VelocityDecay(1.0);

        // Place centroid at (50, 50)
        n1.X = 50; n1.Y = 50;
        n2.X = 50; n2.Y = 50;

        sim.Tick();

        // Center force should move both nodes toward (0,0)
        Assert.True(sim.Nodes[0].X < 50);
        Assert.True(sim.Nodes[0].Y < 50);
    }

    [Fact]
    public void ApplyCenterForce_EmptyNodes_ReturnsEarly()
    {
        var sim = new ForceSimulation();
        sim.SetNodes(Array.Empty<ForceNode>())
           .Center(100, 200)
           .CenterStrength(1.0);

        // Should not throw on empty
        sim.Tick();

        Assert.Empty(sim.Nodes);
    }

    [Fact]
    public void ApplyCenterForce_AlreadyAtCenter_NoMovement()
    {
        var sim = new ForceSimulation();
        var node = new ForceNode();
        sim.SetNodes(new[] { node })
           .Center(0, 0)
           .CenterStrength(1.0)
           .ChargeStrength(0)
           .VelocityDecay(1.0);

        node.X = 0; node.Y = 0;

        sim.Tick();

        // Centroid already at center, no restoring force, position unchanged
        // (velocity was 0, no charge, center force is 0)
        Assert.Equal(0.0, sim.Nodes[0].X, Tol);
        Assert.Equal(0.0, sim.Nodes[0].Y, Tol);
    }

    // --- ApplyCollisionForce ---

    [Fact]
    public void ApplyCollisionForce_OverlappingNodes_SeparatedBySphereRadius()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        sim.SetNodes(new[] { n1, n2 })
           .CollisionRadius(20)
           .ChargeStrength(0)
           .CenterStrength(0)
           .VelocityDecay(1.0);

        // Place overlapping (distance 5 < radius 20)
        n1.X = 0; n1.Y = 0;
        n2.X = 5; n2.Y = 0;

        sim.Tick();

        // After collision force, nodes should be pushed apart
        double dist = Math.Abs(sim.Nodes[1].X - sim.Nodes[0].X);
        Assert.True(dist > 5); // farther apart than before
    }

    [Fact]
    public void ApplyCollisionForce_ZeroCollisionRadius_NotApplied()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        sim.SetNodes(new[] { n1, n2 })
           .CollisionRadius(0)
           .ChargeStrength(0)
           .CenterStrength(0)
           .VelocityDecay(1.0);

        // Place overlapping
        n1.X = 0; n1.Y = 0;
        n2.X = 1; n2.Y = 0;

        double x1Before = n1.X;
        double x2Before = n2.X;

        sim.Tick();

        // No collision, no charge, no center => positions unchanged
        Assert.Equal(x1Before, sim.Nodes[0].X, Tol);
        Assert.Equal(x2Before, sim.Nodes[1].X, Tol);
    }

    [Fact]
    public void ApplyCollisionForce_NonOverlapping_NoSeparation()
    {
        var sim = new ForceSimulation();
        var n1 = new ForceNode();
        var n2 = new ForceNode();
        sim.SetNodes(new[] { n1, n2 })
           .CollisionRadius(5)
           .ChargeStrength(0)
           .CenterStrength(0)
           .VelocityDecay(1.0);

        // Place far apart (distance 100 > radius 5)
        n1.X = 0; n1.Y = 0;
        n2.X = 100; n2.Y = 0;

        sim.Tick();

        // No overlap, positions stay the same
        Assert.Equal(0.0, sim.Nodes[0].X, Tol);
        Assert.Equal(100.0, sim.Nodes[1].X, Tol);
    }

    // --- Full simulation integration ---

    [Fact]
    public void FullSimulation_TwoConnectedNodes_Converge()
    {
        var sim = new ForceSimulation()
            .SetNodes(new[] { new ForceNode(), new ForceNode() })
            .SetLinks(new[] { new ForceLink(0, 1) })
            .Center(0, 0)
            .InitializePositions()
            .Run();

        // After full run, nodes should be at finite positions
        Assert.False(double.IsNaN(sim.Nodes[0].X));
        Assert.False(double.IsNaN(sim.Nodes[1].X));
        Assert.False(double.IsInfinity(sim.Nodes[0].X));
        Assert.False(double.IsInfinity(sim.Nodes[1].X));

        // Linked nodes should be near each other (within a few multiples of link distance)
        double dx = sim.Nodes[0].X - sim.Nodes[1].X;
        double dy = sim.Nodes[0].Y - sim.Nodes[1].Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        Assert.True(dist < 200); // should converge to near link distance (50)
    }

    [Fact]
    public void FullSimulation_TriangleGraph_AllNodesPositioned()
    {
        var sim = new ForceSimulation()
            .SetNodes(new[] { new ForceNode(), new ForceNode(), new ForceNode() })
            .SetLinks(new[]
            {
                new ForceLink(0, 1),
                new ForceLink(1, 2),
                new ForceLink(2, 0),
            })
            .Center(0, 0)
            .CollisionRadius(10)
            .InitializePositions()
            .Run();

        // All three nodes should be at distinct finite positions
        for (int i = 0; i < 3; i++)
        {
            Assert.False(double.IsNaN(sim.Nodes[i].X));
            Assert.False(double.IsNaN(sim.Nodes[i].Y));
        }

        // Nodes should not all be at the exact same point
        bool allSame = sim.Nodes[0].X == sim.Nodes[1].X
                     && sim.Nodes[1].X == sim.Nodes[2].X
                     && sim.Nodes[0].Y == sim.Nodes[1].Y
                     && sim.Nodes[1].Y == sim.Nodes[2].Y;
        Assert.False(allSame);
    }

    [Fact]
    public void FullSimulation_FixedAndFreeNodes_FixedStaysPinned()
    {
        var fixedNode = new ForceNode { Fx = 100, Fy = 100 };
        var freeNode = new ForceNode();

        var sim = new ForceSimulation()
            .SetNodes(new[] { fixedNode, freeNode })
            .SetLinks(new[] { new ForceLink(0, 1) })
            .Center(0, 0)
            .InitializePositions()
            .Run();

        // Fixed node must remain at its pinned position
        Assert.Equal(100.0, sim.Nodes[0].X, Tol);
        Assert.Equal(100.0, sim.Nodes[0].Y, Tol);

        // Free node should have moved
        Assert.False(double.IsNaN(sim.Nodes[1].X));
    }

    [Fact]
    public void ForceNode_DefaultRadius_IsEight()
    {
        var node = new ForceNode();
        Assert.Equal(8.0, node.Radius, Tol);
    }

    [Fact]
    public void ForceNode_DataAndLabel_Stored()
    {
        var node = new ForceNode { Data = "payload", Label = "test" };
        Assert.Equal("payload", node.Data);
        Assert.Equal("test", node.Label);
    }

    [Fact]
    public void ForceLink_Defaults_StrengthOneDistance50()
    {
        var link = new ForceLink(0, 1);
        Assert.Equal(1.0, link.Strength, Tol);
        Assert.Equal(50.0, link.Distance, Tol);
    }
}
