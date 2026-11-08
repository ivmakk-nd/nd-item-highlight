using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NomadDrive.Features.Attachables;
using NomadDrive.Features.Interaction;
using UnityEngine;

namespace ItemHighlight
{
    [BepInPlugin(PluginGuid, "Item Highlight", "1.1.0")]
    [BepInProcess("Nomad Drive.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.ivmakk.nomaddrive.itemhighlight";

        internal static ManualLogSource Log;
        internal static Harmony Harmony;
        internal static ConfigEntry<KeyboardShortcut> HighlightKey;
        internal static ConfigEntry<float> Radius;
        internal static ConfigEntry<float> SearchInterval;
        internal static ConfigEntry<string> OutlineColor;
        internal static ConfigEntry<float> OutlineWidth;
        internal static ConfigEntry<float> FadeDelay;
        internal static ConfigEntry<bool> Verbose;

        internal const string DefaultOutlineColor = "#FFFFFF";

        private static readonly HashSet<string> WarnedParts = new HashSet<string>();
        private static bool _active;

        private void Awake()
        {
            Log = Logger;
            HighlightKey = Config.Bind("General", "HighlightKey", new KeyboardShortcut(KeyCode.LeftAlt),
                "Hold this key to outline the items you can pick up near you.");
            Radius = Config.Bind("General", "Radius", 50f, new ConfigDescription(
                "How far from you, in meters, items get the outline.",
                new AcceptableValueRange<float>(5f, 100f)));
            SearchInterval = Config.Bind("General", "SearchInterval", 2f, new ConfigDescription(
                "While you hold the key and during the fade, how often, in seconds, the mod looks for items that are new in the world (loot that loads in while you drive, " +
                "an item another player drops). Items it already found react within half a second. A higher value costs less CPU, " +
                "and new items take longer to get the outline.",
                new AcceptableValueRange<float>(1f, 10f)));
            OutlineColor = Config.Bind("General", "OutlineColor", DefaultOutlineColor,
                "The color of the outlines while you hold the key and during the fade, as a hex color: #RRGGBB. The # is optional. Colors to try: #7CFF4F, #00D8FF. The item or door under the crosshair always keeps the game's own outline.");
            OutlineWidth = Config.Bind("General", "OutlineWidth", 2f, new ConfigDescription(
                "The thickness of the outlines while you hold the key. The fade thins them from this value. The game's own outline is 4.6. The item or door under the crosshair always keeps the game's own thickness.",
                new AcceptableValueRange<float>(1f, 8f)));
            FadeDelay = Config.Bind("General", "FadeDelay", 3f, new ConfigDescription(
                "How long, in seconds, the outlines take to fade after you release the key. The fade starts at the release. At 0 the outlines go off at once.",
                new AcceptableValueRange<float>(0f, 60f)));
            Verbose = Config.Bind("General", "Verbose", false, "Log extra detail at Debug level. Keep off in normal play.");

            Harmony = new Harmony(PluginGuid);
            try
            {
                GameMembers.Check();
                Harmony.PatchAll();
            }
            catch (Exception e)
            {
                Harmony.UnpatchSelf();
                Log.LogWarning($"Item Highlight is off, the game works as without it. A game member it needs is missing: {e.Message}");
                return;
            }
            HighlightKey.SettingChanged += (sender, args) => Highlighter.CacheKey(HighlightKey.Value);
            Highlighter.CacheKey(HighlightKey.Value);
            _active = true;
            Log.LogInfo("Item Highlight loaded.");
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }
            try
            {
                Highlighter.Tick();
            }
            catch (Exception e)
            {
                WarnOnce(nameof(Highlighter), e);
                try
                {
                    Highlighter.ReleaseAll();
                }
                catch (Exception releaseError)
                {
                    WarnOnce(nameof(Highlighter.ReleaseAll), releaseError);
                }
            }
        }

        // Logs the first error of each part once, so a fault in a per-frame part does not flood the log.
        internal static void WarnOnce(string part, Exception e)
        {
            if (WarnedParts.Add(part))
            {
                Log.LogWarning($"{part} failed, later errors of it are not logged: {e}");
            }
        }
    }

    // The game members the mod calls. A game update can rename one, so Awake checks them before it patches.
    internal static class GameMembers
    {
        internal static void Check()
        {
            Require(AccessTools.Method(typeof(Interactable), nameof(Interactable.EnableOutline)), "Interactable.EnableOutline");
            Require(AccessTools.Method(typeof(Interactable), nameof(Interactable.DisableOutline)), "Interactable.DisableOutline");
            Require(AccessTools.Method(typeof(Interactable), "OnHovered"), "Interactable.OnHovered");
            Require(AccessTools.Method(typeof(Interactable), "OnUnhovered"), "Interactable.OnUnhovered");
            Require(AccessTools.Method(typeof(StaticInteractable), nameof(StaticInteractable.OnHovered)), "StaticInteractable.OnHovered");
            Require(AccessTools.Method(typeof(StaticInteractable), nameof(StaticInteractable.OnUnhovered)), "StaticInteractable.OnUnhovered");
            Require(AccessTools.PropertyGetter(typeof(Interactable), nameof(Interactable.IsVehicleColliderIsolated)), "Interactable.IsVehicleColliderIsolated");
            Require(AccessTools.PropertyGetter(typeof(Interactable), nameof(Interactable.AvailableForInteraction)), "Interactable.AvailableForInteraction");
            Require(AccessTools.PropertyGetter(typeof(HeldItem), nameof(HeldItem.IsEquipped)), "HeldItem.IsEquipped");
            Require(AccessTools.PropertyGetter(typeof(HeldItem), nameof(HeldItem.IsStowed)), "HeldItem.IsStowed");
            Require(AccessTools.PropertyGetter(typeof(HeldItem), nameof(HeldItem.IsInFlight)), "HeldItem.IsInFlight");
            Require(AccessTools.PropertyGetter(typeof(AttachableObject), nameof(AttachableObject.IsAttached)), "AttachableObject.IsAttached");
            Require(AccessTools.PropertyGetter(typeof(AttachableObject), nameof(AttachableObject.ObjectSlot)), "AttachableObject.ObjectSlot");
        }

        private static void Require(MethodInfo method, string name)
        {
            if (method == null)
            {
                throw new MissingMethodException(name);
            }
        }
    }
}
