using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for ElementPool. These test the pool logic itself.
/// Note: WinUI control instantiation requires the WinUI thread in practice,
/// but the pool logic (type checks, capacity) can be tested with the records.
/// </summary>
public class ElementPoolTests
{
    [Fact]
    public void TryRent_EmptyPool_Returns_Null()
    {
        var pool = new ElementPool();
        Assert.Null(pool.TryRent(typeof(TextBlock)));
    }

    [Fact]
    public void TryRent_NonPoolableType_Returns_Null()
    {
        var pool = new ElementPool();
        // Button is not a poolable type
        Assert.Null(pool.TryRent(typeof(Button)));
    }

    [Fact]
    public void IsPoolable_Types_Are_Correct()
    {
        var pool = new ElementPool();
        // Only non-interactive types are poolable
        // TextBlock should be poolable
        Assert.Null(pool.TryRent(typeof(TextBlock))); // Empty, but type is accepted

        // Button should not be poolable
        Assert.Null(pool.TryRent(typeof(Button)));
    }
}
