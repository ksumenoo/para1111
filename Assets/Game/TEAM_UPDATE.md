# Integrated teammate update

Source: user-provided para1111.zip. Kept local scenes, art imports, packages, close follow camera and expanded Russian rooms.

- Ordinary enemies stagger and cancel windup on a player hit; Brutes resist stagger and push harder. Duelists dash more often.
- Kill within two simulation seconds after possession to trigger a five-second damage/speed surge. Further kills extend it.
- Authored six-wave composition, breakable crates, explosive canisters, debris and bounded floor marks.
- Last room ends with the Baron: three hosts, Brute -> Duelist -> Gunner. Possessing his host also removes one boss life.
- Generated four-layer music follows combat/death/pause. Generated cue clips fill missing sounds; assigned Kenney impact clips are preserved.
- Additional dressing: seven selected Quaternius models, clinic tools/screens/signs, supply pallets, industrial tanks/cables, server consoles, floor clutter and perimeter panels. Solid supplies use ArenaLayout collision; litter stays flat.

Local integration fixes: projectile bounds use each room's actual footprint; generated audio clips are released on teardown; music gains leave headroom alongside the existing four combat voices; new model materials remapped to URP.

Validated in Unity 6000.6.0f1: 14 core checks, world checks, FeelChecks (stagger, knockback, chain, authored waves, breakables, three-body boss). Scripted Play-mode traversal reached victory through all three rooms and all three boss hosts with zero Console errors. Human balance/music review remains useful.

Windows build succeeded with zero errors and two existing debug-occlusion shader warnings; standalone startup smoke passed without exceptions. The interactive Editor crashed in UnityEditor.Search.LMDBIndexStorage during indexing; batch validation/build succeeded. Disabled indexOnEditorStartup in local UserSettings/Search.settings to avoid repeating that startup work (not a shipped setting).
