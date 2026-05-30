using System.Linq;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using static Microsoft.UI.Reactor.Factories;
using WinXC = Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Fixtures for the typed, data-driven <c>TreeView&lt;T&gt;</c> — the
/// hierarchical peer of <c>ListView&lt;T&gt;</c> that closes issue #447
/// ("TreeViewNodeData.ContentElement renders blank"). The legacy node-mode
/// TreeView stringifies its content and cannot host a pre-built UIElement;
/// the typed peer renders each node from a <c>data → Element</c> viewBuilder
/// (the WinUI ItemTemplate equivalent), hosted via a ContentControl template.
/// </summary>
internal static class TemplatedTreeViewFixtures
{
    // A discriminated file-system model — folders nest, files are leaves.
    private abstract record FsNode(string Id);
    private record FsFolder(string Id, string Name, FsNode[] Children) : FsNode(Id);
    private record FsFile(string Id, string Name, string Ext) : FsNode(Id);

    private static FsNode[] SampleTree() =>
    [
        new FsFolder("docs", "Documents",
        [
            new FsFile("readme", "readme", "md"),
            new FsFile("notes", "notes", "txt"),
        ]),
        new FsFolder("pics", "Pictures",
        [
            new FsFile("logo", "logo", "png"),
        ]),
    ];

    private static IReadOnlyList<FsNode>? ChildrenOf(FsNode n) =>
        n is FsFolder f ? f.Children : null;

    // viewBuilder = ItemTemplateSelector-as-a-switch. Folders and files get
    // visibly distinct visuals (the "[D]" / "[F]" prefix is the tell).
    private static Element BuildNodeView(FsNode n) => n switch
    {
        FsFolder f => HStack(TextBlock("[D]"), TextBlock(f.Name)),
        FsFile file => HStack(TextBlock("[F]"), TextBlock($"{file.Name}.{file.Ext}")),
        _ => TextBlock("?"),
    };

    // Per-node views host into their containers when the TreeView realizes them,
    // which lands on a dispatcher cycle after mount — and the runtime decides how
    // many pump cycles that takes (the NativeAOT host consistently needs one more
    // than JIT). Pump render passes until the condition holds rather than
    // asserting after a single Render(); returns false if it never does.
    private static async Task<bool> WaitFor(Func<bool> condition, int maxPasses = 15)
    {
        for (int i = 0; i < maxPasses; i++)
        {
            if (condition()) return true;
            await Harness.Render();
        }
        return condition();
    }

