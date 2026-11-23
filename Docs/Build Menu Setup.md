# Build Menu Setup

How to make the build menu and add buildings to it. No experience needed.
(New to Unity's UI? Read the "Words used below" part of [Inventory Setup](Inventory%20Setup.md) first.)

What you will get:

- Press **B**: a window shows every building you can build, with its picture, name and **recipe**
  (e.g. "Stone 2/3" = you have 2, it needs 3; red = not enough).
- **Tabs** at the top: **All**, **Building** (floors, walls, roofs, furniture) and **Traps** (e.g. the Bear Traps).
  Each Building Data's **Category** decides its tab (Auto = Traps for a trap, else Building).
- **Rest the mouse** on a building for **1 second**: an **info box** shows its name, its Description and its numbers
  (e.g. the Iron Bear Trap: built on a Copper Bear Trap, damage 20, stun 3 s, one time use, hurts you too). It works on
  grey buttons too.
- **Upgrades** (a Building Data with **Upgrade Of** set, e.g. the Iron Bear Trap, Upgrade Of = Copper Bear Trap): its
  recipe starts with "on a Copper Bear Trap". Its blueprint jumps onto the placed building of that kind nearest the
  crosshair (within BlueprintPlacer's **Upgrade Reach**, 1.2 m) and turns green; building it replaces that one, and you
  pay only its own Recipe (the extra). With none near the crosshair it's red, and a click says "Place it on a Copper
  Bear Trap".
- A building you can't afford is greyed out.
- Click a building: the window closes and **build mode** starts: a see-through **blueprint** follows your crosshair.
  - **Left click** = build it here (takes the materials from your inventory). You **stay in build mode**: the next
    one of the same building follows your crosshair, turned the same way, so a row of walls or traps is quick.
    (BlueprintPlacer's **Keep Building**; untick it to stop after each one.)
  - **Right click** = back to the build menu, to pick something else
  - **B** or **Esc** = stop building (Esc doesn't open the pause menu then)
  - **Mouse wheel** = turn it. **Snap** mode: 90° per step. **Surface** mode: to the next fixed direction,
    lined up with the world (BlueprintPlacer's **Directions**: 4, 8, 16 or 32; 16 = every 22.5°)
  - **Middle click** = switch between **Snap** (the start mode) and **Surface**
- **Snap**: aim near a piece you already built and the blueprint **locks into place** next to it and turns
  **green** (see [Snap building](#snap-building)). Nothing to snap to: it sits on the surface you aim at.
- **Surface**: the blueprint sits on whatever surface you aim at.
- The blueprint is **red** (and can't be built) when it's inside something, in the air,
  or you don't have the materials. The reason is written in the Console when you click.

---

## Quick way: one click

1. Open the scene you want the menu in (e.g. `Assets/Scenes/GameScene`).
2. Menu **Tools > UI > Build Build Menu UI**. If the menu exists already it asks, then **rebuilds it in the same file**:
   every scene that has it follows, your building list is kept, and nothing new is put in the scene (use this once to
   get the tabs and the info box).
   (If it asks for TMP Essentials: **Window > TextMeshPro > Import TMP Essential Resources**, Import, then try again.)
3. Make at least one building config (next part). It appears in the menu by itself.
4. Press **Play**, pick up the materials, press **B**.

The tool makes `Assets/Prefabs/UI/Building/BuildMenu/BuildingButton.prefab` and `BuildMenuCanvas.prefab`,
puts BuildMenuCanvas in the scene, adds an EventSystem if needed, and adds **BlueprintPlacer** to the Player.
Ctrl + Z removes it from the scene again. Save the scene (**Ctrl + S**) to keep it.

---

## Make a building (its config file)

Every building is one **Building Data** file. It says what the building looks like and what it costs.

### Quick way: one click

1. Build it in the scene from shapes: right click in the Hierarchy > **Create Empty**, name it (e.g. **Furnace**),
   then right click it > **3D Object > Cube** (Sphere, Cylinder...) as many times as you need, and move / scale them.
2. Select its **top object** (Furnace), then menu **Tools > Building > Make Building From Selection**. This makes:
   - **Assets/Prefabs/Building/Furnace.prefab**: the prefab (your scene object becomes a copy of it, blue)
   - **Assets/Config/Building/Furnace.asset**: the Building Data with **Prefab** filled in (it gets selected for you)
3. In the Building Data's Inspector, set the **Recipe** (and Display Name, Description, Icon if you like). See the table below.
4. Delete the Furnace from the scene if you don't want a built one there, and save the scene (**Ctrl + S**).

It's in the build menu (B) straight away. It also works on a prefab you already have (select it in the Project window).
If a Building Data already uses that prefab or has that name (in any folder), it is reused, not made twice.

### By hand

### 1. The building prefab (what it looks like)

1. Build it in the scene from shapes: right click in the Hierarchy > **Create Empty**, name it (e.g. **Furnace**),
   then right click it > **3D Object > Cube** (Sphere, Cylinder...) as many times as you need, and move / scale them.
2. Make a folder for it: Project window > right click **Assets** > **Create > Folder**, e.g. **Building**.
3. Drag **Furnace** from the Hierarchy into that folder. It becomes a prefab (blue). Delete it from the scene.

### 2. An Item Data for each material (what it costs)

The recipe uses the same items the player picks up. Each material needs an **Item Data** config file in
**Assets/Config/Items** (e.g. Wood, Stone). See [Item Setup](Item%20Setup.md): select the item object, then
**Tools > Items > Make Item From Selection**.

Every object with ItemPickup pointing to the same Item Data counts as that material.

### 3. The Building Data (the config file)

1. Project window: open the **Building** folder, right click > **Create > Building > Building Data**.
   Name the file, e.g. **Furnace**.
2. Click the file and fill in the Inspector:

   | Field | What to put |
   |---|---|
   | Display Name | The name on the button, e.g. "Furnace" (empty = the file name). |
   | Description | What it does. |
   | **Prefab** | Drag in the building prefab from step 1. |
   | Icon | Choose **one**: **Capture** = a photo of the prefab. **Static** = a PNG (empty = name only). **Animated** = an **.aseprite** file (it plays on the button). Only the chosen one's field shows. |
   | **Recipe** | Click **+** once per material. **Item** = drag in the material's **Item Data** from Assets/Config/Items. **Amount** = how many. |
   | Piece Type | How it snaps (see [Snap building](#snap-building)). **Auto** = guessed from the file name ("...Floor..." = Floor, "...Wall..." = Wall, "...Roof..." = Roof, "...Trap..." = Trap, anything else = Other). Choose by hand if the name doesn't say. |

3. That's it: the build menu lists every Building Data by itself (when it's made, deleted or moved, and each time scripts compile). **Tools > Building > Update Build Menu List** does the same by hand if you ever need it.

The recipe doesn't have to match the shapes: a furnace made of 10 cubes can cost 3 Stone.

## Snap building

In **Snap** mode (middle click while placing), a new piece locks onto the pieces you already built, like in
Valheim or Rust. Nothing has to be set up on the prefabs: the snap spots come from each piece's size, and its
Building Data's **Piece Type** decides the rules:

| Piece Type | Snaps to |
|---|---|
| **Floor** | next to a floor (same height); over a floor that has walls, resting on top of the walls (a ceiling / second floor) |
| **Wall** | on a floor (along any edge, or across the middle); next to a floor; on top of a wall (taller wall); next to a wall: in line (longer wall), at its end (**L** corner) or on its side (**T**) |
| **Roof** | over a floor that has walls, resting on top of the walls; next to another roof |
| **Other** | furniture (e.g. a crafting table): on a floor or on other furniture (middle, an edge or a corner); against a wall; next to a floor or other furniture |
| **Trap** | (23/11/2026, e.g. the Bear Traps) on a floor; nothing snaps onto or next to a trap. Away from floors it lies on the ground, and the ground under it never counts as "in the way" (so it can go on a slope). Guide: `Docs/Trap Setup.md` |

- "Next to" = touching it, standing at the same height, lined up with one end, the middle or the other end.
- Aim at the spot you want (within BlueprintPlacer's **Snap Distance**, 1.5 m): the blueprint jumps there and
  turns **green**. The spot closest to where your crosshair touches wins.
- The **mouse wheel** turns the blueprint **90°** and it fits again in its new direction, e.g. a wall on a floor
  edge turns across the floor or onto the next edge; a wall at another wall's end turns into an **L** corner.
- Until you use the wheel, Snap picks the direction that fits best (a wall follows the floor edge by itself).
  After you use it, your direction is kept until the piece is built.
- A spot is skipped when the same piece is already built there, or when the new piece would go into another
  built piece. Walls may go into walls, so two walls can meet at a floor's corner.
- Only pieces built while playing snap (each built piece is marked with **BuiltPiece**). Pieces placed in the
  scene by hand don't.
- Snapped pieces don't need ground under them (they hang on the piece they lock onto).
- A roof or a second floor only snaps once a floor has at least one wall on it (something has to hold it up).
  Aim at the top of the walls, or at the sky above the floor.

---

## The finished Hierarchy

```
BuildMenuCanvas                Canvas (Sort Order 10) + BuildMenu script
└── BuildWindow            shown / hidden with B (1200 x 860)
    ├── Title              Text "Build"
    ├── Tabs               Horizontal Layout Group: one button per tab, named All, Building, Traps
    │   └── All ...        Image + Button + Canvas Group, child Label (Text)
    ├── List               Grid Layout Group: the building buttons are made in here when the game starts
    ├── Hint               Text: the placing keys
    └── Info               HIDDEN info box (Vertical Layout Group + Content Size Fitter): InfoTitle, InfoText, InfoStats

Player                     BlueprintPlacer script (+ PlayerInventory...)
```

One **BuildingButton** (prefab, made by BuildMenu per building):

```
BuildingButton             Image + Button + BuildingButton script (250 x 300)
├── Icon                   Image: the building's picture
├── Name                   Text
└── Recipe                 Vertical Layout Group
    └── Ingredient         HIDDEN template, copied once per material
        ├── Icon           Image: the material's picture
        └── Count          Text: "Stone  2/3"
```

The names **Icon**, **Name**, **Recipe**, **Ingredient** and **Count** matter: the script finds the parts by them.
So do **Tabs** (and each tab's name: All, or a Build Category's name) and **Info** with **InfoTitle**, **InfoText** and
**InfoStats**: BuildMenu finds them by name, and works without them.

The info box's numbers come from the building's prefab: any script on it that is an **IBuildingInfo** (like the Iron
Trap's BearTrap) gives lines such as "Damage 20". Its words come from the Building Data's **Description**.

## By hand (instead of the tool)

1. **Canvas**: right click > **UI > Canvas**, name it **BuildMenuCanvas**. Canvas: Sort Order **10**.
   Canvas Scaler: Scale With Screen Size, **1920 x 1080**, Match **0.5**.
2. **BuildWindow**: right click BuildMenuCanvas > **UI > Image**. Width **1200**, Height **860**, Pos Y **60**.
3. **Title**: right click BuildWindow > **UI > Text - TextMeshPro**, text "Build", at the top.
   **Tabs** (optional): an empty under the title (Top **86**, Height **46**) with a **Horizontal Layout Group**; in it
   one **UI > Button - TextMeshPro** per tab, named **All**, **Building** and **Traps** (+ a Canvas Group each).
4. **List**: right click BuildWindow > **Create Empty**. Alt + Shift + stretch stretch, then Left/Right **30**, Top **146**, Bottom **70**.
   Add Component > **Grid Layout Group**: Cell Size **250 x 300**, Spacing **20 x 20**,
   Child Alignment **Upper Center**, Constraint **Fixed Column Count**, Count **4**.
5. **BuildingButton prefab**: make the hierarchy above (Button: **UI > Button - TextMeshPro**, delete its own text child),
   untick **Ingredient** (hidden), add the **BuildingButton** script to the root,
   and drag it into **Assets/Prefabs/UI/Building/BuildMenu**. Delete it from the scene.
6. Select **BuildMenuCanvas** > Add Component > **BuildMenu**:
   Window = **BuildWindow**, List = **List**, Button Prefab = the **BuildingButton** prefab,
   Buildings = your Building Data files (+ for each).
7. Select the **Player** > Add Component > **BlueprintPlacer**.
8. **Info** (optional): an Image named **Info** under BuildWindow (width 400, Raycast Target off) with a **Vertical
   Layout Group** (Control Child Size ticked) and a **Content Size Fitter** (Vertical Fit: Preferred Size), and three
   TextMeshPro texts in it named **InfoTitle**, **InfoText** and **InfoStats** (Raycast Target off). Untick Info.

## Build mode hints (BuildModeCanvas)

A canvas that shows **only while placing** (e.g. pictures of the mouse buttons with "Build", "Cancel", "Turn").

**Quick way:** menu **Tools > UI > Build Build Mode UI**. It makes `Assets/Prefabs/UI/Building/BuildMode/BuildModeCanvas.prefab`
with the four mouse hints (Build, Cancel, Snap / Surface, Rotate, each an animated Aseprite mouse from `Assets/UI/key`)
and puts it in the scene. If the scene already has a BuildModeCanvas, it asks whether to replace it.
To change a hint's text, picture or position, edit the **Hints** table at the top of `BuildModeUIBuilder.cs`
and run the tool again. Save the scene (**Ctrl + S**).

```
BuildModeCanvas            Canvas + BuildModeHUD script (shown only while placing)
├── LeftClickImage         Image + UIAnimatedImage (MouseLeftClick.aseprite)
│   └── LeftClickText      "Build"
├── RightClickImage        (MouseRightClick.aseprite)
│   └── RightClickText     "Cancel"
├── MiddleClickImage       (MouseMiddleClick.aseprite)
│   └── MiddleClickText    "Snap" (BuildModeHUD shows the mode: "Snap" or "Surface")
└── MouseWheelImage        (MouseScrolling.aseprite)
    └── MouseWheelText     "Rotate"
```

**By hand:**

1. Make it like any canvas: **BuildModeCanvas** with children like **LeftClickImage** (UI > Image) + **LeftClickText**.
   Leave it switched on: it hides itself.
2. Menu **Tools > UI > Add Build Mode Scripts**. It adds:
   - **BuildModeHUD** to BuildModeCanvas: shows it while placing, hides it otherwise.
   - **UIAnimatedImage** to every Image under it (keeping the picture it already had).
3. On each image's **UIAnimatedImage**:

   | Field | What to put |
   |---|---|
   | **Animated** | Off = a still picture. On = an Aseprite animation. |
   | Static Image | The still picture (a PNG imported as **Sprite**). |
   | Aseprite | The **.aseprite** file. Its frames and timing are read by themselves. |
   | Aseprite Tag | Only if the file has several animations (tags): the one to play. Empty = the first. |
   | Fallback Fps | Speed if the file has no timing. Aseprite's own timing is used when it has one. |
   | Loop | Start again after the last frame. |

4. Save the scene (**Ctrl + S**).

A PNG must be imported as a Sprite: click it in the Project window, **Texture Type = Sprite (2D and UI)**, Apply.

## Test

1. Press **Play**. Press **B**: the window opens with your buildings. Without the materials they are greyed out.
2. Pick up the materials (F, or touch AutoPickup items). The amounts update and the button lights up.
3. Click a tab: only its buildings show. Rest the mouse on a building for 1 second: its info box shows.
4. Click the building, aim at the ground: the blueprint is blue. Left click: it's built, the materials are gone, and the
   next blueprint follows the crosshair. Right click: the build menu opens again. **B** or **Esc**: building stops.
5. Press **B** or **Esc** with the window open: it closes.

### If something doesn't work

| Problem | Fix |
|---|---|
| B does nothing | Is **BuildMenu** on **BuildMenuCanvas** with **Window** filled in? Is the inventory open (close it first)? |
| No tabs, or no info box | Run **Tools > UI > Build Build Menu UI** again (Rebuild): older menus were made before them. |
| A building is in the wrong tab | Set its Building Data's **Category** (Auto = Traps for Piece Type Trap, else Building). |
| Building stops after each one | BlueprintPlacer's **Keep Building** is off: tick it. |
| The window is empty / a building is missing | A Building Data without a **Prefab** is skipped. Still missing: **Tools > Building > Update Build Menu List**, and check the Console for red errors. |
| Clicking a building does nothing | Is there a **BlueprintPlacer** on the Player? (Console: "add it to the PlayerSphere".) |
| Button stays grey with the items | The recipe's **Item** must be the same **Item Data** that the picked-up objects' ItemPickup points to. |
| Blueprint always red | Read the Console after clicking: "Something is in the way", "Can't build in the air" or "Missing ...". |
| A material shows no picture | Its prefab has no visible mesh, so IconCapture had nothing to photograph. |
| More than 8 buildings don't fit | The window shows 4 x 2. Make the buttons smaller in BuildMenuUIBuilder, or ask for a scrolling list. |
