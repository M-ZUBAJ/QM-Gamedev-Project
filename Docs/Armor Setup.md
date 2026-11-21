# Armor Setup (the Copper and Iron sets and the inventory's Gear page)

**What the player sees:**
- **The copper set (copper colored):** six armor items to craft from Copper in the Crafting tab: Copper Helmet, Copper
  Shoulders, Copper Chestplate, Copper Gloves, Copper Leggings and Copper Boots.
- **The iron set (steel grey):** six better pieces, each crafted from its Copper piece + Iron: Iron Helmet (Copper
  Helmet + 4 Iron), Shoulders (+ 3), Chestplate (+ 6), Gloves (+ 2), Leggings (+ 5) and Boots (+ 3). They protect more
  (16 % for the whole set) and all six give **+2 coins for every monster killed**. A worn Copper piece can be upgraded
  straight away: the recipe shows "1/1 Copper Helmet (worn)", and crafting takes it off and puts the new piece on in
  its place. Iron pieces also count as Copper, so a mix of the two sets still gives the Copper bonus while you upgrade
  one piece at a time.
- **Copper first, then iron:** iron is the higher tier. (Until 20/11/2026 it was the other way round: Iron came first
  and "Reinforced Iron" = an Iron piece + Copper was its upgrade. Running the tool renames those old files by tier.)
- **Colors:** both sets use Blink's plate shapes, so their colors tell them apart: Copper is copper with dark bronze
  trims, dark leather straps and a teal cloth; Iron is steel grey with dark iron trims, brown straps and a navy cloth.
- **In the crafting list,** each set's pieces sit together in one box in the set's color, with the set's name, how
  much the whole set protects, what wearing all six gives, and how many are worn ("BONUS ON" in gold with all six).
- **The Gear tab:** the player's character on the left, standing in a little studio (drag it to turn it round). On the
  right, the six armor slots, laid out like a body. Moving the mouse over a slot makes it grow, turn gold and shine,
  and its name shows next to the mouse. Clicking a slot opens a pop-up with the armor in the inventory that fits it.
- **From the cursor:** a piece picked up on the Inventory page (it sticks to the cursor) can be put straight on: open
  the Gear tab and click its slot. The piece worn there before goes onto the cursor, like Minecraft. A piece that doesn't
  fit that slot isn't taken, and the line under the slots says where it goes.
- **Wearing it:** clicking a piece in the pop-up puts it on. It shows on the character at once: on the Gear page's
  character, on the player's body (third person) and, for the gloves, on the first-person hands. Right-clicking a slot
  (or the pop-up's **Take off**) puts the piece back in the inventory.
- **Protection:** every copper piece takes a share off each hit: **1.5 %** for the helmet, shoulders, gloves, leggings
  and boots, **2.5 %** for the chestplate. The whole copper set takes **10 %** off, the whole iron set **16 %** (the
  most any armor can take is 80 %). Each slot shows its piece's share in its corner ("+1.5%"), and the line under the
  slots says the total ("Hits do 10% less damage").
- **Set bonus:** wearing all six copper pieces gives **+1 coin for every monster killed** (all six iron: +2), on top of
  the monster's own coins (and the Bounty Hunter perk's: `Docs/Loot Chests Setup.md`). Until 21/11/2026 the set
  bonus made stamina recharge faster. The Gear
  page counts the pieces next to the leggings ("Copper set 4/6") and turns gold with all six ("COPPER SET BONUS"). The
  inventory's **Info** tab shows it in gold too: the Coins row says "+1 per kill (Copper Set)" and the Armor row
  "Copper Set bonus", both on a gold-tinted background, so it stands out from the green perk bonuses.

The armor is the plate set already inside Blink's character (the parts `PlateSet1_...`, switched off), so it fits the
player's body exactly and bends with every animation. Both sets are that same plate set in different colors. The skin
under the clothes and the armor is hidden while playing, so it can't flicker through them (see **Skin under the
clothes** below).

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > Items > Build Plate Armor**, then **Build**.
3. Save the scene (**Ctrl + S**).

