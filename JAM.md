# Game jam — 11 November 2026

## Scope
- 6 hours. Theme announced at the start.
- Deliver a Windows build; other formats are allowed.
- One main mechanic, one short level, clear win/lose/restart.
- Check jam rules before reusing prepared code or external assets.
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
- DOTween Free: useful for UI and object feedback; not installed yet.
- Kenney: choose a small matching art/audio pack after the theme is known.
- Avoid large asset imports and complex frameworks for a six-hour project.

## Preparation result
- Windows test build succeeded: 161 seconds, 0 errors, 2 warnings about stripped internal debug-occlusion shaders.
- Output: `Builds/Windows/Game.exe`. Process startup checked in background without graphics; the standalone visuals and controls are not tested.
- First build compiled shaders; retain Library/ to keep the local cache.
- Unity serialized changes to rendering/input/cloud settings and created performance-test Resources during preparation. These changes remain uncommitted for review; do not discard them automatically.
- Initial Git checkpoint exists. No remote repository configured.
