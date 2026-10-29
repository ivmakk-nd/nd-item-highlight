using System.Collections.Generic;

namespace ItemHighlight
{
    // The facts of one pickup item that decide whether it gets the outline.
    public struct ItemFacts
    {
        public bool Alive;
        public bool Active;
        public float SqrDistance;
        public bool Equipped;
        public bool Stowed;
        public bool InFlight;
        public bool AvailableForInteraction;
        public bool VehicleIsolated;
        public bool HasDrivableVehicleParent;
        public bool HasEquippedCarrier;
        public bool InstalledOnDrivableVehicle;
    }

    public enum GateAction
    {
        None,
        Refresh,
        Release,
    }

    // Turns the key, focus, and cursor state of each frame into a refresh on key down and every half second
    // while the key is held, and one release when it stops.
    public sealed class KeyGate
    {
        public const float RefreshInterval = 0.5f;
        // The longest time a new item waits for its outline. Set from the config.
        public float FullSearchInterval { get; set; } = 2f;

        private bool _held;
        private float _lastRefresh;
        private float _lastSearch;

        // After a Refresh: true when the caller must search for all items again, not only check the ones it found.
        public bool FullSearch { get; private set; }

        // A window without focus or an unlocked cursor (an open menu) counts as key up.
        public GateAction Tick(bool keyHeld, bool focused, bool cursorLocked, float now)
        {
            bool held = keyHeld && focused && cursorLocked;
            if (!held)
            {
                if (!_held)
                {
                    return GateAction.None;
                }
                _held = false;
                return GateAction.Release;
            }
            if (_held && now - _lastRefresh < RefreshInterval)
            {
                return GateAction.None;
            }
            FullSearch = !_held || now - _lastSearch >= FullSearchInterval;
            if (FullSearch)
            {
                _lastSearch = now;
            }
            _held = true;
            _lastRefresh = now;
            return GateAction.Refresh;
        }
    }

    // Game-free decisions of the item highlight. No game, Unity, or BepInEx type, so the tests can link this file.
    public static class HighlightLogic
    {
        public static bool ShouldHighlight(ItemFacts item, float radius)
        {
            return PassesCheapChecks(item, radius)
                && !item.HasDrivableVehicleParent
                && !item.HasEquippedCarrier
                && !item.InstalledOnDrivableVehicle;
        }

        // The checks that read only flags and the distance. The caller runs the parent lookups only when these pass.
        public static bool PassesCheapChecks(ItemFacts item, float radius)
        {
            return item.Alive
                && item.Active
                && radius >= 0f
                && item.SqrDistance <= radius * radius
                && !item.Equipped
                && !item.Stowed
                && !item.InFlight
                && item.AvailableForInteraction
                && !item.VehicleIsolated;
        }

        // Fills the items that enter the outlined set and the items that leave it. Clears both lists first, so the caller reuses them.
        public static void Diff<T>(HashSet<T> current, HashSet<T> next, List<T> toEnable, List<T> toDisable)
        {
            toEnable.Clear();
            toDisable.Clear();
            foreach (T item in next)
            {
                if (!current.Contains(item))
                {
                    toEnable.Add(item);
                }
            }
            foreach (T item in current)
            {
                if (!next.Contains(item))
                {
                    toDisable.Add(item);
                }
            }
        }
    }
}