    // ── 1. Rich content actually renders (the core #447 win) ──────────────
    internal class RendersRichContent(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(_ =>
                VStack(
                    TextBlock("File Explorer"),
                    TreeView(SampleTree(),
                        keySelector: n => n.Id,
                        childrenSelector: ChildrenOf,
                        viewBuilder: BuildNodeView)
                        // Expand folders so their child views realize.
                        with { IsExpanded = n => n is FsFolder }
                ).Height(400)
            );

            await Harness.Render();

            H.Check("TTV_RendersRichContent_TreeViewCreated",
                H.FindControl<WinXC.TreeView>(_ => true) is not null);

            // The node's view is a live HStack of TextBlocks — not a stringified
            // blank row. Finding the folder name proves the content hosted.
            H.Check("TTV_RendersRichContent_RootNodeVisible",
                await WaitFor(() => H.FindTextContaining("Documents") is not null));

            // The "[D]" prefix only exists inside the rich per-node template —
            // a stringified node could never produce it.
            H.Check("TTV_RendersRichContent_RichTemplateHosted",
                await WaitFor(() => H.FindText("[D]") is not null));

            // Expanded child leaf renders too.
            H.Check("TTV_RendersRichContent_ChildLeafVisible",
                await WaitFor(() => H.FindTextContaining("readme.md") is not null));
        }
    }

    // ── 2. Heterogeneous nodes → per-shape templates ──────────────────────
    internal class HeterogeneousTemplates(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(_ =>
                TreeView(SampleTree(),
                    keySelector: n => n.Id,
                    childrenSelector: ChildrenOf,
                    viewBuilder: BuildNodeView)
                    with { IsExpanded = n => n is FsFolder }
            );

            await Harness.Render();

            // Both the folder template ("[D]") and the file template ("[F]")
            // are realized from the single switch-based viewBuilder.
            H.Check("TTV_Heterogeneous_FolderTemplate", await WaitFor(() => H.FindText("[D]") is not null));
            H.Check("TTV_Heterogeneous_FileTemplate", await WaitFor(() => H.FindText("[F]") is not null));
        }
    }

    // ── 3. Keyed update reconcile — in-place rename of a matched node ──────
    // Structure is stable across the flip (same keys, same child count), so the
    // matched node's view reconciles in place and stays hosted. (Structural
    // add/remove that forces TreeView to re-realize containers is the separate
    // §6 hosting tradeoff — see KeyedUpdateAddChild + the handoff doc.)
    internal class KeyedUpdateReconcile(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            WinXC.TreeView? firstInstance = null;

            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (phase, set) = ctx.UseState(0);
                FsNode[] tree =
                [
                    new FsFolder("root", "Root",
                    [
                        new FsFile("a", phase == 0 ? "alpha" : "alpha-renamed", "txt"),
                        new FsFile("b", "beta", "txt"),
                    ]),
                ];
                return VStack(
                    Button("Mutate", () => set(1)),
                    TreeView(tree,
                        keySelector: n => n.Id,
                        childrenSelector: ChildrenOf,
                        viewBuilder: BuildNodeView)
                        with { IsExpanded = _ => true }
                );
            });

            await Harness.Render();
            firstInstance = H.FindControl<WinXC.TreeView>(_ => true);
            H.Check("TTV_KeyedUpdate_InitialChild",
                await WaitFor(() => H.FindTextContaining("alpha.txt") is not null));

            H.ClickButton("Mutate");
            await Harness.Render();

            // With per-container hosting the reused node's view reconciles in
            // place and stays rendered in the visual tree.
            H.Check("TTV_KeyedUpdate_RenamedChildReconciled",
                await WaitFor(() => H.FindTextContaining("alpha-renamed.txt") is not null));
            // The rename has rendered, so the old text is gone ("alpha-renamed.txt"
            // does not contain the substring "alpha.txt").
            H.Check("TTV_KeyedUpdate_OldTextGone",
                H.FindTextContaining("alpha.txt") is null);
            // The untouched sibling keeps rendering through the reconcile.
            H.Check("TTV_KeyedUpdate_SiblingPreserved",
                await WaitFor(() => H.FindTextContaining("beta.txt") is not null));

            // The reconcile updated the existing control in place rather than
            // remounting a fresh TreeView.
            var secondInstance = H.FindControl<WinXC.TreeView>(_ => true);
            H.Check("TTV_KeyedUpdate_ControlIdentityPreserved",
                firstInstance is not null && ReferenceEquals(firstInstance, secondInstance));
        }
    }

    // ── 3b. Structural add — a freshly-keyed node appears and renders ─────
    // Verifies the diff inserts a new node (built fresh, so it hosts cleanly)
    // and the live TreeViewNode hierarchy reflects the new child count. The
    // reused siblings' re-hosting under container recycle is the open §6
    // tradeoff, so this asserts the data/structure side, not their pixels.
    internal class KeyedUpdateAddChild(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (phase, set) = ctx.UseState(0);
                FsNode[] children = phase == 0
                    ? [new FsFile("a", "alpha", "txt")]
                    : [new FsFile("a", "alpha", "txt"), new FsFile("b", "beta", "txt")];
                FsNode[] tree = [new FsFolder("root", "Root", children)];
                return VStack(
                    Button("Add", () => set(1)),
                    TreeView(tree,
                        keySelector: n => n.Id,
                        childrenSelector: ChildrenOf,
                        viewBuilder: BuildNodeView)
                        with { IsExpanded = _ => true }
                );
            });

            await Harness.Render();
            var tv = H.FindControl<WinXC.TreeView>(_ => true);
            H.Check("TTV_AddChild_InitialOneChild",
                tv is not null && tv.RootNodes.Count == 1 && tv.RootNodes[0].Children.Count == 1);

            H.ClickButton("Add");
            await Harness.Render();

            H.Check("TTV_AddChild_NodeInserted",
                tv!.RootNodes[0].Children.Count == 2);
            H.Check("TTV_AddChild_NewNodeRenders",
                await WaitFor(() => H.FindTextContaining("beta.txt") is not null));
        }
    }

    // ── 4. Event trampolines hand back the developer's own T (erasure) ────
    internal class EventErasureResolvesT(Harness h) : SelfTestFixtureBase(h)
    {
        public override Task RunAsync()
        {
            var tree = SampleTree();
            FsNode? invoked = null;
            FsNode? expanding = null;

            var el = TreeView(tree,
                keySelector: n => n.Id,
                childrenSelector: ChildrenOf,
                viewBuilder: BuildNodeView)
                with
                {
                    OnItemInvoked = n => invoked = n,
                    OnExpanding = n => expanding = n,
                };

            // The reconciler dispatches through the object-erased base; verify
            // the cast back to T round-trips the original reference.
            var roots = el.GetRoots();
            H.Check("TTV_Erasure_RootCount", roots.Count == 2);
            H.Check("TTV_Erasure_KeyResolves", el.GetKey(roots[0]) == "docs");
            H.Check("TTV_Erasure_ChildrenResolve", el.GetChildren(roots[0])?.Count == 2);
            H.Check("TTV_Erasure_LeafHasNoChildren", el.GetChildren(roots[1]) is { } pics && el.GetChildren(pics[0]) is null);

            el.InvokeItemInvoked(roots[0]);
            H.Check("TTV_Erasure_ItemInvokedResolvesT", ReferenceEquals(invoked, tree[0]));

            el.InvokeExpanding(roots[1]);
            H.Check("TTV_Erasure_ExpandingResolvesT", ReferenceEquals(expanding, tree[1]));

            return Task.CompletedTask;
        }
    }

    // ── 5. Value-type T is boxed/projected correctly ──────────────────────
    internal class ValueTypeT(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            int[] items = [1, 2, 3];
            int invoked = -1;

            var el = TreeView(items,
                keySelector: i => $"n{i}",
                childrenSelector: i => i == 1 ? new[] { 10, 11 } : null,
                viewBuilder: i => TextBlock($"#{i}"))
                with { OnItemInvoked = i => invoked = i, IsExpanded = _ => true };

            var roots = el.GetRoots();
            H.Check("TTV_ValueType_RootCount", roots.Count == 3);
            H.Check("TTV_ValueType_KeyResolves", el.GetKey(roots[0]) == "n1");
            H.Check("TTV_ValueType_ChildrenResolve", el.GetChildren(roots[0])?.Count == 2);

            el.InvokeItemInvoked(roots[2]);
            H.Check("TTV_ValueType_InvokeUnboxesT", invoked == 3);

            // And it actually mounts + renders.
            var host = H.CreateHost();
            host.Mount(_ => el);
            await Harness.Render();
            H.Check("TTV_ValueType_Renders", await WaitFor(() => H.FindTextContaining("#1") is not null));
            H.Check("TTV_ValueType_ChildRenders", await WaitFor(() => H.FindTextContaining("#10") is not null));
        }
    }

    // ── 6. IsExpanded selector drives the node's initial expansion ────────
    internal class IsExpandedApplied(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(_ =>
                TreeView(SampleTree(),
                    keySelector: n => n.Id,
                    childrenSelector: ChildrenOf,
                    viewBuilder: BuildNodeView)
                    with { IsExpanded = n => n.Id == "docs" }
            );

            await Harness.Render();

            var tv = H.FindControl<WinXC.TreeView>(_ => true);
            H.Check("TTV_IsExpanded_TreeFound", tv is not null);
            // First root ("docs") is expanded; the second ("pics") is not.
            H.Check("TTV_IsExpanded_SelectedNodeExpanded",
                tv!.RootNodes.Count == 2 && tv.RootNodes[0].IsExpanded);
            H.Check("TTV_IsExpanded_OtherNodeCollapsed",
                !tv.RootNodes[1].IsExpanded);
        }
    }

    // ── 6b. Expand/collapse cycles keep every child rendered ──────────────
    // Regression for the "every other expand/collapse blanks the first child
    // row(s)" bug: per-container hosting must re-mount a fresh view into
    // whichever pooled container WinUI realizes each node into, so no row goes
    // blank after a collapse→expand cycle recycles containers.
    internal class ExpandCollapseCycle(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(_ =>
                TreeView(SampleTree(),
                    keySelector: n => n.Id,
                    childrenSelector: ChildrenOf,
                    viewBuilder: BuildNodeView)
                    // Start collapsed so we drive the expansions ourselves.
                    with { IsExpanded = _ => false }
            );

            await Harness.Render();
            var tv = H.FindControl<WinXC.TreeView>(_ => true);
            H.Check("TTV_Cycle_TreeFound", tv is not null);

            var docs = tv!.RootNodes[0];   // "Documents" → readme.md, notes.txt
            bool allCyclesOk = true;

            // Several collapse→expand cycles; after each expand both children
            // must be present (the bug blanked the first child every 2nd cycle).
            // WaitFor tolerates the realization landing a pump-cycle later, but a
            // genuinely blank row never appears within the budget → fails.
            for (int cycle = 0; cycle < 4; cycle++)
            {
                docs.IsExpanded = true;
                bool firstChild = await WaitFor(() => H.FindTextContaining("readme.md") is not null);
                bool secondChild = await WaitFor(() => H.FindTextContaining("notes.txt") is not null);
                if (!firstChild || !secondChild) allCyclesOk = false;

                docs.IsExpanded = false;
                await Harness.Render();
            }

            H.Check("TTV_Cycle_NoBlankRowsAcrossCycles", allCyclesOk);

            // Leave it expanded and confirm a final realization renders.
            docs.IsExpanded = true;
            H.Check("TTV_Cycle_FinalExpandRenders",
                await WaitFor(() => H.FindTextContaining("readme.md") is not null)
                && await WaitFor(() => H.FindTextContaining("notes.txt") is not null));
        }
    }

    // ── 7. Unmount tears the tree down without leaking / throwing ─────────
    internal class UnmountTearsDown(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (show, set) = ctx.UseState(true);
                return VStack(
                    Button("Hide", () => set(false)),
                    show
                        ? TreeView(SampleTree(),
                            keySelector: n => n.Id,
                            childrenSelector: ChildrenOf,
                            viewBuilder: BuildNodeView)
                            with { IsExpanded = _ => true }
                        : TextBlock("gone")
                );
            });

            await Harness.Render();
            H.Check("TTV_Unmount_TreeMounted", H.FindControl<WinXC.TreeView>(_ => true) is not null);

            H.ClickButton("Hide");
            await Harness.Render();

            H.Check("TTV_Unmount_TreeRemoved", H.FindControl<WinXC.TreeView>(_ => true) is null);
            H.Check("TTV_Unmount_ReplacementShown", H.FindText("gone") is not null);
        }
    }

    // ── 8. Heterogeneous nodes stay correctly templated across recycling ──
    // The WinUI XAML ItemTemplateSelector failure mode (issue #447 comment):
    // under container recycling a realized container was reused WITHOUT
    // re-matching the selected template to the current item's type, so a
    // recycled row rendered the WRONG template (or blank). ExpandCollapseCycle
    // already proves homogeneous children don't blank; this adds the missing
    // pixel-level, heterogeneous check the §6 note in KeyedUpdateAddChild
    // deferred: drive repeated collapse→expand over a folder whose children are
    // a MIX of sub-folders and files, then assert every realized row is
    // type-consistent — its "[D]"/"[F]" tag agrees with the node's name tag —
    // and that no expected row went blank.
    internal class HeteroRecycleExpandCollapse(Harness h) : SelfTestFixtureBase(h)
    {
        // Names are tagged (Dxxx / Fxxx) so the rendered "[D]"/"[F]" prefix and
        // the name's first letter MUST agree; a stale/mismatched template — the
        // XAML bug — breaks the pair, and a blanked row drops out entirely.
        private static FsNode[] MixedTree() =>
        [
            new FsFolder("mixed", "Dmixed",
            [
                new FsFolder("s1", "Dsub1", []),
                new FsFile("x1", "Ffile1", "txt"),
                new FsFolder("s2", "Dsub2", []),
                new FsFile("x2", "Ffile2", "txt"),
            ]),
            new FsFile("root", "Froot", "txt"),
        ];

        // One coupled label per node, so a single TextBlock reveals a mismatch.
        private static Element CoupledView(FsNode n) => n switch
        {
            FsFolder f => TextBlock($"[D] {f.Name}"),
            FsFile file => TextBlock($"[F] {file.Name}"),
            _ => TextBlock(""),
        };

        // Every realized node row must be type-consistent: tag char (index 1 of
        // "[X] N…") is 'D'/'F' and equals the name's first char (index 4).
        // Blank rows don't start with "[", so presence checks catch those.
        private static bool RowsTypeConsistent(Harness h, out int rowCount)
        {
            var rows = h.FindAllControls<WinXC.TextBlock>(
                tb => tb.Text is { Length: > 0 } t && t.StartsWith("["));
            rowCount = rows.Count;
            foreach (var tb in rows)
            {
                var t = tb.Text;
                if (t.Length < 5 || (t[1] != 'D' && t[1] != 'F') || t[1] != t[4])
                    return false;
            }
            return true;
        }

        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(_ =>
                TreeView(MixedTree(),
                    keySelector: n => n.Id,
                    childrenSelector: ChildrenOf,
                    viewBuilder: CoupledView)
                    // Start collapsed so we drive the expansions ourselves.
                    with { IsExpanded = _ => false });

            await Harness.Render();
            var tv = H.FindControl<WinXC.TreeView>(_ => true);
            H.Check("TTV_HeteroRecycle_TreeFound", tv is not null);

            var mixed = tv!.RootNodes[0];   // "Dmixed" — heterogeneous children
            string[] expectedChildren =
                ["[D] Dsub1", "[F] Ffile1", "[D] Dsub2", "[F] Ffile2"];

            bool allCyclesOk = true;
            int observedRows = 0;

            // Repeated collapse→expand recycles the child containers across
            // folder/file types. After each expand every child must be present
            // AND every realized row must stay type-consistent.
            for (int cycle = 0; cycle < 4; cycle++)
            {
                mixed.IsExpanded = true;
                await WaitFor(() => H.FindText("[F] Ffile2") is not null);

                bool allPresent = expectedChildren.All(l => H.FindText(l) is not null);
                bool consistent = RowsTypeConsistent(H, out int rows);
                observedRows = rows;
                if (!allPresent || !consistent) allCyclesOk = false;

                mixed.IsExpanded = false;
                await Harness.Render();
            }

            // No "[D]" wearing a file's row, no "[F]" on a folder, no blanks —
            // the XAML recycling bug did NOT carry over to Reactor's TreeView<T>.
            H.Check("TTV_HeteroRecycle_NoMismatchedOrBlankRows", allCyclesOk);
            // Guard against a vacuously-true pass: we really realized rows.
            H.Check("TTV_HeteroRecycle_RowsRealized", observedRows > 0);

            // Final expand renders the full heterogeneous child set.
            mixed.IsExpanded = true;
            H.Check("TTV_HeteroRecycle_FinalExpandFullSet",
                await WaitFor(() => expectedChildren.All(l => H.FindText(l) is not null)));
        }
    }
}
