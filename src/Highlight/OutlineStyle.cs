using System;
using HarmonyLib;
using NomadDrive.Features.Interaction;
using UnityEngine;
using UnityEngine.Rendering;
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
        private const string HoverObjectsName = "ItemHighlight Hover Objects";
        private const string HoverFullscreenName = "ItemHighlight Hover Full Screen";

        private static CustomPassVolume _volume;
        private static Material _objects;
        private static Material _fullscreen;
        private static DrawRenderersCustomPass _hoverObjects;
        private static FullScreenCustomPass _hoverFullscreen;
        private static Color _gameColor;
        private static float _gameWidth;
        private static float _width;
        private static bool _applied;
        private static bool _warnedMissing;
        private static bool _warnedColor;
        private static bool _warnedHoverPass;

        // A scene change destroys the volume, and the next key down finds the materials and the passes again.
        internal static bool HasMaterials => _volume != null && _objects != null && _fullscreen != null;

        // The layer that only the separate outline pass of the hover target draws.
        internal static int HoverLayer { get; private set; } = -1;

        internal static bool HasHoverPass => HasMaterials && _hoverObjects != null && _hoverFullscreen != null;

        // The hover target stays visible as a normal object only when the camera also draws the layer.
        internal static bool CanDraw(Camera camera)
        {
            return HasHoverPass && camera != null && (camera.cullingMask & (1 << HoverLayer)) != 0;
        }

        internal static void SetHoverPass(bool on)
        {
            if (_hoverObjects != null)
            {
                _hoverObjects.enabled = on;
            }
            if (_hoverFullscreen != null)
            {
                _hoverFullscreen.enabled = on;
            }
        }

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
            SetHoverPass(false);
            _volume = null;
            _objects = null;
            _fullscreen = null;
            _hoverObjects = null;
            _hoverFullscreen = null;
        }

        // The layer mask ties the choice to the pass that draws the outlines of the Selection layer.
        private static bool FindMaterials()
        {
            int selection = LayerMask.GetMask("Selection");
            foreach (CustomPassVolume volume in UnityEngine.Object.FindObjectsByType<CustomPassVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Material objects = null;
                Material fullscreen = null;
                DrawRenderersCustomPass gameDraw = null;
                FullScreenCustomPass gameFullscreen = null;
                foreach (CustomPass pass in volume.customPasses)
                {
                    if (pass.name == HoverObjectsName || pass.name == HoverFullscreenName)
                    {
                        continue;
                    }
                    if (pass is DrawRenderersCustomPass draw)
                    {
                        if (Plugin.Verbose.Value)
                        {
                            Plugin.Log.LogDebug($"ItemHighlight: pass {volume.name}/{pass.name} draw layers {draw.layerMask.value} (Selection {selection}) material {MaterialName(draw.overrideMaterial)}");
                        }
                        if ((draw.layerMask.value & selection) != 0 && draw.overrideMaterial != null && draw.overrideMaterial.HasProperty(ColorProperty))
                        {
                            objects = draw.overrideMaterial;
                            gameDraw = draw;
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
                            gameFullscreen = full;
                        }
                    }
                }
                if (objects != null && fullscreen != null)
                {
                    _volume = volume;
                    _objects = objects;
                    _fullscreen = fullscreen;
                    SetUpHoverPass(volume, gameDraw, gameFullscreen);
                    return true;
                }
            }
            return false;
        }

        // Called before the mod writes its values, so the copies keep the game's color and width.
        private static void SetUpHoverPass(CustomPassVolume volume, DrawRenderersCustomPass gameDraw, FullScreenCustomPass gameFullscreen)
        {
            try
            {
                Camera camera = Camera.main;
                int layer = FreeLayer(camera);
                if (layer < 0)
                {
                    throw new InvalidOperationException("no free layer that the camera draws");
                }
                // The copy clears its color target, which must not be the frame.
                if (gameDraw.targetColorBuffer != CustomPass.TargetBuffer.Custom)
                {
                    throw new InvalidOperationException($"the game's outline draws to {gameDraw.targetColorBuffer}");
                }
                // Before the copies, so a failure of this call (a missing method without the unstripped module) adds no pass or material.
                CopyCollisionRow(LayerMask.NameToLayer("Selection"), layer);
                AddToHingeMask(layer);
                DrawRenderersCustomPass hoverObjects = FindCopy<DrawRenderersCustomPass>(volume, HoverObjectsName);
                FullScreenCustomPass hoverFullscreen = FindCopy<FullScreenCustomPass>(volume, HoverFullscreenName);
                if (hoverObjects == null || hoverFullscreen == null)
                {
                    hoverObjects = CopyObjectsPass(gameDraw);
                    hoverFullscreen = CopyFullscreenPass(gameFullscreen);
                    DestroyOldCopies();
                    volume.customPasses.Add(hoverObjects);
                    volume.customPasses.Add(hoverFullscreen);
                }
                hoverObjects.layerMask = 1 << layer;
                HoverLayer = layer;
                _hoverObjects = hoverObjects;
                _hoverFullscreen = hoverFullscreen;
                SetHoverPass(false);
                if (Plugin.Verbose.Value)
                {
                    Plugin.Log.LogDebug($"ItemHighlight: hover pass layer {layer}, camera mask {camera.cullingMask}, injection {volume.injectionPoint}, " +
                        $"objects color {gameDraw.targetColorBuffer} depth {gameDraw.targetDepthBuffer} game clear {gameDraw.clearFlags} copy clear {hoverObjects.clearFlags}, " +
                        $"full screen color {gameFullscreen.targetColorBuffer} depth {gameFullscreen.targetDepthBuffer} clear {gameFullscreen.clearFlags}");
                }
            }
            catch (Exception e)
            {
                _hoverObjects = null;
                _hoverFullscreen = null;
                if (!_warnedHoverPass)
                {
                    _warnedHoverPass = true;
                    Plugin.Log.LogWarning($"Item Highlight cannot draw a separate outline for the item under the crosshair, so that item fades with the others: {e.Message}");
                }
            }
        }

        // The highest layer with no name that the camera draws, or -1.
        private static int FreeLayer(Camera camera)
        {
            if (camera == null)
            {
                return -1;
            }
            for (int layer = 31; layer >= 0; layer--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)) && (camera.cullingMask & (1 << layer)) != 0)
                {
                    return layer;
                }
            }
            return -1;
        }

        private static T FindCopy<T>(CustomPassVolume volume, string name) where T : CustomPass
        {
            foreach (CustomPass pass in volume.customPasses)
            {
                if (pass is T copy && pass.name == name)
                {
                    return copy;
                }
            }
            return null;
        }

        private static DrawRenderersCustomPass CopyObjectsPass(DrawRenderersCustomPass game)
        {
            return new DrawRenderersCustomPass
            {
                name = HoverObjectsName,
                enabled = false,
                targetColorBuffer = game.targetColorBuffer,
                targetDepthBuffer = game.targetDepthBuffer,
                // The pass starts from an empty color target, so it does not hold the items of the game's pass.
                // A depth clear is safe only on the custom depth buffer: on the camera buffer it erases the depth of the frame.
                clearFlags = game.targetDepthBuffer == CustomPass.TargetBuffer.Custom ? ClearFlag.All : ClearFlag.Color,
                renderQueueType = game.renderQueueType,
                sortingCriteria = game.sortingCriteria,
                overrideMode = game.overrideMode,
                overrideMaterial = new Material(game.overrideMaterial),
                overrideMaterialPassName = game.overrideMaterialPassName,
                overrideShader = game.overrideShader,
                overrideShaderPassName = game.overrideShaderPassName,
                overrideDepthState = game.overrideDepthState,
                depthCompareFunction = game.depthCompareFunction,
                depthWrite = game.depthWrite,
                overrideStencil = game.overrideStencil,
                stencilReferenceValue = game.stencilReferenceValue,
                stencilWriteMask = game.stencilWriteMask,
                stencilReadMask = game.stencilReadMask,
                stencilCompareFunction = game.stencilCompareFunction,
                stencilPassOperation = game.stencilPassOperation,
                stencilFailOperation = game.stencilFailOperation,
                stencilDepthFailOperation = game.stencilDepthFailOperation,
                shaderPass = game.shaderPass,
            };
        }

        private static FullScreenCustomPass CopyFullscreenPass(FullScreenCustomPass game)
        {
            return new FullScreenCustomPass
            {
                name = HoverFullscreenName,
                enabled = false,
                targetColorBuffer = game.targetColorBuffer,
                targetDepthBuffer = game.targetDepthBuffer,
                clearFlags = game.clearFlags,
                fullscreenPassMaterial = new Material(game.fullscreenPassMaterial),
                materialPassName = game.materialPassName,
                fetchColorBuffer = game.fetchColorBuffer,
            };
        }

        // The copies of the last scene have no use after its volume is gone.
        private static void DestroyOldCopies()
        {
            if (_hoverObjects != null)
            {
                UnityEngine.Object.Destroy(_hoverObjects.overrideMaterial);
            }
            if (_hoverFullscreen != null)
            {
                UnityEngine.Object.Destroy(_hoverFullscreen.fullscreenPassMaterial);
            }
        }

        // The default mask of the hinge sweeps names Selection, so an object on the free layer must still block a door.
        // The property fills the field at its first read.
        private static void AddToHingeMask(int layer)
        {
            int mask = HingeSweep.DefaultMask;
            AccessTools.StaticFieldRefAccess<int>(typeof(HingeSweep), "_defaultMask") = mask | (1 << layer);
        }

        // An object on the free layer collides like an object on Selection.
        private static void CopyCollisionRow(int selection, int free)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                Physics.IgnoreLayerCollision(free, layer, Physics.GetIgnoreLayerCollision(selection, layer == free ? selection : layer));
            }
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
