# Build Menu Setup

How to make the build menu and add buildings to it. No experience needed.
(New to Unity's UI? Read the "Words used below" part of [Inventory Setup](Inventory%20Setup.md) first.)

What you will get:

- Press **B**: a window shows every building you can build, with its picture, name and **recipe**
  (e.g. "Stone 2/3" = you have 2, it needs 3; red = not enough).
- A building you can't afford is greyed out.
- Click a building: the window closes and a see-through **blueprint** follows your crosshair.
  - **Left click** = build it here (takes the materials from your inventory)
  - **Right click** or **B** = cancel
  - **Mouse wheel** = turn it to the next fixed direction, lined up with the world
    (BlueprintPlacer's **Directions**: 4, 8, 16 or 32; 16 = every 22.5°)
  - **Middle click** = sit on the surface you aim at / float in front of you
- The blueprint is **red** (and can't be built) when it's inside something, in the air,
  or you don't have the materials. The reason is written in the Console when you click.

---

## Quick way: one click

1. Open the scene you want the menu in (e.g. `Assets/Scenes/GameScene`).
2. Menu **Tools > UI > Build Build Menu UI**.
   (If it asks for TMP Essentials: **Window > TextMeshPro > Import TMP Essential Resources**, Import, then try again.)
3. Make at least one building config (next part). It appears in the menu by itself.
4. Press **Play**, pick up the materials, press **B**.

The tool makes `Assets/Prefabs/UI/Building/BuildMenu/BuildingButton.prefab` and `BuildMenuCanvas.prefab`,
puts BuildMenuCanvas in the scene, adds an EventSystem if needed, and adds **BlueprintPlacer** to the Player.
Ctrl + Z removes it from the scene again. Save the scene (**Ctrl + S**) to keep it.

---

## Make a building (its config file)

Every building is one **Building Data** file. It says what the building looks like and what it costs.

### 1. The building prefab (what it looks like)

1. Build it in the scene from shapes: right click in the Hierarchy > **Create Empty**, name it (e.g. **Furnace**),
   then right click it > **3D Object > Cube** (Sphere, Cylinder...) as many times as you need, and move / scale them.
2. Make a folder for it: Project window > right click **Assets** > **Create > Folder**, e.g. **Building**.
3. Drag **Furnace** from the Hierarchy into that folder. It becomes a prefab (blue). Delete it from the scene.

### 2. An Item Data for each material (what it costs)

The recipe uses the same items the player picks up. Each material needs an **Item Data** config file in
**Assets/Items** (e.g. Wood, Stone). See [Item Setup](Item%20Setup.md): select the item object, then
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
   | **Recipe** | Click **+** once per material. **Item** = drag in the material's **Item Data** from Assets/Items. **Amount** = how many. |

3. That's it: the build menu lists every Building Data by itself (when it's made, deleted or moved, and each time scripts compile). **Tools > Building > Update Build Menu List** does the same by hand if you ever need it.

The recipe doesn't have to match the shapes: a furnace made of 10 cubes can cost 3 Stone.

---

## The finished Hierarchy

```
BuildMenuCanvas                Canvas (Sort Order 10) + BuildMenu script
└── BuildWindow            shown / hidden with B
    ├── Title              Text "Build"
    ├── List               Grid Layout Group: the building buttons are made in here when the game starts
    └── Hint               Text: the placing keys

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

## By hand (instead of the tool)

1. **Canvas**: right click > **UI > Canvas**, name it **BuildMenuCanvas**. Canvas: Sort Order **10**.
   Canvas Scaler: Scale With Screen Size, **1920 x 1080**, Match **0.5**.
2. **BuildWindow**: right click BuildMenuCanvas > **UI > Image**. Width **1200**, Height **800**, Pos Y **60**.
3. **Title**: right click BuildWindow > **UI > Text - TextMeshPro**, text "Build", at the top.
4. **List**: right click BuildWindow > **Create Empty**. Alt + Shift + stretch stretch, then Left/Right **30**, Top **90**, Bottom **70**.
   Add Component > **Grid Layout Group**: Cell Size **250 x 300**, Spacing **20 x 20**,
   Child Alignment **Upper Center**, Constraint **Fixed Column Count**, Count **4**.
5. **BuildingButton prefab**: make the hierarchy above (Button: **UI > Button - TextMeshPro**, delete its own text child),
   untick **Ingredient** (hidden), add the **BuildingButton** script to the root,
   and drag it into **Assets/Prefabs/UI/Building/BuildMenu**. Delete it from the scene.
6. Select **BuildMenuCanvas** > Add Component > **BuildMenu**:
   Window = **BuildWindow**, List = **List**, Button Prefab = the **BuildingButton** prefab,
   Buildings = your Building Data files (+ for each).
7. Select the **Player** > Add Component > **BlueprintPlacer**.

## Build mode hints (BuildModeCanvas)

A canvas that shows **only while placing** (e.g. pictures of the mouse buttons with "Build", "Cancel", "Turn").

**Quick way:** menu **Tools > UI > Build Build Mode UI**. It makes `Assets/Prefabs/UI/Building/BuildMode/BuildModeCanvas.prefab`
with the four mouse hints (Build, Cancel, Auto / Free, Rotate, each an animated Aseprite mouse from `Assets/UI/key`)
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
│   └── MiddleClickText    "Auto / Free"
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
3. Click the building, aim at the ground: the blueprint is blue. Left click: it's built, and the materials are gone.
4. Press **B** or **Esc** with the window open: it closes.

### If something doesn't work

| Problem | Fix |
|---|---|
| B does nothing | Is **BuildMenu** on **BuildMenuCanvas** with **Window** filled in? Is the inventory open (close it first)? |
| The window is empty / a building is missing | A Building Data without a **Prefab** is skipped. Still missing: **Tools > Building > Update Build Menu List**, and check the Console for red errors. |
| Clicking a building does nothing | Is there a **BlueprintPlacer** on the Player? (Console: "add it to the PlayerSphere".) |
| Button stays grey with the items | The recipe's **Item** must be the same **Item Data** that the picked-up objects' ItemPickup points to. |
| Blueprint always red | Read the Console after clicking: "Something is in the way", "Can't build in the air" or "Missing ...". |
| A material shows no picture | Its prefab has no visible mesh, so IconCapture had nothing to photograph. |
| More than 8 buildings don't fit | The window shows 4 x 2. Make the buttons smaller in BuildMenuUIBuilder, or ask for a scrolling list. |
