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

    [Theory]
    [InlineData(0f, 3f, 1f)]      // full at the release
    [InlineData(1.5f, 3f, 0.5f)]  // a straight line
    [InlineData(3f, 3f, 0f)]      // zero at the end
    [InlineData(4f, 3f, 0f)]      // never below 0
    [InlineData(-1f, 3f, 1f)]     // never above 1
    [InlineData(0f, 0f, 0f)]      // no delay, no division
    public void Fade_strength_falls_in_a_straight_line(float elapsed, float delay, float expected)
    {
        Assert.Equal(expected, HighlightLogic.FadeStrength(elapsed, delay), 4);
    }

    [Fact]
    public void A_release_with_a_fade_delay_starts_the_fade_and_returns_none()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Assert.Equal(GateAction.None, Up(10.1f));
        Assert.Equal(1f, _gate.Strength, 4);
    }

    [Fact]
    public void The_strength_falls_during_the_fade()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        Up(11.5f);
        Assert.Equal(0.5f, _gate.Strength, 4);
    }

    [Fact]
    public void The_end_of_the_fade_releases_once()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        Assert.Equal(GateAction.Release, Up(13f));
        Assert.Equal(GateAction.None, Up(13.1f));
    }

    [Fact]
    public void A_delay_that_reaches_zero_during_the_fade_releases_at_once()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        _gate.FadeDelay = 0f;
        Assert.Equal(GateAction.Release, Up(10.1f));
    }

    [Fact]
    public void The_strength_is_full_when_off_or_held()
    {
        _gate.FadeDelay = 3f;
        Assert.Equal(1f, _gate.Strength, 4);
        Held(10f);
        Assert.Equal(1f, _gate.Strength, 4);
        Up(10f);
        Up(12f);
        Up(13f);
        Assert.Equal(1f, _gate.Strength, 4);
    }

    [Fact]
    public void The_fade_refreshes_on_the_timers_of_a_held_key()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10.1f);
        Assert.Equal(GateAction.None, Up(10.3f));
        Assert.Equal(GateAction.Refresh, Up(10.5f));
        Assert.False(_gate.FullSearch);
        Assert.Equal(GateAction.None, Up(10.7f));
        Assert.Equal(GateAction.Refresh, Up(11.0f));
        Assert.False(_gate.FullSearch);
    }

    [Fact]
    public void The_fade_searches_again_when_the_search_interval_is_over()
    {
        _gate.FadeDelay = 5f;
        Held(10f);
        Up(10.1f);
        Up(10.5f);
        Up(11.0f);
        Up(11.5f);
        Up(12.0f);
        Assert.True(_gate.FullSearch);
    }

    [Fact]
    public void A_key_press_during_the_fade_restores_full_strength_with_no_full_search()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        Up(11.5f);
        Held(11.6f);
        Assert.Equal(1f, _gate.Strength, 4);
        Assert.False(_gate.FullSearch);
    }

    [Fact]
    public void The_release_after_a_key_press_during_the_fade_starts_a_full_fade()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        Up(12f);
        Held(12.1f);
        Assert.Equal(GateAction.None, Up(12.2f));
        Assert.Equal(1f, _gate.Strength, 4);
        Up(13.7f);
        Assert.Equal(0.5f, _gate.Strength, 4);
    }

    [Fact]
    public void A_key_press_on_the_tick_where_the_fade_ends_wins_over_the_release()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        Assert.NotEqual(GateAction.Release, Held(13f));
        Assert.Equal(1f, _gate.Strength, 4);
    }

    [Fact]
    public void A_lost_focus_during_the_fade_releases_at_once_and_stays_off()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        Assert.Equal(GateAction.Release, _gate.Tick(keyHeld: false, focused: false, cursorLocked: true, 10.1f));
        Assert.Equal(GateAction.None, Up(10.6f));
    }

    [Fact]
    public void An_unlocked_cursor_during_the_fade_releases_at_once_and_stays_off()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Up(10f);
        Assert.Equal(GateAction.Release, _gate.Tick(keyHeld: false, focused: true, cursorLocked: false, 10.1f));
        Assert.Equal(GateAction.None, Up(10.6f));
    }

    [Fact]
    public void Key_up_together_with_a_lost_focus_releases_with_no_fade()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Assert.Equal(GateAction.Release, _gate.Tick(keyHeld: false, focused: false, cursorLocked: true, 10.1f));
    }

    [Fact]
    public void Key_up_together_with_an_unlocked_cursor_releases_with_no_fade()
    {
        _gate.FadeDelay = 3f;
        Held(10f);
        Assert.Equal(GateAction.Release, _gate.Tick(keyHeld: false, focused: true, cursorLocked: false, 10.1f));
    }

    [Fact]
    public void An_open_menu_counts_as_key_up()
    {
        Held(10f);
        Assert.Equal(GateAction.Release, _gate.Tick(keyHeld: true, focused: true, cursorLocked: false, 10.1f));
        Assert.Equal(GateAction.None, _gate.Tick(keyHeld: true, focused: true, cursorLocked: false, 10.6f));
    }
}
