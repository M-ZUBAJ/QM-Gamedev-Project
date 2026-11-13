# Tool Setup (tiered pickaxes, axes and swords)

**What the player sees:** 15 tools from Abduo Bricks' free **Low Poly Tool Pack**: a pickaxe, an axe and a sword in
**Wood, Stone, Iron, Gold and Diamond**.
- **In the hand:** select a tool in the hotbar and the character holds it in its right fist, in first and third person.
  In first person the hands sit a little lower while a tool is in the fist (FirstPersonHands > **Tool Lower**, 15 % of the screen by default), so
  the tool doesn't cover the view. Axe and pickaxe handles are 30 % shorter than in the pack (the head keeps its shape).
- **Swinging:** left click swings it. Every tool chops down (Mixamo's Axe Attack Downward), and an empty hand still
  punches. The hit lands when the swing reaches its lowest point, with the grey wind burst (like a punch).
- **Mining:** copper and iron need a **Stone pickaxe or better**. A bare hand, axe or sword can't break them, and a
  message says *"Needs a Stone Pickaxe or better"*.
- **Each tool is best at its own job:**
  - an axe chops trees fast;
  - a pickaxe breaks rock and ores fast;
  - a sword hits enemies and animals hard;
  - each tier is 25 % stronger at that job than the one before.

They replaced Marko's three placeholder tools (Sword, Axe, PickAxe). Change logs:
`Docs/TonyEditMate/Marko/12-11-2026 Tier Tools.md` and `Docs/TonyEditMate/Turki/12-11-2026 Tier Tools.md`.

## The rules

| | Wood | Stone | Iron | Gold | Diamond |
|---|---|---|---|---|---|
| **Pickaxe can mine** | stone | + copper, iron | + gold | + diamond | everything |
| **Strength at its job** (x Marko's numbers) | x1 | x1.25 | x1.56 | x1.95 | x2.44 |
| **Sword damage** (Marko's Sword Damage 25) | 25 | 31 | 39 | 49 | 61 |
| **Recipe** | Planks (pickaxe 4, axe 3, sword 2) | 2 Plank + Stone (pickaxe 2, axe 1, sword 1) | 2 Plank + Iron (same counts) | none yet | none yet |

- **A bare hand** can still punch trees, stone and animals (slowly), but no ore.
- **Copper and iron:** copper counts as the same tier as iron ("copper or iron can mine gold"). The pack has no copper
  tools.
- **Gold and diamond:** there are no gold or diamond ores or items in the game yet. The rules are ready for them: give
  the new item's Item Data a **Mined With** (gold: Iron, diamond: Gold). Gold and diamond tools come from the test rule
  for now.
- **Marko's damage numbers are unchanged** (on the Player's PlayerCombat: Hand Damage, Sword Damage, Axe Tree Damage,
  Pick Axe Stone Damage...). They're what a wooden tool does, and the tiers multiply them.

## Quick way: one click

1. Import the **Low Poly Tool Pack** (free, Abduo Bricks): **Window > Package Manager > My Assets > Import**.
2. Open `Assets/Scenes/GameScene.unity`.
3. Menu **Tools > Items > Build Tier Tools** and choose **Build + replace Marko's tools**. It does all of this:
   - **The tools:** the 15 tools' Item Data in `Assets/Config/Items/Tools` (e.g. `StonePickaxe`) and their prefabs in
     `Assets/Prefabs/Item/Tools`.
   - **Materials:** Standard copies of the pack's materials in `Assets/Materials/Tools`. The pack's own materials are
     made for URP and would show pink in our Built-in project.
   - **Ores:** Copper's and Iron's **Mined With** become **Stone**.
   - **The hands:** **ToolInHand** goes on `PlayerCharacter.prefab`, so your body and the first-person hands hold the
     selected tool.
   - **The Player:** gets **ToolTestKit**, and its **PunchEffect** gets the sword / axe and pickaxe swing timing,
     measured from the animations.
   - **Turki's crafting list:** Sword / Axe / PickAxe become **Iron Sword / Stone Axe / Stone Pickaxe**, with the same
     recipes.
   - **Marko's old tools:** GiveSwordForTesting and EquippedWeaponDisplay leave the Player, the three weapon models
     leave the Main Camera, and the old Sword / Axe / PickAxe files go to the trash (your computer's bin; git has them
     too).
   - **Only build the tools** skips Turki's crafting list and Marko's old tools.
4. Save the scene (**Ctrl + S**).

```
StonePickaxe    Box Collider (aim + F to pick up) + ItemPickup (StonePickaxe, 1)
  Model         the pack's Pickaxe_Stone (Handle + ToolHead), leaning like an icon. ToolInHand copies this into the hand.
```

## Test

1. Select the **GameRules** object and tick **Testing > Tool Test**. You can also tick it while playing.
2. Press Play. All 15 tools are in your inventory.
3. Put a tool in the hotbar and select it (1 – 8). It's in the right hand: look in first person, and in third person.
4. Left click at a tree with an axe, at stone with a pickaxe, at an enemy or deer with a sword.
5. Try a copper or iron rock:
   - with your hand or a wooden pickaxe: *"Needs a Stone Pickaxe or better"*;
   - with a stone pickaxe or better: it breaks.

## Settings

| Where | Field | What it does |
|---|---|---|
| Character (and FirstPersonHands) > **ToolInHand** | **Held Size** (0.8) | How big a held tool is (1 = the pack's size, about 1 m). |
| | **Axe Turn** (180) | Spins a held axe around its handle so its sharp edge faces the other way (0 = the pack's way). |
| | **Grip Nudge**, **Grip Turn** | Move / turn the tool in the fist if it doesn't sit right. Change them while playing, then Copy Component and Paste Component Values after stopping. |
| `HandleShare` at the top of `Assets/Script/Editor/TierToolBuilder.cs` | (0.7) | How long axe and pickaxe handles are, as a share of the pack's (0.7 = 30 % shorter, 1 = the pack's length). Change it, then **Tools > Items > Rebuild Tool Models**: it rebuilds only the 15 tool prefabs, leaving names and recipes alone. |
| Player > **PunchEffect** | **Swing Delay**, **Mine Delay** | Seconds from the click until a sword / axe or pickaxe swing lands (measured by the tool). |
| A tool's Item Data | **Item Type** Tool, **Tool > Kind / Tier** | What the tool is. |
| An ore's Item Data | **Mining > Mined With** | The weakest pickaxe that can mine a rock that drops it. None = anything, even a hand. |
| Player > **PlayerCombat** (Marko) | the damage numbers | What a hand or a wooden tool does. |
| `Assets/Script/Tool/ToolRules.cs` | `TierBonus` (1.25) | How much stronger each tier is. |
| GameRules | **Tool Test** | Gives one of every tool (default off). |

**How the grip is worked out:** ToolInHand measures the hand's own bones (wrist, knuckles, index and little finger). The
handle goes through the fist toward the thumb, and the blade faces where the fingers point. It fits any Humanoid
character. The pack's tools have their pivot at the bottom of the grip, the handle along their Y, and the blade or head
along their X.

## By hand (one tool)

1. **Material:** right-click > **Create > Material**, Shader **Standard**, Albedo colour like the pack's material.
   Make one per pack material, or let the tool do it.
2. **Prefab:**
   1. Create an empty object named `StonePickaxe`.
   2. Drag the pack's `Prefabs/Pickaxe_Stone` onto it, and rename the child **Model** (ToolInHand looks for that name).
   3. Give its two renderers the Standard materials.
   4. On `StonePickaxe`: add **Box Collider** around it and **ItemPickup** (Amount 1).
   5. Drag it into `Assets/Prefabs/Item/Tools`.
3. **Item Data:**
   1. `Assets/Config/Items/Tools` > right-click > **Create > Items > Item Data**, named `StonePickaxe`.
   2. Prefab = the prefab, Display Name `Stone Pickaxe`, **Item Type Tool**, **Tool > Kind Pickaxe, Tier Stone**.
   3. Recipe 2 Plank + 2 Stone.
   4. Drag this file into the prefab's ItemPickup **Data**.
4. **Ores:** `Assets/Config/Items/Copper` and `Iron` > **Mining > Mined With: Stone**.
5. **Hands:** open `Assets/Prefabs/Player/PlayerCharacter.prefab` and add **ToolInHand** to its top object.
6. **Test rule:** Player > Add Component > **ToolTestKit**, and drag the 15 tools into **Tools**.

## Problems

| Problem | Why / fix |
|---|---|
| Tools are pink | A renderer still has the pack's URP material. Run the tool again, or give it the Standard copies in `Assets/Materials/Tools`. |
| No tool in the hand | **The character:** PlayerCharacter.prefab has no ToolInHand. Run the tool again.<br>**The item:** it isn't a tool (Item Type must be Tool) or isn't selected in the hotbar. |
| The tool sits wrong in the fist | Fix it with **Grip Turn** / **Grip Nudge** on ToolInHand (Character for third person, FirstPersonHands for first person) while playing. For example Grip Turn y 180 turns the blade the other way. |
| A floating tool in front of the camera too | That was Marko's EquippedWeaponDisplay / weapon models. Run the tool with **Build + replace Marko's tools**. HeldItemView doesn't show tools any more. |
| "Needs a Stone Pickaxe" on stone | Stone's Item Data has a **Mined With**. Set it back to None. |
| Copper / iron break with a bare hand | Their **Mined With** is None. Set Stone. |
| No tools with Tool Test on | The Player has no ToolTestKit, or its list is empty. Run the tool with GameScene open. |
| The hit comes before / after the tool lands | Change **Swing Delay** / **Mine Delay** on the Player's PunchEffect. |
| Fireball won't cast | Marko's fireball needs a sword in the hand: any tier works now. |
