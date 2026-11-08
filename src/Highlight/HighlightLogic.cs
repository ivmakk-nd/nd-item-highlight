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
    // while the key is held and during the fade after it, and one release when the outlines must go.
    public sealed class KeyGate
    {
        public const float RefreshInterval = 0.5f;
        // The longest time a new item waits for its outline. Set from the config.
        public float FullSearchInterval { get; set; } = 2f;

        // The time from the release until the outlines are gone. Only the caller sets a value above 0.
        public float FadeDelay { get; set; }

        private enum GateState
        {
            Off,
            Held,
            Fading,
        }

        private GateState _state;
        private float _fadeStart;
        private float _lastRefresh;
        private float _lastSearch;

        // After a Refresh: true when the caller must search for all items again, not only check the ones it found.
        public bool FullSearch { get; private set; }

        // From 1 to 0 during the fade. 1 when the gate is off or held.
        public float Strength { get; private set; } = 1f;

        public bool IsFading => _state == GateState.Fading;

        // A window without focus or an unlocked cursor (an open menu) ends the highlight at once.
        public GateAction Tick(bool keyHeld, bool focused, bool cursorLocked, float now)
        {
            bool ready = focused && cursorLocked;
            if (keyHeld && ready)
            {
                bool first = _state == GateState.Off;
                _state = GateState.Held;
                Strength = 1f;
                return RefreshOnTimers(first, now);
            }
            if (_state == GateState.Off)
            {
                return GateAction.None;
            }
            if (!ready || FadeDelay <= 0f)
            {
                return End();
            }
            if (_state == GateState.Held)
            {
                _state = GateState.Fading;
                _fadeStart = now;
                return GateAction.None;
            }
            if (now - _fadeStart >= FadeDelay)
            {
                return End();
            }
            Strength = HighlightLogic.FadeStrength(now - _fadeStart, FadeDelay);
            return RefreshOnTimers(false, now);
        }

        private GateAction RefreshOnTimers(bool first, float now)
        {
            if (!first && now - _lastRefresh < RefreshInterval)
            {
                return GateAction.None;
            }
            FullSearch = first || now - _lastSearch >= FullSearchInterval;
            if (FullSearch)
            {
                _lastSearch = now;
            }
            _lastRefresh = now;
            return GateAction.Refresh;
        }

        private GateAction End()
        {
            _state = GateState.Off;
            Strength = 1f;
            return GateAction.Release;
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

        // Full at the release, zero when the fade delay ends, a straight line between. A delay of 0 gives 0.
        public static float FadeStrength(float elapsed, float delay)
        {
            if (delay <= 0f || elapsed >= delay)
            {
                return 0f;
            }
            return elapsed <= 0f ? 1f : 1f - elapsed / delay;
        }

        // Parses "#RRGGBB" or "#RRGGBBAA" into channels from 0 to 1. The "#" is optional, and alpha is 1 for the 6-digit form.
        // False for any malformed text, so the caller can use its default color.
        public static bool TryParseHexColor(string s, out float r, out float g, out float b, out float a)
        {
            r = 0f;
            g = 0f;
            b = 0f;
            a = 1f;
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }
            s = s.Trim();
            if (s.Length > 0 && s[0] == '#')
            {
                s = s.Substring(1);
            }
            if (s.Length != 6 && s.Length != 8)
            {
                return false;
            }
            int ai = 255;
            if (!TryHexByte(s, 0, out int ri) || !TryHexByte(s, 2, out int gi) || !TryHexByte(s, 4, out int bi)
                || (s.Length == 8 && !TryHexByte(s, 6, out ai)))
            {
                return false;
            }
            r = ri / 255f;
            g = gi / 255f;
            b = bi / 255f;
            a = ai / 255f;
            return true;
        }

        private static bool TryHexByte(string s, int i, out int value)
        {
            value = 0;
            int hi = HexDigit(s[i]);
            int lo = HexDigit(s[i + 1]);
            if (hi < 0 || lo < 0)
            {
                return false;
            }
            value = hi * 16 + lo;
            return true;
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }
            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }
            return -1;
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