The tool:
- **Items:** makes 12 Item Data files in `Assets/Config/Items/Armor`:
  - `CopperHelmet` ... `CopperBoots`: **Protection** 1.5 (2.5 for the chestplate), **Set** = Copper, a recipe of
    Copper (5, 4, 8, 3, 7 and 4).
  - `IronHelmet` ... `IronBoots`: **Protection** 2.5, 2.5, 4, 2, 3 and 2 (16 % for all six), **Set** = Iron, a recipe
    of the Copper piece for the same slot + Iron (4, 3, 6, 2, 5 and 3).
  - each with Item Type **Armor**, its slot, the character's part to show and what that part hides, **Look** = its
    set's material, and a photo icon. A recipe you changed is kept when the tool runs again.
  - **Files from before the swap** (Iron first, then Reinforced Iron) are renamed by tier: the old `Iron...` files
    become the `Copper...` ones and the old `ReinforcedIron...` files the `Iron...` ones (Item Data and prefabs), so the
    crafting list keeps them. Their recipes get Iron and Copper swapped; the amounts (and anything else you changed)
    stay. The first version's `Plate...` files were renamed to `Iron...` back then (in v2.12).
- **Colors:** makes each set's material in `Assets/Materials/Armor` (`Copper Armor.mat`, `Iron Armor.mat`, and
  `Gold Armor.mat` ready for a future gold set: see **Gold armor (ready)** below), each with its own copy of Blink's
  palette picture (`... Armor Palette.png`; the old `Reinforced Iron Armor` files, already copper, are renamed to
  `Copper Armor`). Blink's armor takes its colors from
  strips of that picture; the four strips the plate parts use (the plates, the trims, the straps and the cloth) are
  repainted in the set's colors, keeping their light-to-dark shading, plus a little metal shine. Only made if missing,
  so your own changes stay: delete both files of a set to get the tool's colors back.
- **Prefabs:** makes the items' prefabs in `Assets/Prefabs/Item/Armor`: the piece frozen in the character's pose, in
  its set's colors, for its icon and for when it's dropped (both sets share the frozen shapes in `Meshes`, still named
  `Iron...` from when Iron was the first set).
- **The character:** puts **ArmorLook** on `Assets/Prefabs/Player/PlayerCharacter`. The body, the first-person hands
  and the Gear page's character all have it, so they all show the worn pieces. It also fills in ArmorLook's **Skin
  Under Clothes** list and makes the trimmed skin copies in `Assets/Prefabs/Player/SkinUnderClothes` (see below).
