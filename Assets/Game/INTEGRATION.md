# SECOND CHANCE — possession slasher

One top-down arena, three bodies, five-minute survival plus clearing the last wave. No upgrades or inventory.

## Play
- Scene: `Assets/Game/Scenes/Arena.unity`.
- Enter: start. WASD/arrows: move. Mouse: aim. Hold LMB: attack. Space/Shift: invulnerable dash.
- After lethal damage: E possesses the nearest living marked enemy within radius. Two REAL seconds, while simulation slows to 6%.
- R/Enter on results: restart. Escape: pause.
- Player damage marks enemies for 8 simulation seconds. Possession keeps the target's remaining health and weapon; marks clear between waves.
- Victory: survive 300 simulation seconds, then clear remaining enemies. Wave breaks count toward survival; death-choice slow motion does not accelerate survival.

## Ownership and extension points
- `GameSession`: sole owner of gameplay state. Input passes `PlayerCommand`; UI reads Session. Never set health or ownership in a view.
- `GameSettings.asset`: balance, three `BodyDefinition`s, arena bounds, timings. Body numbers are available in Inspector.
- `GameBootstrap`: VContainer composition root. `GameDriver`: Input System, lifecycle, view binding.
- `BodyView`: replace fallback visuals through `BodyDefinition.VisualPrefab`. Prefab forward +Z, origin at feet. Do not add gameplay movement scripts.
- `CombatFeedback` on scene object `Game`: Kenney clips assigned for hit/heavy hit, melee swing, shot, enemy/player death, dash and transfer. Master Volume starts at 0.65. All clips and volume are replaceable in Inspector; audio taste still needs user review. Spark/muzzle textures provide short impact particles.
- Audio fix: four fixed voices (impact / enemy death / attack / player-critical), no overlapping OneShots. Group retrigger spacing: 55 / 120 / 45 ms. Channel gains sum to 1 before master gain; player death stops the combat voices. Burst test: 20 hits + 20 deaths produced two active combat voices; player death then left one priority voice.
- Current light hit: impactMetal_light_001; heavy hit: impactMetal_heavy_000, selected by the user for listening. Enemy death uses soft impact instead of the long explosion. Metallic swing on misses is disabled pending a suitable whoosh.
- `PossessionView`: exposed cyan soul, capture-radius boundary, real-time countdown arc, selected-host tether and travelling spark/trail. It is presentation only and does not delay ownership transfer. Uses unscaled animation time; reset/scene teardown clears the effect.
- Selected-host ring pulses white; out-of-range marked bodies dim during the death window. Possession text sits below the fight.
- Player always has a thick cyan ground ring and overhead chevron, independent of body type. Enemy mark arcs shrink over their remaining simulation-time lifetime and turn amber near expiry.
- `AimView`: ground reticle, faint melee-sector/ranged-line preview, stronger committed-windup preview. Hidden while paused/dead/choosing a host; system cursor restored outside combat and on teardown.
- Kenney light texture supplies the soul halo. Imported subset and original CC0 licenses: Assets/Game/ThirdParty/Kenney.

## Imported character/environment art
- Duelist: Quaternius Punk; Brute: enlarged, broader SWAT; Gunner: Suit. Prefabs in Assets/Game/Prefabs; all use their own embedded generic-rig clips. Brute currently uses the pack's sword as a heavy-blade stand-in.
- BodyAnimation samples the windup portion of the attack, then plays recovery after the authoritative Attack cue. Movement/idle use Animator states; root motion is disabled. Game pause and death slow motion control animation speed.
- Weapon skin bones are remapped to the character rig. Material colors remain per submesh; role tint, hit flash and possession flash are overlays.
- Death detaches a short-lived animated visual; corpse animation never affects gameplay and is removed after about one second.
- Cyberpunk props/sign atlas and Kenney industrial buildings decorate the outside perimeter only. Neon lighting and a restrained bloom/vignette profile are in the scene.
- Backup: Arena_BeforeArt.unity references GameSettings_BeforeArt.asset, preserving the primitive visuals. Main build still uses Arena.unity only.
- Validated: 14 core checks, initialized animators, windup pose, pause, death clip, health-preserving transfer, corpse cleanup. Human animation/feel review remains needed.
- ArtChecks.Run verifies all 12 controller states use real runtime clips rather than __preview__ assets, root motion is disabled, and character/weapon bones belong to their prefab hierarchy.
- `GameSession.Combat` event: `Windup`, `Attack`, `Hit`, `Death`, `Dash`, `Possessed`. Event has Body/Other, world Position/Direction, damage Amount. Body can already have zero HP on Death; avoid storing mutable snapshots for later playback.
- `Spawned(BodyState)`, `PhaseChanged(RunPhase)`: integration events.
- Player ring cyan; wounded enemy ring pink; possession target white. Enemy red sector is the committed attack direction; dodge after it appears.
- Do not use Time.timeScale for effects: simulation is deliberately scaled in GameSession. UI and transfer timer stay on real time.

## Workflow
- Main code: `Assets/Game/Runtime`; editor-only utilities/checks: `Assets/Game/Editor`.
- `Second Chance/Create Arena (first time)` creates a NEW arena and makes it the build scene; refuses to overwrite an existing arena.
- No NavMesh needed: open rectangular arena, planar movement, body separation. Keep arena geometry inside the rectangle unobstructed unless collision support is added.
- `SecondChance.Editor.CoreChecks.Run()` verifies the critical possession rules without playing a full run.
- Build configured game scenes: `./Build-Windows.ps1`.
- Theme, rules, and scope approved. Priorities: responsive hits/death → reliable transfer → complete run → polish.
