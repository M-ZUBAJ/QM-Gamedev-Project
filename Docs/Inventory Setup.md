# Inventory Setup

How to build the inventory window in Unity, step by step. No experience needed.

What you will make:

- A window with **3 tabs**: Inventory, Gear, Crafting. Clicking a tab shows its page.
- The **Inventory page** has 20 slots (4 rows of 5).
- A **hotbar** of 8 slots at the bottom of the screen that is **always visible**.
- Press **Tab** to open and close the window (Esc also closes it).
- The slots show the player's items. Click them to move items around, like Minecraft.
- Keys **1 - 8** or the **mouse wheel** choose the hotbar slot in hand.

Gear and Crafting pages are placeholders until their designs are ready.

---

## Quick way: one click

1. Open the scene you want the inventory in (e.g. `Assets/Scenes/GameScene`).
2. Menu **Tools > UI > Build Inventory UI**.
   (If it asks for TMP Essentials: **Window > TextMeshPro > Import TMP Essential Resources**, Import, then try again.)
3. Press **Play** and test it (see [Step 13](#step-13--test)).

This builds everything below for you: the prefabs `Assets/Prefab/UI/Slot.prefab` and
`Assets/Prefab/UI/InventoryCanvas.prefab`, the InventoryCanvas in the scene, and an EventSystem if needed,
with every script added and filled in (and HotbarSelector on the player). Ctrl + Z removes it from the scene again.

**Built it before the slots could show items?** Use **Tools > UI > Add Inventory Scripts** instead.
It adds the newer scripts to the prefabs you already have, without rebuilding them.

The steps below do the same thing by hand. Read them to understand how it is put together,
or to change it later.

---

## The finished Hierarchy

This is what the Hierarchy window will look like at the end. Indented = child of the line above.

```
InventoryCanvas            Canvas + InventoryWindow script
├── InventoryWindow        shown / hidden with Tab
│   ├── TabBar             Horizontal Layout Group + TabGroup script
│   │   ├── InventoryTab   Button
│   │   ├── GearTab        Button
│   │   └── CraftingTab    Button
│   ├── Pages              the big frame
│   │   ├── InventoryPage  Grid Layout Group + SlotGroup script
│   │   │   ├── Slot       (20 of them) SlotUI script
│   │   │   │   ├── Icon
│   │   │   │   └── Count
│   │   │   └── ...
│   │   ├── GearPage
│   │   └── CraftingPage
│   └── HeldItem           HeldItemUI script: the stack on the mouse (LAST child = drawn on top)
│       ├── Icon
│       └── Count
└── Hotbar                 Horizontal Layout Group + SlotGroup script (NOT inside InventoryWindow)
    ├── Slot               (8 of them)
    └── ...
EventSystem                made by Unity, lets you click UI

Player                     PlayerInventory + HotbarSelector scripts
```

The hotbar is **outside** InventoryWindow on purpose: hiding the window must not hide the hotbar.

20 inventory slots and 8 hotbar slots match the default sizes in `PlayerInventory` (Max Slots 20, Hotbar Size 8).

---

## Words used below

| Word | Where / what |
|---|---|
| **Hierarchy** | The list of objects in the scene (usually top left). |
| **Inspector** | The settings of the selected object (usually right). |
| **Project** | Your files (usually bottom). |
| **Rect Transform** | The first component of every UI object: its position and size. |
| **Anchor Presets** | The square button at the top left of the Rect Transform. Click it to pick where the object sticks to. |
| **Alt + Shift** | Hold both while clicking an anchor preset: the object also moves and resizes to that spot. |

To **rename** an object: select it in the Hierarchy and press **F2** (or double click it).

---

## Before you start (only once per project)

The first time you make any **TextMeshPro** text, Unity shows a window **"TMP Importer"**.
Click **Import TMP Essentials**, then close the window. Without this, text doesn't show.

---

## Step 1 – The canvas

A Canvas is the "screen" all UI is drawn on.

1. Hierarchy: right click on an empty area > **UI > Canvas**.
   Unity also makes an **EventSystem**. Keep it, clicking UI needs it.
2. Rename the Canvas to **InventoryCanvas**.
3. Inspector > **Canvas Scaler** component:
   - UI Scale Mode = **Scale With Screen Size**
   - Reference Resolution = **1920** x **1080**
   - Match = **0.5**

   This keeps the UI the same size on every screen.

## Step 2 – The window

1. Right click **InventoryCanvas** > **Create Empty**. Rename it **InventoryWindow**.
2. Rect Transform: Anchor Presets > **middle center** (the default).
   Pos X **0**, Pos Y **60**, Width **1200**, Height **800**.

Everything that should hide with Tab goes inside InventoryWindow.

## Step 3 – The tab bar

1. Right click **InventoryWindow** > **UI > Image**. Rename it **TabBar**.
2. Anchor Presets: hold **Alt + Shift** and click **top stretch** (top row, last column).
   Then set Height **80**.
3. **Add Component** (bottom of the Inspector) > type **Horizontal Layout Group** > click it.
   - Control Child Size: tick **Width** and **Height**.
   - Child Force Expand: tick **Width** and **Height**.

   This makes the tab buttons share the bar equally, side by side.

## Step 4 – The 3 tab buttons

1. Right click **TabBar** > **UI > Button - TextMeshPro**. Rename it **InventoryTab**.
2. Click the small arrow next to InventoryTab to open it. Select its child **Text (TMP)**.
   In the Inspector, change the text "Button" to **Inventory**. Font Size about **32**.
3. Select **InventoryTab** and press **Ctrl + D** two times (duplicate).
4. Rename the copies **GearTab** and **CraftingTab**, and change their texts to **Gear** and **Crafting**.

They line up by themselves because of the Horizontal Layout Group.

## Step 5 – The pages frame

1. Right click **InventoryWindow** > **UI > Image**. Rename it **Pages**.
2. Anchor Presets: hold **Alt + Shift** and click **stretch stretch** (bottom right square).
3. Set **Top** to **80** (the height of the tab bar), so the frame starts under the tabs.
4. Color: pick the frame color you like.

## Step 6 – The Inventory page

1. Right click **Pages** > **Create Empty**. Rename it **InventoryPage**.
2. Anchor Presets: **Alt + Shift** + **stretch stretch**.
   Then set Left, Top, Right, Bottom all to **40** (the gap between the frame and the slots).
3. Add Component > **Grid Layout Group**:
   - Cell Size = **120** x **120** (the size of one slot)
   - Spacing = **20** x **20** (the gap between slots)
   - Child Alignment = **Middle Center**
   - Constraint = **Fixed Column Count**, Constraint Count = **5**

   The grid puts every child in rows of 5 by itself.

## Step 7 – One slot (and make it a prefab)

A prefab is a saved object you can reuse. Change the prefab once, and every slot changes.

1. Right click **InventoryPage** > **UI > Image**. Rename it **Slot**.
   This is the box. Image > Source Image: click the small circle and pick **UISprite** (or your own box picture later).
2. Right click **Slot** > **UI > Image**. Rename it **Icon** (the item's picture goes here later).
   - Anchor Presets: **Alt + Shift** + **stretch stretch**, then Left/Top/Right/Bottom **15**.
   - **Untick** the checkbox next to **Image** in the Inspector (hidden until there is an item).
   - Untick **Raycast Target** (so clicks go to the slot, not the icon).
3. Right click **Slot** > **UI > Text - TextMeshPro**. Rename it **Count** (e.g. "64").
   - Anchor Presets: **Alt + Shift** + **bottom right**. Width **80**, Height **40**.
   - Delete the text "New Text" (leave it empty). Font Size **28**. Alignment: **right** and **bottom**.
   - Extra Settings: untick **Raycast Target**.
4. Project window: right click **Assets** > **Create > Folder**, name it **Prefab** (skip if it exists).
   Inside it make a folder **UI**.
5. Drag **Slot** from the Hierarchy into **Assets/Prefab/UI**.
   It turns **blue** in the Hierarchy: it is now a prefab.
6. Select **Slot** in the Hierarchy and press **Ctrl + D** 19 times → **20 slots** in a 5 x 4 grid.

To change all slots later: double click the Slot prefab in the Project window, edit, then click the arrow at the top left of the Hierarchy to go back.

## Step 8 – The Gear and Crafting pages (placeholders)

1. Right click **Pages** > **Create Empty**. Rename it **GearPage**.
   Anchor Presets: **Alt + Shift** + **stretch stretch**.
2. Right click **GearPage** > **UI > Text - TextMeshPro**. Text: **Gear (coming soon)**.
3. Do the same for **CraftingPage** with the text **Crafting (coming soon)**.

## Step 9 – The hotbar

1. Right click **InventoryCanvas** (not InventoryWindow!) > **Create Empty**. Rename it **Hotbar**.
2. Anchor Presets: hold **Alt + Shift** and click **bottom center**.
   Then Pos Y **20**, Width **1100**, Height **130**.
3. Add Component > **Horizontal Layout Group**:
   - Spacing **15**, Child Alignment **Middle Center**.
   - Control Child Size: **untick** both. Child Force Expand: **untick** both.

   (So the slots keep their own 120 x 120 size.)
4. Drag the **Slot** prefab from **Assets/Prefab/UI** onto **Hotbar** in the Hierarchy.
   Select it and press **Ctrl + D** 7 times → **8 slots**.

## Step 10 – Make the tabs work

1. Select **TabBar**. Add Component > **TabGroup**.
2. **Tabs**: set the number to **3**. Open each Element and drag objects from the Hierarchy into it:

   | Element | Button | Page |
   |---|---|---|
   | 0 | InventoryTab | InventoryPage |
   | 1 | GearTab | GearPage |
   | 2 | CraftingTab | CraftingPage |

3. Optional: Selected Color (the open tab) and Normal Color (the others).

## Step 11 – Open / close with Tab

1. Select **InventoryCanvas**. Add Component > **InventoryWindow**.
2. Drag **InventoryWindow** (the child object) from the Hierarchy into the **Window** field.

You don't need to hide the window yourself: the script closes it when the game starts.

## Step 12 – Make the slots show items

1. Project window: double click the **Slot** prefab (Assets/Prefab/UI) to open it.
   Add Component > **SlotUI**. Click the arrow at the top left of the Hierarchy to go back.
   (Every slot now has it, because they are all copies of the prefab.)
2. Select **InventoryPage**. Add Component > **SlotGroup**. Leave **Is Hotbar** unticked.
3. Select **Hotbar**. Add Component > **SlotGroup**. Tick **Is Hotbar**.
4. The item on the mouse: right click **InventoryWindow** > **Create Empty**, rename it **HeldItem**.
   - Drag it to the **bottom** of InventoryWindow's children (the last one is drawn on top).
   - Width **100**, Height **100**.
   - Give it two children exactly like the Slot's: an **Icon** (UI > Image, stretch stretch, Image unticked)
     and a **Count** (UI > Text - TextMeshPro, bottom right, empty text).
   - Select HeldItem, Add Component > **HeldItemUI**.
5. Select the **Player** (the object with PlayerInventory). Add Component > **HotbarSelector** and **ItemDropper**.

SlotUI and HeldItemUI find their Icon and Count by name, so keep those names.

## Step 13 – Test

1. Press **Play**.
2. Only the hotbar shows. Press **Tab**: the window opens on the Inventory page, and the mouse cursor appears.
3. Click **Gear** and **Crafting**: their pages show, and the clicked tab changes color.
4. Press **Tab** (or **Esc**): the window closes and the mouse turns the camera again.
5. Pick up some items (F, or touch AutoPickup items), then press **Tab**: they are in the slots.
6. Move them like Minecraft:

   | Click | Holding nothing | Holding a stack |
   |---|---|---|
   | Left | pick up the whole stack | put it all down (or swap) |
   | Right | pick up half | put down one |
   | Double left | gather every stack of that item here | |
   | Hold right + drag | | one item into each slot you pass over |

7. Holding a stack, click **outside** the window (not on any UI): it's thrown in front of you.
   Left click = the whole stack (as one object), right click = one. Grab it again with **F**.
8. Close the window while holding a stack: it goes back into the inventory.
9. Press **1 - 8** or turn the mouse wheel: the selected hotbar slot changes color.

### If something doesn't work

| Problem | Fix |
|---|---|
| Tab does nothing | Is InventoryWindow (script) on **InventoryCanvas**, with **Window** filled in? Any red errors in the Console? |
| Clicking tabs does nothing | Is there an **EventSystem** in the Hierarchy? If not: right click > **UI > Event System**. |
| Text is missing | Import **TMP Essentials** (see "Before you start"). |
| The hotbar disappears with the window | Hotbar must be a child of **InventoryCanvas**, not of InventoryWindow. |
| Slots are in one long row / column | InventoryPage's Grid Layout Group: Constraint **Fixed Column Count**, Count **5**. |
| The tab key is not Tab | The key is set in **KeyBindings** (Inventory). |
| Slots stay empty | Slot prefab has **SlotUI**? InventoryPage / Hotbar have **SlotGroup**? The Console says if the slot count doesn't match PlayerInventory. |
| The held stack hides behind slots or the hotbar | HeldItemUI draws on its own Canvas on top of everything. If another canvas still covers it, raise HeldItemUI's **Sorting Order** above that canvas's. |
| An item shows no picture | It had no icon when picked up (IconCapture couldn't photograph it), so only its amount shows (nothing for just 1). |
