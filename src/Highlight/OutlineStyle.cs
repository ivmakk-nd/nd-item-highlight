using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ItemHighlight
{
    // The color and the width of the game's outline pass. The materials are shared by all outlines of the Selection layer,
    // so the mod writes them only while the item highlight is on and writes the game's values back at the end.
    internal static class OutlineStyle
    {
        private const string ColorProperty = "_ContourColor";
        private const string WidthProperty = "_OutlineWidth";
        private const string MaxDistanceProperty = "_MaxDistance";

        private static Material _objects;
        private static Material _fullscreen;
        private static Color _gameColor;
        private static float _gameWidth;
        private static float _width;
        private static bool _applied;
        private static bool _warnedMissing;
        private static bool _warnedColor;

        internal static bool HasMaterials => _objects != null && _fullscreen != null;

        // Stores the game's values and writes the config values. Without the materials it writes nothing,
        // and the item highlight shows in the game's look.
        internal static void Begin()
        {
            if (_applied)
            {
                return;
            }
            try
            {
                if (!HasMaterials && !FindMaterials())
                {
                    if (!_warnedMissing)
                    {
                        _warnedMissing = true;
                        Plugin.Log.LogWarning("Item Highlight cannot find the outline materials of the game, so the outlines keep the game's color and width and have no fade.");
                    }
                    return;
                }
                _gameColor = _objects.GetColor(ColorProperty);
                _gameWidth = _fullscreen.GetFloat(WidthProperty);
                Color color = ConfigColor();
                float width = Plugin.OutlineWidth.Value;
                _width = width;
                _applied = true;
                _objects.SetColor(ColorProperty, color);
                _fullscreen.SetFloat(WidthProperty, width);
                if (Plugin.Verbose.Value)
                {
                    string maxDistance = _objects.HasProperty(MaxDistanceProperty) ? _objects.GetFloat(MaxDistanceProperty).ToString("F2") : "none";
                    Plugin.Log.LogDebug($"ItemHighlight: style begin, {_objects.name} {ColorProperty} {_gameColor} -> {color}, " +
                        $"{_fullscreen.name} {WidthProperty} {_gameWidth:F2} -> {width:F2}, {MaxDistanceProperty} {maxDistance}");
                }
            }
            catch (Exception e)
            {
                Drop(e);
            }
        }

        // The fade thins the outlines, because the outline shader does not use alpha.
        internal static void SetStrength(float strength)
        {
            if (!_applied)
            {
                return;
            }
            try
            {
                _fullscreen.SetFloat(WidthProperty, _width * strength);
            }
            catch (Exception e)
            {
                Drop(e);
            }
        }

        // Writes the game's values back. A no-op without a matching Begin, because the error path of Plugin.Update also ends here.
        internal static void End()
        {
            if (!_applied)
            {
                return;
            }
            _applied = false;
            try
            {
                if (_objects != null)
                {
                    _objects.SetColor(ColorProperty, _gameColor);
                }
                if (_fullscreen != null)
                {
                    _fullscreen.SetFloat(WidthProperty, _gameWidth);
                }
                if (Plugin.Verbose.Value)
                {
                    Plugin.Log.LogDebug($"ItemHighlight: style end, restored {ColorProperty} {_gameColor}, {WidthProperty} {_gameWidth:F2}");
                }
            }
            catch (Exception e)
            {
                Drop(e);
            }
        }

        // A fault falls to the path with no materials, so the item highlight still works in the game's look.
        private static void Drop(Exception e)
        {
            Plugin.WarnOnce(nameof(OutlineStyle), e);
            End();
            _objects = null;
            _fullscreen = null;
        }

        // The layer mask ties the choice to the pass that draws the outlines of the Selection layer.
        private static bool FindMaterials()
        {
            int selection = LayerMask.GetMask("Selection");
            foreach (CustomPassVolume volume in UnityEngine.Object.FindObjectsByType<CustomPassVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Material objects = null;
                Material fullscreen = null;
                foreach (CustomPass pass in volume.customPasses)
                {
                    if (pass is DrawRenderersCustomPass draw)
                    {
                        if (Plugin.Verbose.Value)
                        {
                            Plugin.Log.LogDebug($"ItemHighlight: pass {volume.name}/{pass.name} draw layers {draw.layerMask.value} (Selection {selection}) material {MaterialName(draw.overrideMaterial)}");
                        }
                        if ((draw.layerMask.value & selection) != 0 && draw.overrideMaterial != null && draw.overrideMaterial.HasProperty(ColorProperty))
                        {
                            objects = draw.overrideMaterial;
                        }
                    }
                    else if (pass is FullScreenCustomPass full)
                    {
                        if (Plugin.Verbose.Value)
                        {
                            Plugin.Log.LogDebug($"ItemHighlight: pass {volume.name}/{pass.name} fullscreen material {MaterialName(full.fullscreenPassMaterial)}");
                        }
                        if (full.fullscreenPassMaterial != null && full.fullscreenPassMaterial.HasProperty(WidthProperty))
                        {
                            fullscreen = full.fullscreenPassMaterial;
                        }
                    }
                }
                if (objects != null && fullscreen != null)
                {
                    _objects = objects;
                    _fullscreen = fullscreen;
                    return true;
                }
            }
            return false;
        }

        private static string MaterialName(Material material)
        {
            return material == null ? "none" : material.name;
        }

        private static Color ConfigColor()
        {
            if (!HighlightLogic.TryParseHexColor(Plugin.OutlineColor.Value, out float r, out float g, out float b, out float a))
            {
                if (!_warnedColor)
                {
                    _warnedColor = true;
                    Plugin.Log.LogWarning($"Item Highlight: OutlineColor \"{Plugin.OutlineColor.Value}\" is not a hex color, the outlines use {Plugin.DefaultOutlineColor}.");
                }
                HighlightLogic.TryParseHexColor(Plugin.DefaultOutlineColor, out r, out g, out b, out a);
            }
            return new Color(r, g, b, a);
        }
    }
}
