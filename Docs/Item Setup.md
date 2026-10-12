# Item Setup

How to add an item (a material like Wood or Stone) to the game. No experience needed.

Every item has **one config file**, an **Item Data**, kept in **Assets/Config/Items**. The file decides:

- **what the item is**: its file name ("Wood") is the item. Same file = same item = stacks in the inventory.
- **what it looks like**: its **Prefab**, used when it's dropped, for its icon and in recipes.
- **how it's picked up**: **Pickup** = aim and press **F**. **AutoPickup** = collected by touching it (F works too).

Objects in the world only need the **ItemPickup** script, with **Data** set to that file. **No tags.**

---

## Quick way: one click

1. Make the item: right click in the Hierarchy > **3D Object > Cylinder** (or any shape, or your own model).
   Rename it to the item's name, e.g. **Wood**. Size it and color it (e.g. Add Component > **ShapeColor**).
2. Select it, then menu **Tools > Items > Make Item From Selection**. This makes:
   - **Assets/Config/Items/Wood.asset**: the Item Data (it gets selected for you)
   - **Assets/Prefabs/Item/Wood.prefab**: the prefab, if the object wasn't one yet
   - **ItemPickup** on the prefab and on your scene object, with **Data** = the Item Data
3. In the Inspector of the Item Data, choose **Pickup** (Pickup or AutoPickup) and **Item Type**
   (Material, or Consumable for food: then set its stats).
4. Save the scene (**Ctrl + S**).

If an Item Data with that name already exists (in any folder), the tool reuses it instead of making a new one.

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
| **Item Type** | **Material** = used to make things (Wood, Stone), can't be eaten. **Consumable** = food / drink, right click to eat. Both can be used in crafting and building recipes. |
| Consumable | Only shows when Item Type = **Consumable**. **Health**, **Hunger**, **Stamina**: how much eating it changes each stat. Positive = gives (Health 20 = heals 20), negative = takes away, 0 = no change. Set as many as you like, e.g. a meal: Health 20, Hunger 20. See [Food](#food-consumable-items). |
| Recipe | What crafting this item costs (for the crafting system, coming later). **+** per material: drag in its Item Data, set Amount. Empty = can't be crafted (a raw material like Wood). |
| Craft Amount | How many of this item one craft makes (e.g. 1 Wood -> 4 Planks). |
| Crafted At | Where it's made: **Crafting** (the inventory's Crafting page) or **Furnace**. More places can be added to this list later. |

## By hand (instead of the tool)

The Item Data and the prefab must point at **each other**: Item Data > Prefab = the prefab, and the
prefab's ItemPickup > Data = the Item Data. Missing either one is the usual reason an item "doesn't work".

1. **Shape:** right click in the Hierarchy > 3D Object > e.g. Sphere. Rename it to the item's name, e.g. **Apple**.
2. **Prefab:** drag it from the Hierarchy into **Assets/Prefabs/Item**. It turns blue in the Hierarchy (= a prefab copy).
3. **Item Data:** in **Assets/Config/Items**, right click > **Create > Items > Item Data**. Name it exactly like the item: **Apple**.
4. **Fill in the Item Data:** click it, then
   - **Prefab** = drag in **Apple.prefab** from the Project window (not the object from the Hierarchy),
   - **Pickup** = Pickup,
   - **Item Type** = Consumable (food) or Material, and for food set its stats (e.g. Hunger 20).
5. **ItemPickup on the prefab:** double click **Apple.prefab** in the Project window to open it.
   Select its **top (root) object** > Add Component > **ItemPickup** > drag the **Apple** Item Data into **Data**.
   Leave the prefab (the **<** arrow at the top of the Hierarchy); it saves by itself (Auto Save) or click **Save**.
6. Save the scene (**Ctrl + S**), press Play, pick it up with **F**.

Easy mistakes:

| Mistake | What happens | Fix |
|---|---|---|
| ItemPickup added to the copy in the **scene**, not the prefab | Only that one copy works; it shows **bold** in the Inspector (an override) | Add it in the prefab (step 5), or Overrides > **Apply All** |
| ItemPickup's **Data** left empty | It can be picked up (named after the object) but isn't linked: food can't be eaten, the Console warns | Drag the Item Data into **Data** |
| Item Type left on **Material** | A food item can't be eaten | Item Type = **Consumable** |
| Two Item Data files with the same name | The prefab may point to the wrong (empty) one | Keep one, delete the other, run the tool on the prefab again |
| Changes made while **playing** | Lost when you press Stop | Stop first, then change things |

## ItemPickup (on the object)

| Field | What to put |
|---|---|
| **Data** | The item's Item Data file. |
| Amount | How many the player gets from this one object (e.g. a log pile = 5 Wood). |

## Food (consumable items)

1. Make the item as above (e.g. **Apple**).
2. Click its Item Data > **Item Type** = **Consumable**. The **Consumable** section appears: type the stat changes,
   e.g. Health **5**, Hunger **20**.
3. Click the **Player**: Add Component > **ItemConsumer** (once; it also needs PlayerInventory, and
   PlayerHealth / PlayerHunger / PlayerStamina for the stats you use; a missing one is just skipped).
4. Play: pick up the apple, put it in a hotbar slot, select it (**1 - 8**), **right click**. One apple is used up and the bars change.

Eating does nothing with an empty hand, a non-food item, a menu open, while placing a blueprint, or when dead.
Stats never go above their max (100) or below 0. The key is **Consume** in KeyBindings.
Food can still be a recipe ingredient (e.g. Apple in an Apple Pie, or in a building's recipe): just drag its Item Data into the recipe.

---

## Moving from tags (one time)

Items used to need the tag **PickUp** or **AutoPickUp**. That's gone.
Menu **Tools > Items > Convert Tagged Objects To Items** turns every tagged object and prefab into an item
(AutoPickUp tag = AutoPickup), untags them, and deletes the two tags. Save the scene afterwards.

## Test

1. Press **Play**. Aim at the item: the crosshair turns **yellow** (Pickup) or **green** (AutoPickup, when close).
2. Press **F** (or walk into an AutoPickup item). Press **Tab**: it's in the inventory.
3. Drop it (hold it on the cursor, click outside the window): it's thrown in front of you as a dropped stack.
   Pick it up again with **F**. Or tick **Auto Pickup Drops** on the **GameRules** object: then walking near a
   dropped stack (1.5 m, AutoCollector's Drop Pickup Radius) collects it by itself.

### If something doesn't work

| Problem | Fix |
|---|---|
| F does nothing | The object (or a parent) needs **ItemPickup** and a **Collider**. Too far away? |
| Walking into it does nothing | Its Item Data's **Pickup** must be **AutoPickup**. The object needs a Collider. |
| Two kinds of wood don't stack | Their ItemPickups point to **different** Item Data files. Use one file. |
| A recipe line is empty after the update | Drag the material's **Item Data** into the recipe's **Item** again. |
| Right click doesn't eat | The Player needs **ItemConsumer**, the item must be **selected in the hotbar** (not just in the inventory), its **Item Type** must be **Consumable**, and its Consumable numbers can't all be 0. The item's **prefab** needs **ItemPickup** with **Data** = its Item Data (the Console warns if not); pick it up again after fixing. |
| Dropped item looks like a box | It has no Item Data (old item): make one with the tool, then its real prefab drops. |
