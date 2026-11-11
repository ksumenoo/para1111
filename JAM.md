# Game jam — 11 November 2026

## Scope
- 6 hours. Theme: Second Chance. Approved game: top-down possession slasher.
- Deliver a Windows build; other formats are allowed.
- Current mode: three authored rooms, two waves each, walls and cursor-facing field of view; E at the cleared-room exit. Three bodies, attack, dash, lethal-hit possession, win/lose/restart. No progression/inventory/procedural generation. User explicitly requested this extension after testing the arena.
- AI owns code/integration/builds; user owns visual direction, sound, feel, and hands-on testing. Main scene edits are authorized.
- Current implementation / controls / replacement points: Assets/Game/INTEGRATION.md.
- Main scene: Assets/Game/Scenes/Arena.unity. Build-Windows.ps1 now builds the game by default.
- 14 core gameplay checks cover combat and possession; live possession into Brute preserved 115 HP. Full five-minute human balance/feel test remains necessary.
- Current arena build: build_95d2907a2e09, Succeeded in 13.4 seconds, 0 errors / 0 warnings. Windows player startup checked without graphics; no initialization exceptions in Temp/arena-player.log.
- User confirmed jam rules allow external assets, prepared code, and AI assistance.
- Keep assistant replies and tool output short. Read only relevant files; avoid repeated discovery.

## Time budget
- 00:00–00:20: theme, idea, minimum scope.
- 00:20–02:00: playable core; first game build.
- 02:00–03:30: complete level and game loop.
- 03:30–04:30: sound, feedback, UI.
- 04:30–05:30: playtest, fixes, final build.
- 05:30–06:00: submission and buffer. No new features.

## Project and verified tools
- Unity 6000.6.0f1, Input System enabled; URP package installed.
- Unity Pipeline 1.0.0-pre.1 and Unity CLI are available.
- Commands need access to the host user session; isolated execution can falsely report no Editor.
- CLI requires the full command name as ONE quoted argument.
- This installed version does not support --caller / --skill.
- Verified: scene creation, materials, animation assets, hierarchy reads, Play start/stop, frame and transform changes, screenshots, Console reads.
- SampleScene is the original template. ConnectionTest is only a tool-check scene.
- Do not install packages or update Unity during the jam without a concrete need.

## Known commands (run from project root)
```powershell
unity status --format json
unity command 'scene get_hierarchy' --format json
unity command 'console status' --format json
unity command 'console get' --tail 10 --level error --format json
unity command 'editor.playmode start' --format json
unity command 'capture game_view' --output-path Temp/playtest.png --format json
unity command 'editor.playmode stop' --format json
```
For unfocused testing use `editor set_auto_tick` and verify that frames advance; a screenshot alone is not proof.

## Build
```powershell
# Build scenes enabled in Build Settings:
.\Build-Windows.ps1
# Or explicitly build one scene:
.\Build-Windows.ps1 -Scene Assets/ConnectionTest/ConnectionTest.unity
unity command 'build status' --format json
```
The script queues an asynchronous build. Only a final `Succeeded` report proves completion.
Output: `Builds/Windows/Game.exe`. Submit the entire Windows folder, not the EXE alone.
Before final submission: launch the build, verify controls, win/lose/restart, sound, and quitting.

## Optional resources
- Installed: DOTween Free 1.3.045 (Setup complete, UI + TextMeshPro modules, ASMDEF), UniTask 2.5.11, VContainer 1.19.0.
- UniTask and VContainer Git tags are pinned in Packages/manifest.json; resolved commits are in packages-lock.json.
- Basic API checks passed: VContainer resolves a registered instance, UniTask returns a result, DOTween reaches its target value.
- Kenney: choose a small matching art/audio pack after the theme is known.
- Avoid large asset imports and complex frameworks for a six-hour project.

## Preparation result
- Rebuilt after installing DOTween/UniTask/VContainer: Succeeded, 33.7 seconds, 0 errors; same 2 debug-occlusion shader warnings. Build ID: build_d4ed5cb6e54e. This builds ConnectionTest; it does not exercise all library features in the player.
- Windows test build succeeded: 161 seconds, 0 errors, 2 warnings about stripped internal debug-occlusion shaders.
- Output: `Builds/Windows/Game.exe`. Process startup checked in background without graphics; the standalone visuals and controls are not tested.
- First build compiled shaders; retain Library/ to keep the local cache.
- Unity serialized changes to rendering/input/cloud settings and created performance-test Resources during preparation. These changes remain uncommitted for review; do not discard them automatically.
- Initial Git checkpoint exists. No remote repository configured.
# Camera and teammate prototype

- Cinemachine 6.6 follows a stable player proxy; orthographic size 5.6, damping 0.2. Camera center is clamped to the room using viewport dimensions. Room changes/restarts snap; possession follows the new body. Combat shake is additive in the Cinemachine pipeline.
- Compared https://github.com/evansfiet-sketch/- : keep this project's combat/rooms/visibility/animated-body implementation. Useful material to adapt next: Oттиск storyline, clinic props, memory messages, Baron and alternate endings. These are not integrated yet; avoid importing a second GameManager/input/combat system.
