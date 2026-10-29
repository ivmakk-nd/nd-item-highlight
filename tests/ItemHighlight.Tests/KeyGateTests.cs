using ItemHighlight;
using Xunit;

public class KeyGateTests
{
    private readonly KeyGate _gate = new KeyGate();

    private GateAction Held(float now) => _gate.Tick(keyHeld: true, focused: true, cursorLocked: true, now);
    private GateAction Up(float now) => _gate.Tick(keyHeld: false, focused: true, cursorLocked: true, now);

    [Fact]
    public void Key_up_from_the_start_does_nothing()
    {
        Assert.Equal(GateAction.None, Up(0f));
    }

    [Fact]
    public void Key_down_refreshes_at_once()
    {
        Assert.Equal(GateAction.Refresh, Held(10f));
    }

    [Fact]
    public void A_held_key_refreshes_every_half_second()
    {
        Held(10f);
        Assert.Equal(GateAction.None, Held(10.3f));
        Assert.Equal(GateAction.Refresh, Held(10.5f));
        Assert.Equal(GateAction.None, Held(10.7f));
        Assert.Equal(GateAction.Refresh, Held(11.0f));
    }

    [Fact]
    public void Key_up_releases_once()
    {
        Held(10f);
        Assert.Equal(GateAction.Release, Up(10.1f));
        Assert.Equal(GateAction.None, Up(10.2f));
    }

    [Fact]
    public void A_new_key_down_after_a_release_refreshes_at_once()
    {
        Held(10f);
        Up(10.1f);
        Assert.Equal(GateAction.Refresh, Held(10.2f));
    }

    [Fact]
    public void Focus_loss_while_held_releases()
    {
        Held(10f);
        Assert.Equal(GateAction.Release, _gate.Tick(keyHeld: true, focused: false, cursorLocked: true, 10.1f));
    }

    [Fact]
    public void A_return_with_the_key_up_does_not_refresh()
    {
        Held(10f);
        _gate.Tick(keyHeld: true, focused: false, cursorLocked: true, 10.1f);
        Assert.Equal(GateAction.None, Up(12f));
    }

    [Fact]
    public void Key_down_also_runs_a_full_search()
    {
        Held(10f);
        Assert.True(_gate.FullSearch);
    }

    [Fact]
    public void A_refresh_within_two_seconds_reuses_the_found_items()
    {
        Held(10f);
        Held(10.5f);
        Assert.False(_gate.FullSearch);
        Held(11.5f);
        Assert.False(_gate.FullSearch);
    }

    [Fact]
    public void A_refresh_two_seconds_after_the_last_search_searches_again()
    {
        Held(10f);
        Held(10.5f);
        Held(11.0f);
        Held(11.5f);
        Held(12.0f);
        Assert.True(_gate.FullSearch);
        Held(12.5f);
        Assert.False(_gate.FullSearch);
    }

    [Fact]
    public void A_new_key_down_soon_after_a_release_searches_again()
    {
        Held(10f);
        Up(10.2f);
        Held(10.5f);
        Assert.True(_gate.FullSearch);
    }

    [Fact]
    public void A_longer_search_interval_from_the_config_delays_the_next_search()
    {
        _gate.FullSearchInterval = 5f;
        Held(10f);
        Held(12.0f);
        Assert.False(_gate.FullSearch);
        Held(15.0f);
        Assert.True(_gate.FullSearch);
    }

    [Fact]
    public void An_open_menu_counts_as_key_up()
    {
        Held(10f);
        Assert.Equal(GateAction.Release, _gate.Tick(keyHeld: true, focused: true, cursorLocked: false, 10.1f));
        Assert.Equal(GateAction.None, _gate.Tick(keyHeld: true, focused: true, cursorLocked: false, 10.6f));
    }
}
