# Inventory Setup

How to build the inventory window in Unity, step by step. No experience needed.

What you will make:

- A window with **4 tabs**: Inventory, Gear, Crafting, Info. Clicking a tab shows its page.
- The **Inventory page** has 20 slots (4 rows of 5).
- A **hotbar** of 8 slots at the bottom of the screen that is **always visible**.
- Press **Tab** to open and close the window (Esc also closes it).
- The slots show the player's items. Click them to move items around, like Minecraft.
- Keys **1 - 8** or the **mouse wheel** choose the hotbar slot in hand.
- **Mouse over an item** (window open): its name shows next to the mouse. The name is the item's Item Data
  **Display Name** (`Assets/Config/Items`), or the Item Data's **file name** when Display Name is empty: `Meat.asset`
  shows "Meat", `StonePickaxe.asset` (Display Name "Stone Pickaxe") shows "Stone Pickaxe". It's **not** the prefab's
  name. To rename an item, type a Display Name (don't rename the file: the file name is the Item Id that stacks and
  recipes use).

The Gear page shows the player's character and the armor slots after **Tools > UI > Build Gear Page** (which
**Tools > Items > Build Plate Armor** runs too): `Docs/Armor Setup.md`. The Crafting page holds Turki's crafting list (with
pictures, a scroll list and the tier tools after **Tools > UI > Build Crafting Menu**: `Docs/Crafting Menu Setup.md`).
**Info** shows the player's status (health, hunger, stamina, walk and sprint speed, jump height, fireball, coins),
what perks added to it (in green, e.g. "+44 from perks"), and the perks the player has, with their icons.

---

## Quick way: one click

1. Open the scene you want the inventory in (e.g. `Assets/Scenes/GameScene`).
2. Menu **Tools > UI > Build Inventory UI**.
   (If it asks for TMP Essentials: **Window > TextMeshPro > Import TMP Essential Resources**, Import, then try again.)
