# Release candidate 0.3.0

- HUD safe area and text fitting respond to resolution changes; status/boss panels use separate regions. Long state screens have more room. Checked 25 state/resolution combinations (800x600, 1280x720, 1920x1080, 2560x1080, 720x1280), plus exit-guide visibility and pause.
- Cyan exit arrow and distance show after room clear, including when the exit is offscreen. Clear text moves below the fight.
- Waves: 6, 9, 12, 15, 18, 22 enemies; authored/fallback spawns capped at 24. Fast combat balance retained.
- Static readable room meshes combined (221 renderers in clinic); HUD text throttled while bars/flash/exit guide remain per-frame. Dynamic sight mesh marked dynamic. Four combat voices, four quiet music layers, 90 floor-mark cap and projectile-view reuse retained. Frame rate capped at 120. Focus loss pauses the session; Q exits from pause.
- Resizable window, Alt+Enter, 1280x720 default, fullscreen window; no changes to render scale or texture quality on resize.
- Release checks: core/world/feel passed; 22-enemy CPU simulation approximately 0.10 ms/tick on this machine, without GPU measurement. Fixed the boss-possession test's random navigation dependency with a controlled nearby host fixture; retained its assertions.
- Windows build succeeded, zero errors, two existing internal debug-occlusion shader warnings. Final gameplay balance and GPU frame rate require a normal playtest.

Rebuild: Unity execute method `SecondChance.Editor.ReleasePreparation.Build` (runs configuration, checks and release build). Full Windows ZIP excludes Unity's backup/debug-only folder.
