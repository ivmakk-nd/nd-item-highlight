using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NomadDrive.Features.Attachables;
using NomadDrive.Features.Interaction;
using NomadDrive.Features.Vehicle.Networking;
using UnityEngine;

namespace ItemHighlight
{
    // Outlines the pickup items near the camera while the highlight key is held and during the fade after it, with the game's hover outline.
    internal static class Highlighter
    {
        private static readonly KeyGate Gate = new KeyGate();
        private static HashSet<HeldItem> _outlined = new HashSet<HeldItem>();
        private static HashSet<HeldItem> _next = new HashSet<HeldItem>();
        private static readonly List<HeldItem> ToEnable = new List<HeldItem>();
        private static readonly List<HeldItem> ToDisable = new List<HeldItem>();
        private static KeyCode _mainKey;
        private static KeyCode[] _modifiers = new KeyCode[0];
        private static Camera _camera;

        // The pickup items of the last full search. Refreshes in between check only these.
        private static HeldItem[] _found = Array.Empty<HeldItem>();

        // The item under the crosshair, kept by the hover patches. Its outline belongs to the game's hover.
        internal static Interactable Hovered;

        // A door or another fixed object under the crosshair. It has no base class in common with Interactable.
        internal static StaticInteractable HoveredStatic;

        // The hover target on the free layer while the item highlight shows, and the renderers that were moved there.
        private static Component _gameLook;
        private static Renderer[] _gameLookRenderers;

        // An outlined item that the crosshair just left. AttachableObject.OnUnhovered calls DisableOutline after the base
        // method, so the outline comes back at the next Tick, not in the postfix of the base.
        private static HeldItem _reoutline;

        // The last strength written to the outline materials.
        private static float _strength = 1f;
        private static bool _wasFading;

        // True while the key is held and during the fade after it.
        internal static bool IsShowing { get; private set; }

        internal static void CacheKey(KeyboardShortcut shortcut)
        {
            _mainKey = shortcut.MainKey;
            _modifiers = new List<KeyCode>(shortcut.Modifiers).ToArray();
        }

        internal static void Tick()
        {
            if ((object)_reoutline != null)
            {
                HeldItem item = _reoutline;
                _reoutline = null;
                Reoutline(item);
            }
            Gate.FullSearchInterval = Plugin.SearchInterval.Value;
            // Without the materials there is no fade, so the outlines go off at the release.
            if (IsShowing)
            {
                Gate.FadeDelay = OutlineStyle.HasMaterials ? Plugin.FadeDelay.Value : 0f;
            }
            GateAction action = Gate.Tick(KeyHeld(), Application.isFocused, Cursor.lockState == CursorLockMode.Locked, Time.unscaledTime);
            switch (action)
            {
                case GateAction.Refresh:
                    if (!IsShowing)
                    {
                        if (Plugin.Verbose.Value)
                        {
                            Plugin.Log.LogDebug("ItemHighlight: key down");
                        }
                        OutlineStyle.Begin();
                    }
                    IsShowing = true;
                    Refresh(Gate.FullSearch);
                    if (Gate.IsFading && Plugin.Verbose.Value)
                    {
                        Plugin.Log.LogDebug($"ItemHighlight: fade strength {Gate.Strength:F2}");
                    }
                    break;
                case GateAction.Release:
                    ReleaseAll();
                    if (Plugin.Verbose.Value)
                    {
                        Plugin.Log.LogDebug(_wasFading ? "ItemHighlight: fade end" : "ItemHighlight: key up");
                    }
                    break;
            }
            // Skipped on the Release tick, so OutlineStyle.End stays the last material write of an item highlight.
            if (action != GateAction.Release && IsShowing && Gate.Strength != _strength)
            {
                _strength = Gate.Strength;
                OutlineStyle.SetStrength(_strength);
            }
            if (Gate.IsFading && !_wasFading && Plugin.Verbose.Value)
            {
                Plugin.Log.LogDebug("ItemHighlight: fade start");
            }
            _wasFading = Gate.IsFading;
            if (IsShowing)
            {
                UpdateGameLook();
            }
        }

        private static void UpdateGameLook()
        {
            Component target = OutlineStyle.CanDraw(_camera) ? HoverTarget() : null;
            // A destroyed object compares equal to a null target, but its pass and its renderers still need the end.
            bool destroyed = (object)_gameLook != null && _gameLook == null;
            if (target == _gameLook && !destroyed)
            {
                return;
            }
            EndGameLook();
            if (target != null)
            {
                StartGameLook(target);
            }
        }

        // A destroyed or disabled hover target gives C# null here. InteractionManager skips the unhover of a destroyed object,
        // and OnDisable of the game removes the unhover listener, so the fields can keep such an object.
        private static Component HoverTarget()
        {
            if (Hovered != null && Hovered.isActiveAndEnabled)
            {
                return Hovered;
            }
            if (HoveredStatic != null && HoveredStatic.isActiveAndEnabled)
            {
                return HoveredStatic;
            }
            return null;
        }

