using System.Collections.Generic;
using ItemHighlight;
using Xunit;

public class OutlineSetTests
{
    private readonly List<string> _toEnable = new List<string> { "stale" };
    private readonly List<string> _toDisable = new List<string> { "stale" };

    [Fact]
    public void From_an_empty_start_each_new_item_gets_enabled()
    {
        HighlightLogic.Diff(new HashSet<string>(), new HashSet<string> { "crowbar", "can" }, _toEnable, _toDisable);
        Assert.Equal(new[] { "can", "crowbar" }, Sorted(_toEnable));
        Assert.Empty(_toDisable);
    }

    [Fact]
    public void An_item_that_enters_gets_enabled_and_one_that_stays_is_in_neither_list()
    {
        HighlightLogic.Diff(new HashSet<string> { "crowbar" }, new HashSet<string> { "crowbar", "can" }, _toEnable, _toDisable);
        Assert.Equal(new[] { "can" }, _toEnable);
        Assert.Empty(_toDisable);
    }

    [Fact]
    public void An_item_that_leaves_gets_disabled()
    {
        HighlightLogic.Diff(new HashSet<string> { "crowbar", "can" }, new HashSet<string> { "crowbar" }, _toEnable, _toDisable);
        Assert.Empty(_toEnable);
        Assert.Equal(new[] { "can" }, _toDisable);
    }

    [Fact]
    public void A_clear_disables_every_item()
    {
        HighlightLogic.Diff(new HashSet<string> { "crowbar", "can" }, new HashSet<string>(), _toEnable, _toDisable);
        Assert.Empty(_toEnable);
        Assert.Equal(new[] { "can", "crowbar" }, Sorted(_toDisable));
    }

    private static string[] Sorted(List<string> items)
    {
        var copy = items.ToArray();
        System.Array.Sort(copy);
        return copy;
    }
}
