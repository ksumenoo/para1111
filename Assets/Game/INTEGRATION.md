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
- `CombatFeedback` on scene object `Game`: assign optional hit/death/dash/transfer/attack audio clips in Inspector. The component supplies hit flash, attack arcs, impact rings and camera shake. No audio files are bundled yet.
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