        private static void StartGameLook(Component target)
        {
            int selection = LayerMask.NameToLayer("Selection");
            // The same list as the protected GetRenderers of the game.
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            bool moved = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.layer == selection)
                {
                    renderer.gameObject.layer = OutlineStyle.HoverLayer;
                    moved = true;
                }
            }
            _gameLook = target;
            _gameLookRenderers = renderers;
            // A trigger zone or a slot can have no outlined renderer, and then the pass would draw nothing.
            OutlineStyle.SetHoverPass(moved);
            if (Plugin.Verbose.Value)
            {
                Plugin.Log.LogDebug($"ItemHighlight: game look start on {target.name} ({target.GetType().Name}), {renderers.Length} renderers");
            }
        }

        // The stored renderers also cover children that left the object during the look. The other outlines come back
        // afterwards, because the items stacked below the object share its renderers.
        private static void EndGameLook()
        {
            if ((object)_gameLook == null)
            {
                return;
            }
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            Component ended = _gameLook;
            Renderer[] renderers = _gameLookRenderers;
            _gameLook = null;
            _gameLookRenderers = null;
            OutlineStyle.SetHoverPass(false);
            int defaultLayer = LayerMask.NameToLayer("Default");
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.gameObject.layer == OutlineStyle.HoverLayer)
                {
                    renderer.gameObject.layer = defaultLayer;
                }
            }
            if (IsShowing)
            {
                foreach (HeldItem item in _outlined)
                {
                    if (item != null)
                    {
                        item.EnableOutline();
                    }
                }
            }
            RestoreHover();
            if (Plugin.Verbose.Value)
            {
                string name = ended == null ? "a destroyed object" : ended.name;
                Plugin.Log.LogDebug($"ItemHighlight: game look end on {name} in {Ms(System.Diagnostics.Stopwatch.GetTimestamp() - start):F3} ms");
            }
        }

        // The main key and its own modifiers. Other held keys (Shift to sprint) do not matter.
        private static bool KeyHeld()
        {
            if (_mainKey == KeyCode.None || !UnityInput.Current.GetKey(_mainKey))
            {
                return false;
            }
            foreach (KeyCode modifier in _modifiers)
            {
                if (!UnityInput.Current.GetKey(modifier))
                {
                    return false;
                }
            }
            return true;
        }

        private static void Refresh(bool fullSearch)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            // Another camera can take the MainCamera tag (the game's recording rig), so a disabled cached camera is replaced.
            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
            }
            if (fullSearch)
            {
                _found = UnityEngine.Object.FindObjectsByType<HeldItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }
            long searched = System.Diagnostics.Stopwatch.GetTimestamp();
            _next.Clear();
            if (_camera != null)
            {
                Vector3 cameraPosition = _camera.transform.position;
                float radius = Plugin.Radius.Value;
                foreach (HeldItem item in _found)
                {
                    if (Qualifies(item, cameraPosition, radius))
                    {
                        _next.Add(item);
                    }
                }
            }
            long checkedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            HighlightLogic.Diff(_outlined, _next, ToEnable, ToDisable);
            foreach (HeldItem item in ToDisable)
            {
                if (item != null && item != Hovered)
                {
                    item.DisableOutline();
                }
            }
            if (ToDisable.Count > 0)
            {
                // DisableOutline also clears the renderers of items stacked on the removed one, so each kept item
                // and the hovered item get their outline back.
                foreach (HeldItem item in _next)
                {
                    item.EnableOutline();
                }
                RestoreHover();
            }
            else
            {
                foreach (HeldItem item in ToEnable)
                {
                    item.EnableOutline();
                }
            }
            HashSet<HeldItem> swap = _outlined;
            _outlined = _next;
            _next = swap;
            // Empty after a full refresh, so ReleaseAll after a refresh that threw can still find the items it outlined.
            ToEnable.Clear();
            ToDisable.Clear();

            if (Plugin.Verbose.Value)
            {
                long end = System.Diagnostics.Stopwatch.GetTimestamp();
                Plugin.Log.LogDebug($"ItemHighlight: refresh search {(fullSearch ? "yes" : "no")} walked {_found.Length} outlined {_outlined.Count} " +
                    $"in {Ms(end - start):F3} ms (find {Ms(searched - start):F3} checks {Ms(checkedAt - searched):F3} outline {Ms(end - checkedAt):F3})");
            }
        }

        private static double Ms(long ticks)
        {
            return ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }

        private static bool Qualifies(HeldItem item, Vector3 cameraPosition, float radius)
        {
            if (item == null)
            {
                return false;
            }
            var facts = new ItemFacts
            {
                Alive = true,
                Active = item.isActiveAndEnabled,
                SqrDistance = (item.transform.position - cameraPosition).sqrMagnitude,
                Equipped = item.IsEquipped,
                Stowed = item.IsStowed,
                InFlight = item.IsInFlight,
                AvailableForInteraction = item.AvailableForInteraction,
                VehicleIsolated = item.IsVehicleColliderIsolated,
            };
            if (!HighlightLogic.PassesCheapChecks(facts, radius))
            {
                return false;
            }
            facts.HasDrivableVehicleParent = item.GetComponentInParent<NetworkedNWHVehicle>() != null;
            facts.HasEquippedCarrier = HasEquippedCarrier(item);
            facts.InstalledOnDrivableVehicle = InstalledOnDrivableVehicle(item);
            return HighlightLogic.ShouldHighlight(facts, radius);
        }

        // An installed vehicle part is not a child of its vehicle, so the parent check misses it. Its slot is on the vehicle.
        private static bool InstalledOnDrivableVehicle(HeldItem item)
        {
            if (!(item is AttachableObject part) || !part.IsAttached)
            {
                return false;
            }
            Component slot = part.ObjectSlot as Component;
            return slot != null && slot.GetComponentInParent<NetworkedNWHVehicle>() != null;
        }

        // True when a pickup item above this one in the hierarchy is in a player's hands.
        private static bool HasEquippedCarrier(HeldItem item)
        {
            Transform parent = item.transform.parent;
            HeldItem carrier = parent == null ? null : parent.GetComponentInParent<HeldItem>();
            while (carrier != null)
            {
                if (carrier.IsEquipped)
                {
                    return true;
                }
                parent = carrier.transform.parent;
                carrier = parent == null ? null : parent.GetComponentInParent<HeldItem>();
            }
            return false;
        }

        // Removes each outline that the mod added. The hovered item keeps the game's hover outline.
        // ToEnable holds items only when a refresh threw before it stored them in the set.
        internal static void ReleaseAll()
        {
            IsShowing = false;
            _strength = 1f;
            _reoutline = null;
            try
            {
                // First, so the hover target is on the game's layer before OutlineStyle.End.
                EndGameLook();
                Disable(_outlined);
                Disable(ToEnable);
                RestoreHover();
            }
            finally
            {
                _outlined.Clear();
                ToEnable.Clear();
                ToDisable.Clear();
                _found = Array.Empty<HeldItem>();
                OutlineStyle.End();
            }
        }

        private static void Disable(IEnumerable<HeldItem> items)
        {
            foreach (HeldItem item in items)
            {
                if (item != null && item != Hovered)
                {
                    item.DisableOutline();
                }
            }
        }

        // A DisableOutline of an item under the hovered one also clears the hovered item's renderers.
        private static void RestoreHover()
        {
            if (HoverTarget() is IInteractable target)
            {
                target.EnableOutline();
            }
        }

        // The game's unhover removes any outline of the item. While the item highlight shows, an item that still qualifies gets it back.
        internal static void OnUnhovered(Interactable interactable)
        {
            if (IsShowing && interactable is HeldItem item && _outlined.Contains(item))
            {
                _reoutline = item;
            }
        }

        private static void Reoutline(HeldItem item)
        {
            if (!IsShowing || item == Hovered || !_outlined.Contains(item) || _camera == null)
            {
                return;
            }
            if (Qualifies(item, _camera.transform.position, Plugin.Radius.Value))
            {
                item.EnableOutline();
                if (Plugin.Verbose.Value)
                {
                    Plugin.Log.LogDebug($"ItemHighlight: kept outline on {item.name}");
                }
            }
        }
    }

    // The overrides of the subclasses call the base, so these postfixes fire for each pickup item.
    [HarmonyPatch(typeof(Interactable), "OnHovered")]
    internal static class HighlightOnHovered
    {
        private static void Postfix(Interactable __instance)
        {
            try
            {
                Highlighter.Hovered = __instance;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(HighlightOnHovered), e);
            }
        }
    }

    [HarmonyPatch(typeof(Interactable), "OnUnhovered")]
    internal static class HighlightOnUnhovered
    {
        private static void Postfix(Interactable __instance)
        {
            try
            {
                if (Highlighter.Hovered == __instance)
                {
                    Highlighter.Hovered = null;
                }
                Highlighter.OnUnhovered(__instance);
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(HighlightOnUnhovered), e);
            }
        }
    }

    // The overrides of the subclasses call the base, so these postfixes fire for each fixed object.
    [HarmonyPatch(typeof(StaticInteractable), "OnHovered")]
    internal static class HighlightOnHoveredStatic
    {
        private static void Postfix(StaticInteractable __instance)
        {
            try
            {
                Highlighter.HoveredStatic = __instance;
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(HighlightOnHoveredStatic), e);
            }
        }
    }

    [HarmonyPatch(typeof(StaticInteractable), "OnUnhovered")]
    internal static class HighlightOnUnhoveredStatic
    {
        private static void Postfix(StaticInteractable __instance)
        {
            try
            {
                if (Highlighter.HoveredStatic == __instance)
                {
                    Highlighter.HoveredStatic = null;
                }
            }
            catch (Exception e)
            {
                Plugin.WarnOnce(nameof(HighlightOnUnhoveredStatic), e);
            }
        }
    }
}
