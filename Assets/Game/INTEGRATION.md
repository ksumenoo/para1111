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
- Kenney light texture supplies the soul halo. Imported subset and original CC0 licenses: Assets/Game/ThirdParty/Kenney.
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
