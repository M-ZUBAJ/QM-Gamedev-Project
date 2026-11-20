# Armor Setup (the iron armor set and the inventory's Gear page)

**What the player sees:**
- **The iron set:** six armor items to craft from Iron in the Crafting tab: Iron Helmet, Iron Shoulders, Iron
  Chestplate, Iron Gloves, Iron Leggings and Iron Boots.
- **The Gear tab:** the player's character on the left, standing in a little studio (drag it to turn it round). On the
  right, the six armor slots, laid out like a body. Moving the mouse over a slot makes it grow, turn gold and shine,
  and its name shows next to the mouse. Clicking a slot opens a pop-up with the armor in the inventory that fits it.
- **From the cursor:** a piece picked up on the Inventory page (it sticks to the cursor) can be put straight on: open
  the Gear tab and click its slot. The piece worn there before goes onto the cursor, like Minecraft. A piece that doesn't
  fit that slot isn't taken, and the line under the slots says where it goes.
- **Wearing it:** clicking a piece in the pop-up puts it on. It shows on the character at once: on the Gear page's
  character, on the player's body (third person) and, for the gloves, on the first-person hands. Right-clicking a slot
  (or the pop-up's **Take off**) puts the piece back in the inventory.
- **Protection:** every piece takes a share off each hit: **1.5 %** for the helmet, shoulders, gloves, leggings and
  boots, **2.5 %** for the chestplate. The whole set takes **10 %** off (the most any armor can take is 80 %). Each slot
  shows its piece's share in its corner ("+1.5%"), and the line under the slots says the total ("Hits do 10% less
  damage").
- **Set bonus:** wearing all six iron pieces makes **stamina recharge 50 % faster**. The Gear page counts the pieces
  next to the leggings ("Iron set 4/6") and turns gold with all six ("IRON SET BONUS"). The inventory's **Info** tab
  shows it in gold too: the Stamina row says "+50% recharge (Iron Set)" and the Armor row "Iron Set bonus", both on a
  gold-tinted background, so it stands out from the green perk bonuses.

The armor is the plate set already inside Blink's character (the parts `PlateSet1_...`, switched off), so it fits the
player's body exactly and bends with every animation. It's made from Iron, so the game calls it the iron set (it was
called "Plate" at first; running the tool renames the old files). The skin under the clothes and the armor is hidden while
playing, so it can't flicker through them (see **Skin under the clothes** below).

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > Items > Build Plate Armor**, then **Build**.
3. Save the scene (**Ctrl + S**).

The tool:
- **Items:** makes the 6 Item Data files in `Assets/Config/Items/Armor` (`IronHelmet` ... `IronBoots`). Each has Item
  Type **Armor**, its slot, **Protection** (1.5, or 2.5 for the chestplate), **Set** = Iron, the character's part to
  show and what that part hides, a photo icon, and a recipe of Iron (5, 4, 8, 3, 7 and 4). A recipe you changed is
  kept when the tool runs again. The first version called them `PlateHelmet` and so on: those files are renamed, so
  the crafting list and the Tool Test kit keep them.
- **Prefabs:** makes the items' prefabs in `Assets/Prefabs/Item/Armor`: the piece frozen in the character's pose,
  for its icon and for when it's dropped.
- **The character:** puts **ArmorLook** on `Assets/Prefabs/Player/PlayerCharacter`. The body, the first-person hands
  and the Gear page's character all have it, so they all show the worn pieces. It also fills in ArmorLook's **Skin
  Under Clothes** list and makes the trimmed skin copies in `Assets/Prefabs/Player/SkinUnderClothes` (see below).
- **The Player:** puts **PlayerArmor** on the Player (what's worn, and how much it protects). It also adds the armor
  to its **ToolTestKit**, so GameRules **Testing > Tool Test** (or the title screen's TESTING option) gives the armor too.
- **Crafting list:** adds the armor to Turki's crafting list, after the tools.
- **Gear page:** builds it (**Tools > UI > Build Gear Page**: the character on the left, the slots on the right, the
  set line and the pop-up). It changes the InventoryCanvas prefab, so every copy of the inventory follows.
- **Info tab:** adds the **Armor** row under Stamina, if the tab was made before the armor.

Run it again any time. It refills everything in place.

## How it works

| Part | Where | What it does |
|---|---|---|
| **ArmorStats** | the Armor section of an armor's Item Data | Slot, Protection (percent of every hit it takes off), Set (e.g. Iron), Piece (the character's part to switch on, e.g. `PlateSet1_Helmet`), Hides (parts to switch off while worn, e.g. `Hair` under the helmet, `Starter_Chest` under the chestplate) |
| **PlayerArmor** | the Player (added by itself if the Player has none, e.g. when the scene wasn't saved after the tool) | What's worn (one per slot). EquipFromCursor puts on the piece held on the cursor. Equip takes the piece out of the inventory and puts the old one back in; TakeOff puts it back if there's room. Every hit through `PlayerHealth.TakeDamage` is made smaller by the worn pieces' Protection added up (starving and bad food use `LoseHealth`, which armor can't stop). A piece of one set in every slot gives that set's bonus from **Set Bonuses** (Iron: it tells PlayerStamina to refill 1.5 times as fast). |
| **PlayerStamina** | the Player | `SetRegenBoost`: how much faster stamina refills, given by other scripts (the set bonus). The short rest before it starts stays the same. |
| **ArmorLook** | the PlayerCharacter prefab | Shows the worn pieces on that character and hides what they cover; taking a piece off puts every part back how it was. It also hides the skin under whatever clothes show (**Skin Under Clothes**, below). |
| **GearPage** | the Gear page | The line under the slots ("Hits do 10% less damage"), the set line ("Iron set 4/6", gold with all six); closes the pop-up when the page is left. |
| **PlayerInfoPage** | the inventory's Info tab | The Armor row (the total protection, the set) and the set bonus on the Stamina row, both in gold on a gold-tinted row while the whole set is worn. |
| **ArmorSlotUI** | each slot | Shows the worn piece (or a faint outline), the name next to the mouse. With a stack on the cursor, a click puts that piece on (or says why not); otherwise left click = the pop-up, right click = take off. The grow / gold / shine hover is **ButtonFX**. |
| **ArmorPicker** | the pop-up | Lists the armor in the inventory that fits (each kind once, "x2" for more); click = wear it. Esc, the X or a click outside closes it. |
| **GearPreview** | the character picture | A copy of PlayerCharacter in a studio far below the world, on its own layer (29) with its own camera and light. The world's lights, the night and the game camera never touch it. It's drawn into the picture only while the page is open; drag to turn. |

**When the player dies:** the armor follows the same GameRules as the items. Keep Inventory off (the default) means
it's dropped around the body with the items. Clear Item On Dead deletes it. Keep Inventory on means it stays on.

**First person:** the body only casts its shadow in first person, so a helmet never blocks the view. The hands only
ever draw their arms, hands and gloves, so plate gloves show on the first-person hands too.

## Skin under the clothes (no flicker)

Blink's clothes sit right on the body's skin: the starter shirt's sleeves are the arm's skin pushed out a hair, and in
places not at all. When two surfaces are in the same spot, the graphics card can't tell which one is in front, and it
picks differently from one frame to the next as the body moves. The shoulders flickered between the shirt and the skin
many times a second (this is called **z-fighting**). So while a piece of clothing shows, ArmorLook hides the skin
under it:

| Clothing | Skin switched off under it | Skin drawn with a trimmed copy |
|---|---|---|
| Starter_Chest (shirt) | Arms, Chest | |
| Starter_Pants | Legs | |
| Starter_Boots | Feet | |
| PlateSet1_Chest (chestplate) | Chest | Arms: the tops of the shoulders show through the chestplate, so the arms must stay |
| PlateSet1_Pants (leggings) | Legs | |
| PlateSet1_Boots | Feet | |
| PlateSet1_Gloves | | Hands: the fingertips show |

- **Switched off:** only skin a piece covers from **every** side. These were checked on the model: nowhere can you see
  through the character. A hair-thin slit can open at the ankle when the plate leggings are worn with the starter boots,
  or the starter trousers with the plate boots (about 2 mm, under a pixel at normal viewing distance).
- **Trimmed copy:** skin a piece covers only partly is drawn with a copy of its mesh without the faces lying under the
  piece (closer than about 3 mm). Build Plate Armor makes them in `Assets/Prefabs/Player/SkinUnderClothes`
  (`Body_Arms under PlateSet1_Chest`, `Body_Hands under PlateSet1_Gloves`), for the body and, separately, for the
  first-person hands (`Body_Hands FP under PlateSet1_Gloves`). Those have arm and hand meshes of their own, so each
  copy is only used on the part with the mesh it was made from.
- **First person:** the hands copy never draws the shirt, so its arms stay on even when the body's shirt hides the
  body's arms.
- **The list:** ArmorLook > **Skin Under Clothes** (on the PlayerCharacter prefab). It comes filled in for Blink's
  character, even before the tool runs. Running Build Plate Armor again keeps the Hides you changed and makes the trimmed
  copies again.

For a new piece of clothing that flickers: add a row with its part name. Put a body part in **Hides** only if the
piece covers it from every side (otherwise you'd see through the character there). Then run Build Plate Armor again: it
makes a trimmed copy for any skin that lies on the piece (at least 1 % of it within about 1 mm) and isn't hidden.

## Settings

| Where | Setting | What it does |
|---|---|---|
| PlayerArmor (Player) | **Max Protection** (80) | the most armor can take off a hit, in percent |
| | **Set Bonuses** | per set: **Set** (its name, as in the pieces' Armor > Set) and **Stamina Regen** (1.5 = stamina refills 50 % faster while the whole set is worn) |
| an armor's Item Data | **Armor > Protection** | how much of every hit it takes off, in percent (iron: 1.5, the chestplate 2.5) |
| | **Armor > Set** | the set it belongs to (Iron) |
| | **Armor > Piece / Hides** | which part of the character it shows / hides |
| | **Recipe** | what crafting it costs (Iron by default) |
| GearPage (Gear page) | **Bonus Color** / **Hint Color** | the set line with the whole set on / before |
| PlayerInfoPage (Info tab) | **Set Bonus Color**, **Set Bonus Row Color** | the gold of the set bonus text, and of the rows it improves |
| GearPreview (Gear page > Preview > Character) | **Start Turn**, **Turn Speed**, **Field Of View** | the view of the character |
| | **Light Color / Intensity**, **Ambient Color** | the studio's light |

## A new piece of armor by hand

1. Right click in `Assets/Config/Items/Armor` > **Create > Items > Item Data**, and name it (e.g. `IronHelmet`).
2. **Item Type** = **Armor**. In the Armor section: **Slot**, **Protection** (percent), **Set** (e.g. Iron, or a new
   set's name), **Piece** = the exact name of the part inside the character that shows it, **Hides** = parts to switch
   off while it's worn.
   A new set's bonus: on the Player's **PlayerArmor > Set Bonuses**, add a line with the set's name and its Stamina
   Regen. The bonus needs a piece of that set in all six slots.
3. Give it a **Prefab** (for its icon) and a **Recipe**, then run **Tools > UI > Build Crafting Menu** to put it in the
   crafting list.

The character must have that part inside it: Blink's free character has the starter clothes and the plate set (Blink's
paid armor packs add more).

## Testing

1. Tick **GameRules > Testing > Tool Test** (or craft the pieces from Iron), press Play.
2. **Tab > Gear**: drag the character to turn it. Move the mouse over a slot: it grows and turns gold, and its name
   shows.
3. Click **Helmet** and pick the Iron Helmet: it shows on the character, the slot shows its picture and "+1.5%", the
   line under the slots says "Hits do 1.5% less damage", and the set line says "Iron set 1/6".
4. Put on the whole set, close the inventory, switch to third person: the player wears it. Turn the Gear page's
   character with and without armor: the shoulders, gloves and boots don't flicker.
5. With all six on, the set line turns gold ("IRON SET BONUS: Stamina recharges 50% faster"). PlayerHealth's
   **⋮ > Test: Take Damage** (10) now takes 9. Sprint until the stamina bar is empty: it refills in about 2.7 seconds
   instead of 4 (after the usual 1 second rest).
6. **Info** tab: the Stamina row says "+50% recharge (Iron Set)" in gold and the Armor row "10%" with "Iron Set bonus",
   both rows tinted gold. Take one piece off: the gold goes, and the Armor row says "Iron set 5/6".
7. Right-click a slot: the piece goes back to the inventory.
8. On the Inventory page click the helmet (it sticks to the cursor), open Gear, click the Helmet slot: it goes on. With
   boots on the cursor instead, the line under the slots says "Iron Boots goes in the Boots slot."

## Problems

| Problem | Why / fix |
|---|---|
| The Gear page still says "coming soon" | Run **Tools > UI > Build Gear Page** (Build Plate Armor runs it). |
| No character on the Gear page | GearPreview's **Character Prefab** is empty: run Build Gear Page again (it needs `Assets/Prefabs/Player/PlayerCharacter`). |
| The pop-up says there's no armor | The armor must be in the inventory or hotbar. A piece on the cursor goes on by clicking its slot instead. |
| A slot's pop-up found no armor although it was in the inventory | The Player had no PlayerArmor, because GameScene wasn't saved after Build Plate Armor (the title screen opens the saved scene). PlayerArmor now adds itself when it's missing; still save GameScene, so the Tool Test kit keeps the armor. |
| Worn armor doesn't show on the player | PlayerCharacter has no **ArmorLook**: run Build Plate Armor again. Or the piece's **Armor > Piece** name doesn't match a part inside the character. |
| Hair pokes through the helmet | Add the hair's part name to the helmet's **Armor > Hides**. |
| Clothes or armor flicker with the skin's colour in spots that change many times a second | The skin under them isn't hidden: check ArmorLook's **Skin Under Clothes** (see above) and run Build Plate Armor again for the trimmed copies. |
| A see-through hole in the character | A row's **Hides** has a body part that piece doesn't cover from every side: take it out of Hides and run Build Plate Armor again (it gets a trimmed copy instead). |
| "No room in the inventory" | Taking off needs a free slot (or a stack of the same piece). |
| Armor doesn't protect | The Player needs **PlayerArmor** (Build Plate Armor adds it; open GameScene first and save it after). |
| The pieces still say "Plate" or "+2", or a slot shows "+2%" | Their Item Data is from the first version: run Build Plate Armor again (it renames the files and sets Protection and Set). |
| No set bonus with all six on | Every piece needs the same **Armor > Set** (Iron), and PlayerArmor's **Set Bonuses** needs a line for that set. |
| No set line on the Gear page, or no Armor row on the Info tab | They came with this version: run Build Plate Armor again (it rebuilds the Gear page and adds the row). |

Change log for Turki's crafting list: `Docs/TonyEditMate/Turki/20-11-2026 Plate Armor In The Crafting List.md`.