- **The Player:** puts **PlayerArmor** on the Player (what's worn, and how much it protects) with both sets' bonuses
  (a list saved before the swap loses its old Iron and Reinforced Iron lines first).
  It also puts **ArmorTestKit** on the Player with all 12 pieces, so GameRules **Testing > Armor Test** gives the whole
  of both sets (its own rule since 20/11/2026: **Tool Test** gives only tools now, and the tool takes the armor out of
  the ToolTestKit's list, where the first version had put it). This part needs the game scene open: run with another
  scene open (e.g. the title screen) and it's skipped. That's fine for testing: a game scene without an ArmorTestKit
  gets one by itself when it loads, and an empty kit gives every armor piece the game has loaded (the crafting list
  links them all). PlayerArmor also adds itself when it's missing, and puts an old bonus list right when the game starts.
- **Crafting list:** adds the armor to Turki's crafting list, after the tools, set by set (Copper, then Iron), and makes
  the set boxes (Tools > UI > Build Crafting Menu's sorting and set panels).
- **Gear page:** builds it (**Tools > UI > Build Gear Page**: the character on the left, the slots on the right, the
  set line and the pop-up). It changes the InventoryCanvas prefab, so every copy of the inventory follows.
- **Info tab:** adds the **Armor** row under Stamina, if the tab was made before the armor.

Run it again any time. It refills everything in place.

## How it works

| Part | Where | What it does |
|---|---|---|
| **ArmorStats** | the Armor section of an armor's Item Data | Slot, Protection (percent of every hit it takes off), Set (e.g. Copper), Piece (the character's part to switch on, e.g. `PlateSet1_Helmet`), Hides (parts to switch off while worn, e.g. `Hair` under the helmet, `Starter_Chest` under the chestplate), Look (the set's material) |
| **PlayerArmor** | the Player (added by itself if the Player has none, e.g. when the scene wasn't saved after the tool) | What's worn (one per slot). EquipFromCursor puts on the piece held on the cursor. Equip takes the piece out of the inventory and puts the old one back in; TakeOff puts it back if there's room. Every hit through `PlayerHealth.TakeDamage` is made smaller by the worn pieces' Protection added up (starving and bad food use `LoseHealth`, which armor can't stop). A piece of one set in every slot gives that set's bonus from **Set Bonuses** (**Coins Per Kill**: copper 1, iron 2; Marko's EnemyCombat gives them through MonsterBounty, which asks `CoinsPerKill`). A set with **Counts As** fills that set's slots too (Iron counts as Copper); when two sets are full, the one with more pieces of its own wins. Sets missing from the list are added when the game starts, and a list saved before 21/11/2026 (stamina bonuses, or Iron first then Reinforced Iron) is replaced by today's first (**UpdateOldBonuses**). **LendForUpgrade / FinishUpgrade**: crafting an upgrade of a worn piece takes it off for the recipe and wears the new one. |
| **CraftingMenu** (Turki's) | the crafting list | `[Tony]` lines call PlayerArmor's LendForUpgrade before checking an armor recipe and FinishUpgrade after (log `Docs/TonyEditMate/Turki/20-11-2026 Reinforced Iron Armor And Set Panels.md`). |
| **CraftingRow** | each crafting row | Counts the worn piece in an armor upgrade's recipe ("1/1 Copper Helmet (worn)"). |
| **CraftingSetPanels** | the crafting list's Panel | Puts each set's rows in one box in the set's color (see `Docs/Crafting Menu Setup.md`). |
| **MonsterBounty** | (code only) | The extra coins when a monster dies: the set bonus's Coins Per Kill + the Bounty Hunter perk. |
| **ArmorLook** | the PlayerCharacter prefab | Shows the worn pieces on that character in their set's material (Armor > Look) and hides what they cover; taking a piece off puts every part (and its material) back how it was. It also hides the skin under whatever clothes show (**Skin Under Clothes**, below). |
| **GearPage** | the Gear page | The line under the slots ("Hits do 10% less damage"), the set line ("Copper set 4/6", gold with all six, and under it the better set being worn toward: "Iron 3/6: +2 coins per kill"); closes the pop-up when the page is left. |
| **PlayerInfoPage** | the inventory's Info tab | The Armor row (the total protection, the set) and the set bonus on the Coins row, both in gold on a gold-tinted row while the whole set is worn. |
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
| | **Set Bonuses** | per set: **Set** (its name, as in the pieces' Armor > Set), **Coins Per Kill** (extra coins for every monster killed while the whole set is worn: copper 1, iron 2), **Counts As** (a lower set its pieces also fill: Iron = Copper) and **Color** (its box in the crafting list). Better sets go lower in the list. |
| an armor's Item Data | **Armor > Protection** | how much of every hit it takes off, in percent (copper: 1.5, the chestplate 2.5) |
| | **Armor > Set** | the set it belongs to (Copper, Iron) |
| | **Armor > Piece / Hides** | which part of the character it shows / hides |
| | **Armor > Look** | the material it's drawn with when worn, dropped and in its icon (the set's) |
| | **Recipe** | what crafting it costs (Copper; Iron: the Copper piece + Iron) |
| `Assets/Materials/Armor` | **Copper Armor**, **Iron Armor** | each set's material: **Albedo** color tints the whole set, **Metallic** / **Smoothness** its shine; its **... Armor Palette** picture holds the set's colors |
| GearPage (Gear page) | **Bonus Color** / **Hint Color** | the set line with the whole set on / before |
| PlayerInfoPage (Info tab) | **Set Bonus Color**, **Set Bonus Row Color** | the gold of the set bonus text, and of the rows it improves |
| GearPreview (Gear page > Preview > Character) | **Start Turn**, **Turn Speed**, **Field Of View** | the view of the character |
| | **Light Color / Intensity**, **Ambient Color** | the studio's light |

## Gold armor (ready, not made yet)

The colors for a gold set are ready, so it only takes a few lines once you want it:
- **Its look already exists:** `Assets/Materials/Armor/Gold Armor.mat` with its palette picture: bright gold plates,
  dark gold trims, red leather straps and a crimson cloth, shinier than the others (Metallic 0.75, Smoothness 0.7).
  Open it and drag it onto a piece's **Armor > Look** while playing to see the gold look early.
- **Its numbers are drafted** in `Editor/ArmorBuilder.cs` > `Sets` (switched off with `build = false`): 21 % for all six
  (3, 3, 5, 3, 4, 3), each piece = its Iron piece + Gold (4, 3, 6, 2, 5, 3). The Gold item comes from
  **Tools > World > Build Desert Content** (gold ore in the new world's desert caves, mined with an Iron Pickaxe).
- **To make it:** set `build = true` on the Gold set, add its bonus to `PlayerArmor.DefaultBonuses()` (for example
  `new SetBonus("Gold", 2.5f, "Iron", GoldColor)`: gold counts as Iron, which counts as Copper), and run **Tools >
  Items > Build Plate Armor** again. The crafting list gets a gold box by itself.

## A new piece of armor by hand

1. Right click in `Assets/Config/Items/Armor` > **Create > Items > Item Data**, and name it (e.g. `CopperHelmet`).
2. **Item Type** = **Armor**. In the Armor section: **Slot**, **Protection** (percent), **Set** (e.g. Copper, or a new
   set's name), **Piece** = the exact name of the part inside the character that shows it, **Hides** = parts to switch
   off while it's worn.
   A new set's bonus: on the Player's **PlayerArmor > Set Bonuses**, add a line with the set's name, its Coins Per
   Kill, its Color and (for an upgrade of another set) **Counts As**. The bonus needs a piece of that set (or of a set
   that counts as it) in all six slots. Its colors: duplicate a set's material in `Assets/Materials/Armor` (and its
   palette picture), change them, and drag the material into each piece's **Armor > Look** and its prefab's Model.
   An upgrade's recipe = the lower set's piece for the same slot (1) + the materials: crafting can then use a worn piece.
3. Give it a **Prefab** (for its icon) and a **Recipe**, then run **Tools > UI > Build Crafting Menu** to put it in the
   crafting list.

The character must have that part inside it: Blink's free character has the starter clothes and the plate set (Blink's
paid armor packs add more).

## Testing

1. Tick **GameRules > Testing > Armor Test** (or craft the pieces), press Play: all 12 pieces (Copper and Iron) are in
   the inventory. Ticking it while playing gives them at once; pieces you have or wear aren't given twice. From the
   title screen: **NEW GAME > TESTING > Start with all armor** does the same.
2. **Tab > Gear**: drag the character to turn it. Move the mouse over a slot: it grows and turns gold, and its name
   shows.
3. Click **Helmet** and pick the Copper Helmet: it shows on the character in copper, the slot shows its picture and
   "+1.5%", the line under the slots says "Hits do 1.5% less damage", and the set line says "Copper set 1/6".
4. Put on the whole set, close the inventory, switch to third person: the player wears it. Turn the Gear page's
   character with and without armor: the shoulders, gloves and boots don't flicker.
5. With all six on, the set line turns gold ("COPPER SET BONUS: +1 coin for every monster killed"). PlayerHealth's
   **⋮ > Test: Take Damage** (10) now takes 9. Kill a slime: one more coin than before ("+1 Coins" after its own).
6. **Info** tab: the Coins row says "+1 per kill (Copper Set)" in gold and the Armor row "10%" with "Copper Set
   bonus", both rows tinted gold. Take one piece off: the gold goes, and the Armor row says "Copper set 5/6".
7. Right-click a slot: the piece goes back to the inventory.
8. On the Inventory page click the helmet (it sticks to the cursor), open Gear, click the Helmet slot: it goes on. With
   boots on the cursor instead, the line under the slots says "Copper Boots goes in the Boots slot."
9. **Iron:** with the copper set on and some Iron, open **Crafting > Armor**: the Copper pieces are in a copper box
   ("BONUS ON"), the Iron pieces in a steel-grey box. The Iron Helmet's row says "1/1 Copper Helmet (worn)". Click it:
   the helmet on the character turns steel grey, the Gear page's Helmet slot shows "+2.5%", the Copper bonus stays (the
   set line adds "Iron 1/6: +2 coins per kill" under it).
10. Upgrade all six: the set line says "IRON SET BONUS: +2 coins for every monster killed", the steel-grey box says
    "BONUS ON", and the Info tab's Coins row "+2 per kill (Iron Set)". Test: Take Damage (10) takes 8.4.
11. Without enough Iron, clicking an Iron row says what's missing ("Missing: 2 Iron") and the worn Copper piece stays
    on.

## Problems

| Problem | Why / fix |
|---|---|
| The Gear page still says "coming soon" | Run **Tools > UI > Build Gear Page** (Build Plate Armor runs it). |
| No character on the Gear page | GearPreview's **Character Prefab** is empty: run Build Gear Page again (it needs `Assets/Prefabs/Player/PlayerCharacter`). |
| The pop-up says there's no armor | The armor must be in the inventory or hotbar. A piece on the cursor goes on by clicking its slot instead. |
| A slot's pop-up found no armor although it was in the inventory | The Player had no PlayerArmor, because GameScene wasn't saved after Build Plate Armor (the title screen opens the saved scene). PlayerArmor and the Armor Test kit now add themselves when they're missing; still save GameScene after the tool. |
| Worn armor doesn't show on the player | PlayerCharacter has no **ArmorLook**: run Build Plate Armor again. Or the piece's **Armor > Piece** name doesn't match a part inside the character. |
| Hair pokes through the helmet | Add the hair's part name to the helmet's **Armor > Hides**. |
| Clothes or armor flicker with the skin's colour in spots that change many times a second | The skin under them isn't hidden: check ArmorLook's **Skin Under Clothes** (see above) and run Build Plate Armor again for the trimmed copies. |
| A see-through hole in the character | A row's **Hides** has a body part that piece doesn't cover from every side: take it out of Hides and run Build Plate Armor again (it gets a trimmed copy instead). |
| "No room in the inventory" | Taking off needs a free slot (or a stack of the same piece). |
| Armor doesn't protect | The Player needs **PlayerArmor** (Build Plate Armor adds it; open GameScene first and save it after). |
| The pieces still say "Reinforced Iron", the Iron pieces cost Iron alone, or the first set is still Iron | Their Item Data is from before the swap: run Build Plate Armor once (it renames the files by tier, swaps Iron and Copper in their recipes and sets Protection, Set and Look). |
| No set bonus with all six on | Every piece needs the same **Armor > Set** (e.g. Copper), and PlayerArmor's **Set Bonuses** needs a line for that set. |
| No set line on the Gear page, or no Armor row on the Info tab | They came with this version: run Build Plate Armor again (it rebuilds the Gear page and adds the row). |
| No Copper or Iron pieces, or the armor still has Blink's colors | Run Build Plate Armor again (with GameScene open, then save). The recipes need the **Copper** and **Iron** items (`Assets/Config/Items/Copper`, `.../Iron`). |
| The set colors don't change after editing them in the tool | The materials are only made if missing: delete `Assets/Materials/Armor/<Set> Armor.mat` and its palette picture, then run the tool. Or change the material's Albedo color by hand. |
| The Iron row says "0/1 Copper Helmet" | The Copper Helmet is neither worn nor in the inventory. A worn one counts ("(worn)"). |
| No Iron bonus with all six on, or only +1 coin | PlayerArmor's **Set Bonuses** needs an Iron line with Coins Per Kill 2 that counts as Copper. A list from before 21/11/2026 is put right by itself when the game starts (and by Build Plate Armor / Build Loot Chests). |
| The armor isn't in boxes in the crafting list | Run **Tools > UI > Build Crafting Menu** (or Build Plate Armor) to add the set panels. |

Change logs for Turki's crafting list: `Docs/TonyEditMate/Turki/20-11-2026 Plate Armor In The Crafting List.md` and
`.../20-11-2026 Reinforced Iron Armor And Set Panels.md` (with the Copper-first update at its end).
