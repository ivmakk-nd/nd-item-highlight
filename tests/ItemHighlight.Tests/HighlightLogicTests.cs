using ItemHighlight;
using Xunit;

public class HighlightLogicTests
{
    private const float Radius = 25f;

    // A pickup item on the ground, 10 m away, that the player can take.
    private static ItemFacts ItemOnGround() => new ItemFacts
    {
        Alive = true,
        Active = true,
        SqrDistance = 100f,
        AvailableForInteraction = true,
    };

    [Fact]
    public void Item_on_the_ground_within_the_radius_gets_the_outline()
    {
        Assert.True(HighlightLogic.ShouldHighlight(ItemOnGround(), Radius));
    }

    [Fact]
    public void Item_at_exactly_the_radius_gets_the_outline()
    {
        var item = ItemOnGround();
        item.SqrDistance = 625f;
        Assert.True(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_outside_the_radius_gets_no_outline()
    {
        var item = ItemOnGround();
        item.SqrDistance = 1600f;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_that_is_not_alive_gets_no_outline()
    {
        var item = ItemOnGround();
        item.Alive = false;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Inactive_item_gets_no_outline()
    {
        var item = ItemOnGround();
        item.Active = false;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_in_hand_gets_no_outline()
    {
        var item = ItemOnGround();
        item.Equipped = true;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_in_the_hotbar_gets_no_outline()
    {
        var item = ItemOnGround();
        item.Stowed = true;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_in_flight_gets_no_outline()
    {
        var item = ItemOnGround();
        item.InFlight = true;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_that_the_player_cannot_take_now_gets_no_outline()
    {
        var item = ItemOnGround();
        item.AvailableForInteraction = false;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Cargo_gets_no_outline()
    {
        var item = ItemOnGround();
        item.VehicleIsolated = true;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_parented_to_a_drivable_vehicle_gets_no_outline()
    {
        var item = ItemOnGround();
        item.HasDrivableVehicleParent = true;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Item_on_top_of_a_carried_item_gets_no_outline()
    {
        var item = ItemOnGround();
        item.HasEquippedCarrier = true;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Vehicle_part_installed_on_a_drivable_vehicle_gets_no_outline()
    {
        var item = ItemOnGround();
        item.InstalledOnDrivableVehicle = true;
        Assert.False(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void Loose_vehicle_part_gets_the_outline()
    {
        var item = ItemOnGround();
        item.InstalledOnDrivableVehicle = false;
        Assert.True(HighlightLogic.ShouldHighlight(item, Radius));
    }

    [Fact]
    public void The_cheap_checks_do_not_read_the_installed_part_fact()
    {
        var item = ItemOnGround();
        item.InstalledOnDrivableVehicle = true;
        Assert.True(HighlightLogic.PassesCheapChecks(item, Radius));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void A_bad_radius_gives_no_outline(float radius)
    {
        Assert.False(HighlightLogic.ShouldHighlight(ItemOnGround(), radius));
    }

    [Fact]
    public void The_cheap_checks_pass_before_the_parent_checks_run()
    {
        var item = ItemOnGround();
        item.HasDrivableVehicleParent = true;
        Assert.True(HighlightLogic.PassesCheapChecks(item, Radius));
    }

    [Fact]
    public void The_cheap_checks_fail_outside_the_radius()
    {
        var item = ItemOnGround();
        item.SqrDistance = 1600f;
        Assert.False(HighlightLogic.PassesCheapChecks(item, Radius));
    }
}
