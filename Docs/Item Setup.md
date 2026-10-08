# Item Setup

How to add an item (a material like Wood or Stone) to the game. No experience needed.

Every item has **one config file**, an **Item Data**, kept in **Assets/Items**. The file decides:

- **what the item is**: its file name ("Wood") is the item. Same file = same item = stacks in the inventory.
- **what it looks like**: its **Prefab**, used when it's dropped, for its icon and in recipes.
- **how it's picked up**: **Pickup** = aim and press **F**. **AutoPickup** = collected by touching it (F works too).

Objects in the world only need the **ItemPickup** script, with **Data** set to that file. **No tags.**

---

## Quick way: one click

1. Make the item: right click in the Hierarchy > **3D Object > Cylinder** (or any shape, or your own model).
   Rename it to the item's name, e.g. **Wood**. Size it and color it (e.g. Add Component > **ShapeColor**).
2. Select it, then menu **Tools > Items > Make Item From Selection**. This makes:
   - **Assets/Items/Wood.asset**: the Item Data (it gets selected for you)
   - **Assets/Prefabs/Items/Wood.prefab**: the prefab, if the object wasn't one yet
   - **ItemPickup** on the prefab and on your scene object, with **Data** = the Item Data
3. In the Inspector of the Item Data, choose **Pickup** (Pickup or AutoPickup).
4. Save the scene (**Ctrl + S**).

Put more of them in the world: drag **Wood.prefab** from the Project window into the scene.

It also works on a prefab you already have: select the prefab in the Project window, then the same menu.
Recipes that pointed to that prefab are switched to the new Item Data.

## The Item Data fields

| Field | What to put |
|---|---|
| Display Name | The name shown to the player. Empty = the file name. |
| **Prefab** | What it looks like in the world. Its root must have ItemPickup with **Data** = this file (the tool does it). |
| **Pickup** | **Pickup**: aim + F. **AutoPickup**: collected on touch. More ways can be added to this list later. |
| Icon | Choose **one**: **Capture** = a photo of the prefab, taken by itself. **Static** = drag in a PNG (Texture Type = Sprite). **Animated** = drag in an **.aseprite** file (it plays in the inventory; Aseprite Tag only if the file has several animations). Only the chosen one's field shows. |
| Recipe | What crafting this item costs (for the crafting system, coming later). **+** per material: drag in its Item Data, set Amount. Empty = can't be crafted (a raw material like Wood). |
| Craft Amount | How many of this item one craft makes (e.g. 1 Wood -> 4 Planks). |
| Crafted At | Where it's made: **Crafting** (the inventory's Crafting page) or **Furnace**. More places can be added to this list later. |

## By hand (instead of the tool)

1. Project window: make the folder **Assets/Items** (right click Assets > Create > Folder).
2. In it: right click > **Create > Items > Item Data**. Name it after the item, e.g. **Wood**.
3. Make the prefab: drag your item object from the Hierarchy into **Assets/Prefabs/Items**.
4. Open the prefab (double click). On its root: Add Component > **ItemPickup**, drag **Wood** (the Item Data) into **Data**.
5. Click the Item Data: drag the prefab into **Prefab**, choose **Pickup**.

## ItemPickup (on the object)

| Field | What to put |
|---|---|
| **Data** | The item's Item Data file. |
| Amount | How many the player gets from this one object (e.g. a log pile = 5 Wood). |

---

## Moving from tags (one time)

Items used to need the tag **PickUp** or **AutoPickUp**. That's gone.
Menu **Tools > Items > Convert Tagged Objects To Items** turns every tagged object and prefab into an item
(AutoPickUp tag = AutoPickup), untags them, and deletes the two tags. Save the scene afterwards.

## Test

1. Press **Play**. Aim at the item: the crosshair turns **yellow** (Pickup) or **green** (AutoPickup, when close).
2. Press **F** (or walk into an AutoPickup item). Press **Tab**: it's in the inventory.
3. Drop it (hold it on the cursor, click outside the window): its prefab is thrown in front of you.

### If something doesn't work

| Problem | Fix |
|---|---|
| F does nothing | The object (or a parent) needs **ItemPickup** and a **Collider**. Too far away? |
| Walking into it does nothing | Its Item Data's **Pickup** must be **AutoPickup**. The object needs a Collider. |
| Two kinds of wood don't stack | Their ItemPickups point to **different** Item Data files. Use one file. |
| A recipe line is empty after the update | Drag the material's **Item Data** into the recipe's **Item** again. |
| Dropped item looks like a box | It has no Item Data (old item): make one with the tool, then its real prefab drops. |
