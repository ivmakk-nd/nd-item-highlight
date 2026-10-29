# Copilot code-review instructions

This repo is a BepInEx 5 (Mono) Harmony mod for *Nomad Drive*, a co-op Unity game on Mirror. The plugin derives from `BaseUnityPlugin` and patches the game's `Assembly-CSharp.dll` at runtime. Review with these traps in mind; a general C# review misses most of them.

## Unity and Harmony traps

- **Unity null.** `UnityEngine.Object` overloads `==`, so a destroyed object compares equal to `null` only through `==` or its bool conversion. Flag `?.`, `??`, `is null`, and `is not null` on a Unity object (a `Component`, a `GameObject`, a `ScriptableObject`).
- **Guard game lookups.** Services, components, and singletons are often null during a scene load or in the main menu. Flag an unchecked dereference inside a patch.
- **A patch must not break the game.** Each patch body sits in a `try`/`catch` that logs the error once. Flag a patch body without one, and a log call inside a per-frame path with no once-only guard.
- **Reflection lookups can fail after a game update.** `AccessTools.Method`, `AccessTools.Field`, and `FieldRefAccess` by name return null or throw when the game renames a member. Flag a lookup whose result is used with no null check, and a lookup repeated on each call instead of cached in a static field.
- **Prefer a Postfix.** Flag a Prefix that returns `false` (skips the original) with no comment that says why.
- **State machines.** A patch of an `async` method or a coroutine runs only at its start; the work is in the `MoveNext` of the state machine. Flag a patch that expects to see the result of such a method.
- **Burst.** Flag a patch of an `Execute` method of a job with `[BurstCompile]`: it has no effect.

## Multiplayer (Mirror)

- **Woven methods.** The Mirror weaver moves the body of each `[Command]`, `[ClientRpc]`, and `[TargetRpc]` method into `UserCode_<Method>__<ParamTypes>`. Flag a patch on the original `Cmd`/`Rpc` method that expects to change what the receiver does.
- **SyncVars.** A `[SyncVar]` field syncs only when written through its `Network<field>` property, and only on the host. Flag a direct write of a SyncVar field, and a write on a client.
- **Side.** Flag server logic that runs with no `NetworkServer.active` or `isServer` check, and a change of shared state from a client.

## Performance

- Flag `FindObjectsByType`, `FindFirstObjectByType`, `GetComponentsInChildren`, `Camera.main`, LINQ, string building, or `Traverse` in an `Update`, `FixedUpdate`, or a patch of a per-frame or per-physics-step method.

## Structure and tests

- **Pure logic is separated and tested.** Logic that does not need the running game lives in its own file with no BepInEx, Unity, or game reference, unit-tested under `tests/`. Flag new pure logic in a patch class, and new pure logic with no test.
- **One patch class for each target method,** named `<Feature>On<Target>`.

## Release and config hygiene

- **Verbose ships off.** The `Verbose` config binds with default `false`. Diagnostic tracing goes on `LogDebug` behind it; `LogInfo` stays quiet apart from the load line.
- **The plugin GUID never changes.** It is the BepInEx identity and the config file name. Flag any edit to it.
- **The version is in two places that must agree:** `<Version>` in the csproj and the `BepInPlugin` attribute.
- **No committed build output.** Flag `bin/`, `obj/`, `dist/`, or a game DLL in the diff. Game `<Reference>` entries keep `<Private>false</Private>`.
- **No game source.** Flag a method body copied from the decompiled game code.
- **Changelog matches the change.** A player-visible change adds an `[Unreleased]` entry to `CHANGELOG.md` in player-facing wording. An internal-only refactor gets none.

## This mod

Item Highlight outlines the pickup items (`HeldItem`) near the camera while a key is held, with the game's own hover outline (`Interactable.EnableOutline` and `DisableOutline`, which move the item's renderers to the outlined layer).

- **Patch targets.** Postfixes on `Interactable.OnHovered` and `Interactable.OnUnhovered`, which only track the item under the crosshair. The per-frame work runs in `Plugin.Update`. The game members are checked once in `GameMembers.Check()` at `Awake`; a missing one turns the whole mod off with one warning (`UnpatchSelf`).
- **Local only.** The mod changes only what the local player sees. It sends nothing over the network and writes no game state. Flag any `Cmd`/`Rpc` call, any SyncVar write, and any change to an item other than its outline.
- **Search budget.** `FindObjectsByType<HeldItem>` runs only on key down and every `SearchInterval` seconds while the key is held (`KeyGate`). The refreshes in between re-check only the items of the last search. Flag a full search on each refresh or each frame, and per-frame work while the key is up.
- **Outline ownership.** `DisableOutline` clears the renderers of the item and of every item stacked under it, also the hovered one. So after any removal the mod enables the outline again on each kept item and restores the hover outline. Flag a removal path that skips this, and a release path that leaves an outline the mod added.
- **What gets no outline.** Items in or on a drivable vehicle (`NetworkedNWHVehicle` in the parents, or the cargo flag `IsVehicleColliderIsolated`), vehicle parts installed on a drivable vehicle (through the slot of `AttachableObject.ObjectSlot`), and items equipped, stowed, in flight, or carried by an equipped item. Wrecks have no `NetworkedNWHVehicle`, so their items get the outline. The decision lives in `HighlightLogic.ShouldHighlight`, unit-tested.
