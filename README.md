# Gridfinity for SolidWorks

A SolidWorks add-in (and C# API) that builds parametric [Gridfinity](https://gridfinity.xyz) boxes, part-holder trays,
baseplates and connection clips from a window inside SolidWorks or from a saved JSON file.

**Version 1.1.0** · Windows only · SolidWorks 2026 (64-bit) · .NET Framework 4.8

## What it does

| Tab | Builds |
|---|---|
| **Box** | A Gridfinity bin of any grid size and height, solid or open tray, stacking lip, magnet holes, walls, dividers. |
| **Baseplate** | An open-frame baseplate with optional magnet pockets (any diameter and thickness), screw holes and connection slots. |
| **Cutout** | Pockets cut into a box: rectangles, circles, slots, hexagons, polygons and round-bottomed cradles, with auto-layout and debossed text labels. |
| **Clips** | The connection clips as a part of their own, as many as you want. |

Everything can be saved to and built from JSON (Tools > Gridfinity > Build from JSON file...). `samples\` has examples,
including a 73-socket 1/2-inch-drive tray in `trays\`.

## Install

**With the installer** (if you were given `GridfinityForSolidWorks-Setup.exe`): run it as administrator, confirm your SolidWorks
folder, confirm the install folder, then start SolidWorks and open **Tools > Gridfinity**. If the menu is missing, tick
*Gridfinity Tray Builder* under **Tools > Add-Ins**.

**From source:** see *Build and install* below, or run `scripts\make-installer.cmd` (needs Inno Setup 6.3+ or 7) to make the installer.

## What's new in 1.1.0

- **Baseplate rebuilt** from a connectable plate exported from gridfinitygenerator.com: always an open frame, with magnets
  (diameter, thickness), screw holes, and connection slots with per-side switches.
- **Clips tab** that builds the connection clips as a separate part.
- **Round-bottomed cradles** for cylindrical parts such as sockets, with floors kept above the Gridfinity feet.
- **Debossed labels** now build (sketch-text call reworked).
- **Add-in menu fix:** the Tools > Gridfinity menu is created once, with saved settings preserved (contributed by the project owner).
- **Installer** that finds your SolidWorks folder and asks you to confirm it.
- Log file moved to `%LocalAppData%\GridfinityForSolidWorks\Logs\Gridfinity_addin.log`.

See [CHANGELOG.md](CHANGELOG.md).

## Tested, and not tested

Tested by the author in SolidWorks Design 2026: box and baseplate builds (several sizes), magnets, screws, slots, cradles,
labels, the clip part, and the add-in loading. **Not yet tested:** the installer on a second computer, other SolidWorks
versions, and whether printed clips fit the printed slots (print a short test joint before a large plate). Nothing has been
tried on a SolidWorks edition other than Professional for Makers.

## Help wanted: please try it and tell me how it went

If you install it, please open an [issue](../../issues) with:

1. Your SolidWorks version and edition, and Windows version.
2. Whether the installer found your SolidWorks folder.
3. Whether **Tools > Gridfinity** appeared on its own, or only after ticking the add-in under Tools > Add-Ins.
4. What you built, and anything that looked wrong.
5. The last lines of `%LocalAppData%\GridfinityForSolidWorks\Logs\Gridfinity_addin.log`.

## Limits

Windows only. Built and tested against SolidWorks 2026; other years may need a rebuild against their interop files. Dimensions follow
the commonly published Gridfinity profiles, not an official CAD file, so print one cell and check a bin drops in.

---

## Layout

| Project | Target | Purpose |
|---|---|---|
| `src/Gridfinity.Core` | netstandard2.0 | No SolidWorks dependency. Gridfinity spec, pocket shapes, auto-layout, validation, and the **build plan** (an ordered list of sketch + extrude/cut features). |
| `src/Gridfinity.Sw` | net48, x64 | Replays a build plan through the SolidWorks API; the `ISwAddin` shell; a PropertyGrid designer dialog. |
| `tests/Gridfinity.Core.Tests` | net8.0 | xUnit tests for the core (dimensions, closed contours, layout, validation, JSON). |

All dimensional logic is in Core, so it can be tested and debugged without SolidWorks, and the same plan
could drive another CAD system.

## Build and install

1. Install Visual Studio 2022 (or the .NET SDK plus the .NET Framework 4.8 targeting pack).
2. Make sure `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist` has the three interop DLLs, or build with
   `-p:SolidWorksApiDir="<your path>"`.
3. `dotnet test` (core tests), then build the solution in **Release**.
4. From an **administrator** prompt: `scripts\register.cmd`. Restart SolidWorks.
5. A **Gridfinity** menu appears with *New Gridfinity tray...* and *Build tray from JSON file...*.
   Use `scripts\unregister.cmd` to remove it.

SolidWorks add-ins need the full API. I could not confirm whether your "SolidWorks Design" edition allows
add-ins and macros; if the add-in will not load, check that first.

## The designer window (Tools > Gridfinity > Gridfinity designer...)

| Tab | What it holds |
|---|---|
| **Box** | The bin itself: grid size (UnitsX, UnitsY), HeightUnits, style (SolidBlock or OpenTray), stacking lip, magnet holes, walls, floor height, compartments and dividers. |
| **Baseplate** | A baseplate, always an open frame: UnitsX, UnitsY; **Magnets** (on/off, diameter, thickness, material under the magnet); **Screws** (on/off, diameter); **Connection** (slots on/off, which sides, clearance); **Clip length** for the slots. |
| **Clips** | The connection clips as a part of their own: how many (side by side) and their length. `samples\clips-6.json` is an example. |
| **Cutout** | What is cut into the box: the pocket list (rectangle, circle, slot, hex, polygon, with depth, clearance, position and finger notch), the auto-layout settings, and the debossed labels. |

Box and Cutout edit the same definition, so **Build box** on either tab builds the box with its cutouts and labels.
**Build baseplate** builds the Baseplate tab. *Open JSON* detects whether a file is a box or a baseplate and switches tab;
*Save JSON* saves whichever tab you are on. The menu command *Build from JSON file* does the same without opening the window.
`samples\baseplate-4x3.json` is an example baseplate with magnets, screws, slots and six clips.

### Baseplate details (measured from the site's own plate)

The baseplate geometry was read from a connectable 2 x 2 plate exported from gridfinitygenerator.com, so it follows that plate:

- **Height:** a 3 mm lower layer plus the 4.65 mm bin-shaped profile (2.15 chamfer, 1.8 vertical, 0.7 chamfer), 7.65 mm in all.
- **Lower layer:** every cell is open all the way through, apart from a rounded boss in each corner (4.5 mm radius, centred 13.5 mm from the cell centre).
- **Magnets:** a pocket in each boss, opening upward so the magnet drops in from the top. Defaults 6.2 mm diameter x 2.1 mm thick, with 0.9 mm of material under it. The lower layer thickens automatically if you choose a thicker magnet.
- **Screws:** a 3.5 mm hole through that 0.9 mm (or through the whole layer when magnets are off).
- **Connection slots:** a 20 mm notch (the 19.6 mm clip plus 0.4 mm) in the outer wall at the middle of every cell edge, with a stepped hook that traps the clip. Because they sit on the 42 mm grid, plates of different sizes line up. Each side can be switched off.
- **Clips:** the clip profile (4.3 mm wide, 3.6 mm tall, hollow underside, 19.6 mm long) comes from the site's Connection Clip STL. Build them from the **Clips** tab: one part holding as many clips as you ask for. (They are a separate build because a second part opening on top of the plate looked as if the plate had not been made.) The slot is cut in steps rather than with the slope the site uses, so test-print one joint before printing a big plate.

`WebAtTop` is still the number to change if bins fit too tight or loose.

## Using the API

```csharp
using Gridfinity.Core;
using Gridfinity.Sw;

var tray = new TrayDefinition
{
    Name = "SocketTray",
    UnitsX = 3, UnitsY = 2, HeightUnits = 3,
    Style = TrayStyle.SolidBlock,     // or OpenTray (walls, cavity, dividers, stacking lip)
    MagnetHoles = true,
    DefaultClearance = 0.4,
};
tray.Pockets.Add(Pocket.Round("1/2 in socket", 24.0, depth: 14));
tray.Pockets.Add(Pocket.SlotPocket("Ratchet", 150, 30, depth: 10));
tray.Pockets.Add(Pocket.Rect("Extension", 100, 14, depth: 8, cornerRadius: 2));

// Without SolidWorks: validate and inspect the plan.
CompileResult c = TrayCompiler.Compile(tray);       // auto-layout + validation, never mutates `tray`
Console.WriteLine(c.Report);                         // errors and warnings
foreach (var f in c.Plan.Features) Console.WriteLine(f.Name);

// With SolidWorks (swApp is an ISldWorks):
SwBuildResult r = new GridfinityBuilder(swApp).Build(tray, new SwBuildOptions { SaveAsPath = @"C:\Parts\SocketTray.sldprt" });
```

JSON in and out: `tray.ToJson()`, `TrayDefinition.FromJson(text)`. See `samples\`.

### Pockets

| Shape | `Width` | `Height` |
|---|---|---|
| `Rectangle` | length (local X) | width (local Y), optional `CornerRadius` |
| `Circle` | diameter | ignored |
| `Slot` | overall length | slot width |
| `Hex` | across flats | ignored |
| `Polygon` | (unused) | convex `Points` list, JSON only |
| `Cradle` | part diameter (X) | part length (Y); half-round bottom, `Depth` is to the lowest point, no rotation |

Each pocket also has `Depth`, `Clearance` (overrides the tray default), `X`/`Y`/`RotationDeg` (used when
`AutoLayout` is off), `AllowRotate`, and an optional round finger notch (`NotchDiameter`, `NotchOffset`).

### Layout

- `AutoLayout` (default on) shelf-packs pockets, rotating them 90 degrees where that helps.
- `AutoGrowGrid` finds the smallest `UnitsX` x `UnitsY` that fits everything.
- Turn `AutoLayout` off to place pockets by hand with `X`, `Y`, `RotationDeg`.
- Validation errors (overlap, outside the usable area, too little material under a pocket, thin walls) stop the
  build before SolidWorks is touched; warnings are returned in `SwBuildResult.Warnings`.

## What gets built

Sketches are on the Front Plane, extruded along +Z, with the bin centred on X/Y = 0 (so the part appears on its
side in SolidWorks' default Y-up view; re-orient if you prefer).

1. Per cell, three extrudes form the base: 0.8 mm 45-degree chamfer, 1.8 mm vertical, 2.15 mm 45-degree chamfer
   (35.6 mm up to 41.5 mm, corner radius 0.8 up to 3.75 mm), using draft angles.
2. Body block from 4.75 mm to the top.
3. `OpenTray`: cavity cut (with dividers), then four cuts forming the 4.4 mm stacking lip if enabled.
4. Pockets and notches, one cut per depth.
5. Debossed labels (one text cut, 0.8 mm deep by default).
6. 6.5 x 2.4 mm magnet pockets at 13 mm from each cell centre.

Assumptions you can change in `GridfinitySpec.cs` / `TrayModeler.cs`: height units are measured to the top of
the walls with the lip added on top; the lip top land defaults to 0.8 mm (`LipTopInset`). The lip follows the
commonly published 0.7 / 1.8 / 1.9 mm profile; print a test stack before trusting fit.

## If the Gridfinity menu is missing

The add-in writes a progress log to `%LocalAppData%\\GridfinityForSolidWorks\\Logs\\Gridfinity_addin.log` (paste that path into File Explorer's address bar). It says whether
the command group and menu items were created. Check **Tools > Add-Ins** that *Gridfinity Tray Builder* is ticked, and look
under **Tools > Gridfinity** with a part open.

## First-run checklist

- **Compile errors.** Most likely spots are the long `FeatureExtrusion3` / `FeatureCut4` argument lists in
  `FeatureExecutor.cs`; the compiler will show the expected signature.
- **Direction probe.** Before each build the add-in makes a scratch part (closed again without saving),
  measures which way extrudes, cuts and start offsets go, and adapts. Anything unexpected shows up in the
  warnings. Set `SwBuildOptions.RunDirectionProbe = false` to skip it.
- **Overall size check.** After the build, the part box is compared with the planned size; a mismatch is
  reported as a warning.
- **Drafted extrudes with a start offset** (base chamfers, lip cuts) are the most likely place for SolidWorks to
  behave differently from what is assumed. If the base looks wrong, inspect the first three features.
- **Icons.** The commands are menu-only and have no icon list.

## Ideas for next steps

Label recesses or engraved text, scoops and fingertip cut-outs on open trays, baseplate generation, a table of
tool dimensions (for example the Quinn set) fed in as JSON, and exporting STL/3MF straight from the build.

## Socket tray (1/2" drive)

`trays\Sockets_1-2in_ALL.json` is one tray holding all 73 sockets from `data\sockets_half_inch_drive.csv`. Open it with
**Tools > Gridfinity > Build from JSON file...** (about 130 grid cells, 73 cradles and 79 labels, so the build takes a
while; the cradles are cut from the Top plane, and the add-in first checks in a scratch part how that plane behaves). `trays\SUMMARY.md` has the sizes and `trays\PREVIEW_top_view.png` shows the layout.

- **Size:** 16 x 8 cells, 671.5 x 335.5 mm (26.4 x 13.2 in), 42 mm tall; limit used: 51.5 x 19.75 in.
- **Layout:** SAE on the left half, Metric on the right, everything in each half justified to the left. Top to bottom:
  6-point (wrapped onto two rows, small to large), 12-point, then the 3.125 in deep wells at the bottom (the front of
  the drawer). A title is centred above each section and each socket's size is centred below it. Rows line up across
  the two halves.
- **Sockets lie flat**, long axis front to back, each in a round-bottomed cradle (`Shape: Cradle`): a half-round trough
  with straight sides, sized to the socket's maximum diameter plus 0.4 mm total (0.2 mm per side) and its length plus 0.4 mm.
  0.4 mm is a safe FDM fit; 0.002 in (0.05 mm) would be tighter than a printer can hold. Change it with `--clearance`.
- **Level tops:** every socket top sits 47.5 mm above the tray base. A socket resting in its cradle has its top at *lowest
  point + diameter*, so `depth = tray height - 47.5 + diameter`. The lowest point of the largest cradle is 6 mm up (the
  4.75 mm feet plus 1.25 mm of floor), which stops the big sockets breaking through the bottom. The tray is 42 mm tall so
  every cradle is at least as deep as its radius; the sockets stand about 5.6 mm above the tray top.
- **Labels** are debossed 0.8 mm (four layers at 0.2 mm) for a colour change or paint fill.
- The SAE 12-point lengths (blank in the original sheet) use 1.500 in, as confirmed.

To change sizes, edit the CSV and run `python tools\make_socket_tray.py`, then `python tools\check_socket_tray.py`
(needs Python; not needed to use the supplied file). Options: `--max-width-in`, `--max-depth-in`, `--clearance`,
`--center-deep` (centre the deep rows instead), `--wrap 2,1,1` (rows for 6-point, 12-point, deep), `--optimize-all`
(also try wrapping the 12-point and deep rows; it saves only about 1.5 % of the area).

Label centring uses the widths of Arial Bold and assumes the sketch-text size is the font size. If the labels look
a different size than expected or sit off-centre, tell me what you see and I will adjust one number.
