using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

public class WindowIdAllocatorTests
{
    [Theory]
    [InlineData("My App", "my-app")]
    [InlineData("  Trim Me  ", "trim-me")]
    [InlineData("Counter Demo — Preview", "counter-demo-preview")]
    [InlineData("___under__scores__", "under-scores")]
    [InlineData("Already-Has-Dashes", "already-has-dashes")]
    [InlineData("v1.2.3 Release", "v123-release")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("!!!", "")]
    public void Slugify_Cases(string title, string expected)
    {
        Assert.Equal(expected, WindowIdAllocator.Slugify(title));
    }

    [Fact]
    public void UniqueTitle_GetsSlug()
    {
        var a = new WindowIdAllocator();
        Assert.Equal("my-app", a.Allocate("My App"));
    }

    [Fact]
    public void EmptyTitle_FallsBackToAnonymousCounter()
    {
        var a = new WindowIdAllocator();
        Assert.Equal("Win1", a.Allocate(""));
        Assert.Equal("Win2", a.Allocate(null));
        Assert.Equal("Win3", a.Allocate("   "));
    }

    [Fact]
    public void Collision_GetsSuffixedId()
    {
        var a = new WindowIdAllocator();
        Assert.Equal("my-app", a.Allocate("My App"));
        Assert.Equal("my-app-2", a.Allocate("My App"));
        Assert.Equal("my-app-3", a.Allocate("My App"));
    }

    [Fact]
    public void IdIsReservedForever_EvenAcrossDifferentCallers()
    {
        // Spec §2.2: "Window ids are never reused even if a new window opens
        // with the same title." The allocator has no "release" method — once
        // given out, an id is retired.
        var a = new WindowIdAllocator();
        _ = a.Allocate("App");        // app
        _ = a.Allocate("App");        // app-2
        _ = a.Allocate("Other");      // other
        Assert.Equal("app-3", a.Allocate("App"));
    }

    [Fact]
    public void AnonymousAndSluggedMix_DoesNotCollide()
    {
        var a = new WindowIdAllocator();
        Assert.Equal("real-title", a.Allocate("Real Title"));
        Assert.Equal("Win1", a.Allocate(null));
        Assert.Equal("real-title-2", a.Allocate("Real Title"));
    }

    [Fact]
    public void Reserve_ForcesExplicitIdRegardlessOfTitle()
    {
        // feedback #2: devtools main window pins to "main" via Reserve so the
        // handle survives switchComponent changing the window title.
        var a = new WindowIdAllocator();
        Assert.Equal("main", a.Reserve("main"));
        // A later title-based allocation for "Main" slug collides with the
        // reserved id and must get a disambiguating suffix.
        Assert.Equal("main-2", a.Allocate("Main"));
    }

    [Fact]
    public void Reserve_CollisionSuffixesTheExplicitId()
    {
        var a = new WindowIdAllocator();
        Assert.Equal("main", a.Reserve("main"));
        Assert.Equal("main-2", a.Reserve("main"));
    }
}
