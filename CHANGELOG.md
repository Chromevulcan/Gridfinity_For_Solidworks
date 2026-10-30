# Changelog

## 1.1.0
- Baseplate rebuilt as an open frame measured from a connectable baseplate STL: bossed base layer, magnet pockets (diameter, thickness, floor), screw holes, connection slots with per-side switches.
- New Clips tab / `ClipDefinition` / `GridfinityBuilder.BuildClips`; clip profile taken from the Connection Clip STL.
- New `Cradle` pocket shape (round bottom) built from the Top plane, with a scratch-part direction probe.
- Pocket floors in solid blocks must clear the 4.75 mm Gridfinity feet (validator error otherwise).
- Debossed labels: sketch-text call reworked, typed and late-bound attempts, detailed log lines.
- Add-in menu created once after SolidWorks idles, with saved command-group settings preserved (fixes the missing Tools menu).
- Inno Setup installer with SolidWorks folder detection and confirmation.
- Log moved to `%LocalAppData%\GridfinityForSolidWorks\Logs`.
- Removed: baseplate `Style` / `FloorThickness` (old files still load).

## 1.0.0
- First release: box, pockets, auto-layout, labels, baseplate, add-in shell.