3. Press **Play** and test it (see [Step 13](#step-13--test)).

This builds everything below for you: the prefabs `Assets/Prefabs/UI/Inventory/Slot.prefab` and
`Assets/Prefabs/UI/Inventory/InventoryCanvas.prefab` (found by name if you moved them), the InventoryCanvas in the
scene, and an EventSystem if needed, with every script added and filled in (and HotbarSelector + ItemDropper on the
player). The Crafting page gets Turki's **CraftingCanvas** (the crafting list). If the scene already has an
InventoryCanvas, it asks whether to replace it. Ctrl + Z removes it from the scene again.

**Built it before?** Use **Tools > UI > Add Inventory Scripts** instead. It adds only what's missing to the
prefabs you already have (SlotUI, SlotGroup, HeldItem, the ItemName texts, the crafting list, the **Info** tab, the
item name box), without rebuilding them.

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
│   │   ├── CraftingTab    Button
│   │   └── InfoTab        Button
│   ├── Pages              the big frame
│   │   ├── InventoryPage  Grid Layout Group + SlotGroup script
│   │   │   ├── Slot       (20 of them) SlotUI script
│   │   │   │   ├── Icon
│   │   │   │   ├── Count
│   │   │   │   └── ItemName   (hidden) the item's name, e.g. "SWORD"
│   │   │   └── ...
│   │   ├── GearPage
│   │   ├── CraftingPage
│   │   │   └── CraftingCanvas  Turki's crafting list (CraftingMenu), shows when the tab is open
│   │   └── InfoPage       PlayerInfoPage script: the player's status and perks
│   │       ├── Status     HealthRow, HungerRow, StaminaRow (each Icon, Name, Value, Bonus, Bar > Fill),
│   │       │              ArmorRow, SpeedRow, SprintRow, JumpRow, FireballRow, CoinsRow (Icon, Name, Value, Bonus)
│   │       └── PerkList   NoPerks text + PerkTemplate (switched off: Icon, Name, Stats), copied per perk
│   └── HeldItem           HeldItemUI script: the stack on the mouse (LAST child = drawn on top)
│       ├── Icon
│       ├── Count
│       └── ItemName       (hidden) the name in Label mode
├── Hotbar                 Horizontal Layout Group + SlotGroup script (NOT inside InventoryWindow)
│   ├── Slot               (8 of them)
│   └── ...
└── ItemTooltip            ItemTooltip script: the item's name next to the mouse (LAST child = drawn on top)
    └── Box                dark round box that fits the name
        └── Text
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

## Step 4 – The tab buttons

1. Right click **TabBar** > **UI > Button - TextMeshPro**. Rename it **InventoryTab**.
2. Click the small arrow next to InventoryTab to open it. Select its child **Text (TMP)**.
   In the Inspector, change the text "Button" to **Inventory**. Font Size about **32**.
3. Select **InventoryTab** and press **Ctrl + D** three times (duplicate).
4. Rename the copies **GearTab**, **CraftingTab** and **InfoTab**, and change their texts to **Gear**, **Crafting**
   and **Info**.

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
   - Right click **Slot** > **UI > Text - TextMeshPro** again. Rename it **ItemName** (the item's name, e.g. "SWORD").
     Anchor Presets: **Alt + Shift** + **stretch stretch**, Left / Top / Right / Bottom **4**. Empty text, **Bold**,
     Alignment **center** and **middle**, **Auto Size** on (Min 10, Max 18), untick Raycast Target.
     Then **untick the object** at the top of the Inspector (hidden until needed: tools without a picture, and
     every item when GameRules > Item Icon Style = Label).
4. Project window: make the folders **Assets/Prefabs/UI/Inventory** (right click > **Create > Folder**, skip the ones
   that exist).
5. Drag **Slot** from the Hierarchy into **Assets/Prefabs/UI/Inventory**.
   It turns **blue** in the Hierarchy: it is now a prefab.
6. Select **Slot** in the Hierarchy and press **Ctrl + D** 19 times → **20 slots** in a 5 x 4 grid.

To change all slots later: double click the Slot prefab in the Project window, edit, then click the arrow at the top left of the Hierarchy to go back.

## Step 8 – The Gear and Crafting pages

1. Right click **Pages** > **Create Empty**. Rename it **GearPage**.
   Anchor Presets: **Alt + Shift** + **stretch stretch**.
2. Right click **GearPage** > **UI > Text - TextMeshPro**. Text: **Gear (coming soon)** (a placeholder; later
   **Tools > UI > Build Gear Page** fills the page with the character and the armor slots: `Docs/Armor Setup.md`).
3. Make **CraftingPage** the same way (empty, stretch stretch), then drag Turki's **CraftingCanvas** prefab
   (`Assets/Prefabs/UI`) onto it. Nothing else to set: when the game runs it fills the page, fixes its size
   (the prefab is saved at scale 0) and shows itself whenever the Crafting tab is open.
   (The CraftingCanvas at the top of the scene is a separate copy that opens with **C**. Keep it if you want both.)
4. **InfoPage**: the easy way is **Tools > UI > Add Inventory Scripts**, which builds the whole page. By hand: an empty
   **InfoPage** (stretch stretch) with **Add Component > PlayerInfoPage**. PlayerInfoPage finds its parts by name:
   - rows named **HealthRow**, **HungerRow**, **StaminaRow**, **ArmorRow**, **SpeedRow**, **SprintRow**, **JumpRow**,
     **FireballRow**, **CoinsRow**, each with a TextMeshPro **Value** and **Bonus** (and for the first three a
     **Bar** with a **Fill** image inside it, stretched). A row's own **Image** (its background) turns gold while an
     armor set bonus improves its stat (Stamina and Armor with the whole iron set on);
   - **PerkList** with a TextMeshPro **NoPerks** and a switched-off **PerkTemplate** row (an Image **Icon**, and
     TextMeshPro **Name** and **Stats**). One copy of PerkTemplate is made for every perk.

## Step 9 – The hotbar

1. Right click **InventoryCanvas** (not InventoryWindow!) > **Create Empty**. Rename it **Hotbar**.
2. Anchor Presets: hold **Alt + Shift** and click **bottom center**.
   Then Pos Y **20**, Width **1100**, Height **130**.
3. Add Component > **Horizontal Layout Group**:
   - Spacing **15**, Child Alignment **Middle Center**.
   - Control Child Size: **untick** both. Child Force Expand: **untick** both.

   (So the slots keep their own 120 x 120 size.)
4. Drag the **Slot** prefab from **Assets/Prefabs/UI/Inventory** onto **Hotbar** in the Hierarchy.
   Select it and press **Ctrl + D** 7 times → **8 slots**.

## Step 10 – Make the tabs work

1. Select **TabBar**. Add Component > **TabGroup**.
2. **Tabs**: set the number to **4**. Open each Element and drag objects from the Hierarchy into it:

   | Element | Button | Page |
   |---|---|---|
   | 0 | InventoryTab | InventoryPage |
   | 1 | GearTab | GearPage |
   | 2 | CraftingTab | CraftingPage |
   | 3 | InfoTab | InfoPage |

3. Optional: Selected Color (the open tab) and Normal Color (the others).

## Step 11 – Open / close with Tab

1. Select **InventoryCanvas**. Add Component > **InventoryWindow**.
2. Drag **InventoryWindow** (the child object) from the Hierarchy into the **Window** field.

You don't need to hide the window yourself: the script closes it when the game starts.

## Step 12 – Make the slots show items

1. Project window: double click the **Slot** prefab (Assets/Prefabs/UI/Inventory) to open it.
   Add Component > **SlotUI**. Click the arrow at the top left of the Hierarchy to go back.
   (Every slot now has it, because they are all copies of the prefab.)
2. Select **InventoryPage**. Add Component > **SlotGroup**. Leave **Is Hotbar** unticked.
3. Select **Hotbar**. Add Component > **SlotGroup**. Tick **Is Hotbar**.
4. The item on the mouse: right click **InventoryWindow** > **Create Empty**, rename it **HeldItem**.
   - Drag it to the **bottom** of InventoryWindow's children (the last one is drawn on top).
   - Width **100**, Height **100**.
   - Give it three children exactly like the Slot's: an **Icon** (UI > Image, stretch stretch, Image unticked),
     a **Count** (UI > Text - TextMeshPro, bottom right, empty text) and an **ItemName** (unticked).
   - Select HeldItem, Add Component > **HeldItemUI**.
5. Select the **Player** (the object with PlayerInventory). Add Component > **HotbarSelector** and **ItemDropper**.
6. The name next to the mouse: right click **InventoryCanvas** > **Create Empty**, rename it **ItemTooltip**, stretch
   stretch, and keep it the **last** child of InventoryCanvas. In it a UI > Image **Box** (dark, Raycast Target
   **off**) with a **Horizontal Layout Group** (Padding 14 / 14 / 8 / 8, Control Child Size Width + Height) and a
   **Content Size Fitter** (both Preferred Size), and in the Box a TextMeshPro **Text** (white, Raycast Target off).
   Select ItemTooltip > Add Component > **ItemTooltip**.

SlotUI and HeldItemUI find their Icon, Count and ItemName by name, so keep those names.

## Step 13 – Test

1. Press **Play**.
2. Only the hotbar shows. Press **Tab**: the window opens on the Inventory page, and the mouse cursor appears.
3. Click **Gear**, **Crafting** and **Info**: their pages show, and the clicked tab changes color. **Info** shows
   your health, hunger and stamina bars moving, your speeds and coins. Take a perk from a chest: its stat gets a green
   "+ ... from perks" and the perk appears on the right with its icon.
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
   How it looks is the Player's **ItemDropper > Drop Style**: **Icon** (the item's picture floating on the floor with the amount, like Minecraft)
   or **Bundle** (a small box with the picture and amount on its sides). Same items lying close together merge into one stack.
8. Close the window while holding a stack: it goes back into the inventory.
9. Press **1 - 8** or turn the mouse wheel: the selected hotbar slot changes color.
10. Window open, move the mouse over an item (inventory or hotbar): its name shows next to the mouse. Over an empty
    slot, or while holding a stack on the mouse, nothing shows.

### If something doesn't work

| Problem | Fix |
|---|---|
| Tab does nothing | Is InventoryWindow (script) on **InventoryCanvas**, with **Window** filled in? Any red errors in the Console? |
| No Info tab | Run **Tools > UI > Add Inventory Scripts** (it adds the tab and its page to the inventory you have). |
| No Armor row on the Info tab | Run **Tools > UI > Add Inventory Scripts** or **Tools > Items > Build Plate Armor** (both add it under Stamina). |
| No name when the mouse is on an item | Run **Tools > UI > Add Inventory Scripts** (it adds the ItemTooltip). The Slot prefab needs **SlotUI**. |
| The name is wrong / ugly (e.g. "IronOre") | Select the item's Item Data in `Assets/Config/Items` and type its **Display Name**. |
| The Info page's perks have no icons | Run **Tools > UI > Build HUD** or **Add Inventory Scripts**: they put the perk pictures in the Player's **PlayerPerks > Icons**. |
| Clicking tabs does nothing | Is there an **EventSystem** in the Hierarchy? If not: right click > **UI > Event System**. |
| Text is missing | Import **TMP Essentials** (see "Before you start"). |
| The hotbar disappears with the window | Hotbar must be a child of **InventoryCanvas**, not of InventoryWindow. |
| Slots are in one long row / column | InventoryPage's Grid Layout Group: Constraint **Fixed Column Count**, Count **5**. |
| The tab key is not Tab | The key is set in **KeyBindings** (Inventory). |
| Slots stay empty | Slot prefab has **SlotUI**? InventoryPage / Hotbar have **SlotGroup**? The Console says if the slot count doesn't match PlayerInventory. |
| The held stack hides behind slots or the hotbar | HeldItemUI draws on its own Canvas on top of everything. If another canvas still covers it, raise HeldItemUI's **Sorting Order** above that canvas's. |
| An item shows no picture | It had no icon when picked up (IconCapture couldn't photograph it), so only its amount shows (nothing for just 1). |
