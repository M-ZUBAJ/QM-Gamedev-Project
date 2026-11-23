# Trap Setup (the Bear Traps you build with B)

**What the player does:** during the day, press **B**, open the **Traps** tab and place traps on the ground where
monsters will come. You stay in build mode, so you can place several in a row. At night, a walking monster that steps
onto one is caught:

- the two jaws **snap shut**, the monster takes the trap's **damage** (a kill gives its coins as usual) and "CAUGHT!"
  floats up;
- it's **held still** for the trap's **stun** time: it can't move or attack, so you can hit it freely (your punches
  don't push it out);
- after its last use it's used up and goes away ("Used up"). A trap with 2 uses stays shut for 6 seconds after letting
  go, then opens again.

There are four Bear Traps:

| Trap | Recipe | Uses | Damage | Stun |
|---|---|---|---|---|
| **Copper Bear Trap** | 4 Copper | 1 | 10 | 2 s |
| **Iron Bear Trap** | a placed Copper Bear Trap + 3 Iron | 1 | 20 | 3 s |
| **Gold Bear Trap** | a placed Iron Bear Trap + 4 Gold | 2 | 25 | 3 s |
| **Diamond Bear Trap** | a placed Gold Bear Trap + 4 Diamond | 2 | 30 | 4 s |

**Iron, Gold and Diamond are upgrades**, one step at a time: Copper -> Iron -> Gold -> Diamond. You don't build them
anywhere: each one goes on a placed trap of the tier before it. For example, for an Iron Bear Trap: build a Copper Bear
Trap, then pick the Iron Bear Trap in the B menu and **aim at the Copper Bear Trap** (within about 1 m). The blueprint
jumps onto it and turns green. Left click: the Copper Bear Trap is replaced by the new one, and you pay only the extra
metal. Then a Gold Bear Trap goes on that Iron Bear Trap, and a Diamond Bear Trap on a Gold one. Away from the right
trap the blueprint is red, and a click says what it needs ("Place it on an Iron Bear Trap"). In the B menu an upgrade's
recipe starts with that trap: "on a Copper Bear Trap", "on an Iron Bear Trap", "on a Gold Bear Trap". The new trap
starts fresh: all its uses, jaws open.

**Careful: they hurt you too.** Step on one yourself and it snaps on you (its damage, less with armor; "Ouch! You
stepped on a trap.") and that counts as one of its uses. Flying monsters (the beholder) fly over traps, and animals
never set them off.

Rest the mouse on a trap in the build menu for 1 second to see all this in its info box.

(The first trap was called **Bear Trap**, then **Iron Trap**. The tool renames its old files to the Iron Bear Trap and
keeps their links.)

## Quick way: one click

1. The model must be in the project: `Assets/Imported3D/CGTraderBearTrap/beartrap.fbx` (CGTrader's free "Metalic Bear
   trap Rigged Free", see `Docs/Assets We Use.md`).
2. Menu **Tools > Building > Build Traps**, then **Build**. No scene changes.
3. The build menu needs its tabs and info box once: **Tools > UI > Build Build Menu UI**, then **Rebuild** (see
   `Docs/Build Menu Setup.md`).
4. Play and test (below).

The tool:

- **Renames the old Iron Trap files**: its prefab, Building Data and material to Iron Bear Trap, its 3 meshes to
  Bear Trap Base / Jaw Front / Jaw Back. It keeps their links (the build menu's list), but only while the new name
  isn't taken yet.
- **Reads the model** as it rests, centers it with its bottom on the ground and makes it **0.7 m** wide.
- **Cuts it into 3 meshes** in `Assets/Prefabs/Building/Traps/Meshes`, shared by all four traps:
  - **Base**: the plate in the middle and the bar across it;
  - **Jaw Front** and **Jaw Back**: the two halves of the ring of teeth around it.

  Each jaw turns around the **hinge**, the trap's long middle line at ground level, so closing it lifts it into an
  arch with the teeth meeting in the middle. (The model's own bones can't do that: its 4 jaw bones sit at the ring's
  edge, not on the hinge, and its 2 "animations" are single still frames.)
- **Makes a material per trap** in `Assets/Materials/Traps` (`Copper Bear Trap.mat` copper, `Iron Bear Trap.mat` dark
  metal, `Gold Bear Trap.mat` gold, `Diamond Bear Trap.mat` ice blue), only if missing, so a color you change stays.
- **Makes a prefab per trap**, `Assets/Prefabs/Building/Traps/<Tier> Bear Trap.prefab`:
  - its top object is on the **Ignore Raycast** layer, with a trigger **Box Collider**: the catching box, 0.5 m tall.
    Monsters walk into it, nothing bumps into it, and the crosshair, attacks and E look through it;
  - **BearTrap** (the trap's script) with the trap's damage, stun, uses and player damage, and the jaws filled in;
  - **StandOnGround**, so it lies flat on the ground, on a slope too;
  - **Model** > Base, Jaw Front, Jaw Back.
- **Makes a Building Data per trap**, `Assets/Config/Building/<Tier> Bear Trap.asset`:
  - Piece Type **Trap**, so it goes in the **Traps** tab;
  - a photo of the trap as its picture;
  - its description for the info box;
  - its recipe, and for Iron, Gold and Diamond **Upgrade Of** = the Building Data of the trap it goes on (Iron: Copper
    Bear Trap, Gold: Iron Bear Trap, Diamond: Gold Bear Trap); that's what makes them upgrades.

  They appear in the B menu by themselves. The tool then puts the four together in the menu's list, Copper first.
- **Makes Gold or Diamond** (the items) if the project doesn't have them yet, as Build Desert Content / Build Snow
  Content do.

Run the tool again any time: the traps are rebuilt in the same files. **The four traps' numbers and recipes are
written every run** from the table at the top of `Assets/Script/Editor/TrapBuilder.cs` (`Tiers`): change them there.
The other BearTrap settings (catch radius, words, jaw speed...), the materials' colors and your descriptions stay.

## Testing

1. Open `Assets/Scenes/GameScene.unity` and press **Play**.
2. Get the metals (4 Copper, then 3 Iron, 4 Gold and 4 Diamond). For a quick test, empty a recipe instead: click its
   Building Data in `Assets/Config/Building`, select its Recipe lines and press **-** (run the tool again afterwards: it
   writes the recipes back).
3. Press **B** and open the **Traps** tab: Copper, Iron, Gold and Diamond Bear Trap. Rest the mouse on one: after 1
   second its info box shows (an upgrade starts with the trap it goes on, e.g. "Built on an Iron Bear Trap (it
   replaces it)").
4. Click **Copper Bear Trap**, aim at the ground and left click: it's built, and the next one follows the crosshair
   (you're still building). Right click = back to the menu, **B** or **Esc** = stop building.
5. Right click, pick **Iron Bear Trap**: away from the copper trap it's red. Aim at the copper trap: it jumps onto it,
   green. Left click: the copper trap turns into an iron one, and 3 Iron are gone. Then pick **Gold Bear Trap**: it's
   red on a copper trap and green on the iron one; then **Diamond Bear Trap** on the gold one.
6. Make it night so monsters come: select the **Directional Light** and set **Time Of Day** to **0.8** in its
   DaylightCycle.
7. Stand behind a trap so the monster walks over it: the jaws snap, "CAUGHT!", a damage number. The monster stands
   still for the stun time while you hit it. A gold or diamond trap opens again 6 seconds after letting go and catches
   once more before it's used up.
8. Walk over a trap yourself: it snaps on you and your health drops by its damage (less with armor).
9. Without a monster: select a built trap (e.g. **Copper Bear Trap(Clone)**), **⋮** (top right of its BearTrap) >
   **Test: Snap Shut**, then **Test: Open**.

## Settings

| Where | Setting | What it does |
|---|---|---|
| `TrapBuilder.cs` > `Tiers` (top of the file) | name, color, metallic, smoothness, built on (the tier it upgrades, or null), cost, uses, damage, stun | the four traps; run Tools > Building > Build Traps after a change |
| each trap prefab > **BearTrap** | **Damage** | damage to a monster when it snaps shut (written by the tool) |
| | **Hold Seconds** | how long the monster stands still: the "Stun" in the info box (written by the tool) |
| | **Uses** | snaps before it's used up and goes away; 1 = one time use, 0 = never (written by the tool) |
| | **Rearm Seconds** (6) | with more than 1 use: how long it stays shut before opening again |
| | **Catch Radius** (0.35 m) | how close to the middle a monster's (or your) feet must come |
| | **Catch Flying** (off) | on = the beholder is caught too when it comes down onto it |
| | **Hurts Player** (on), **Player Damage** (= its damage, written by the tool), **Player Text** | stepping on it yourself |
| | **Closed Angle** (88), **Snap Seconds** (0.06), **Open Seconds** (0.6) | how far and how fast the jaws move |
| | **Caught Text**, **Caught Color**, **Broke Text** ("Used up") | the words that float up |
| | Front Jaw, Back Jaw, Hinge Axis, Front / Back Turn | filled by the tool |
| each trap's Building Data | **Display Name**, **Description** | the name, and the words in the info box |
| | **Recipe**, **Upgrade Of** | written by the tool from its table |
| | **Category** (Auto = Traps) | which build menu tab it's in |
| `Assets/Materials/Traps/<Tier> Bear Trap.mat` | **Albedo**, **Metallic**, **Smoothness** | its color and shine |
| Player > **BlueprintPlacer** | **Upgrade Reach** (1.2 m) | how close the crosshair must point to the placed trap to put an upgrade on it |

The info box's numbers come straight from BearTrap's settings, so change a number and the info box follows.

## How it works

| Script | On | What it does |
|---|---|---|
| **BearTrap** (`Assets/Script/Traps/BearTrap.cs`) | each trap | Each physics step, it checks the things in its catching box. A living, walking monster (Marko's EnemyCombat) with its feet within Catch Radius is caught: jaws snap shut, it's hurt and held. The player within Catch Radius is hurt (PlayerHealth.TakeDamage, so armor counts). After its last use it goes away. It also gives the build menu's info box its numbers (IBuildingInfo) |
| **EnemyStun** (`Assets/Script/Enemy/EnemyStun.cs`) | put on the monster when it's caught | Switches off Marko's EnemyMovement and EnemyAttack for the hold time (like the turtle's shell), keeps its sideways speed at 0 and keeps it on the trap's middle, then switches them back on. A monster that dies or burns away at dawn is simply let go. Marko's scripts aren't changed |
| **BuildingData** > **Upgrade Of** | each upgrade's Building Data | "built on a placed building of this kind, which it replaces". Any building can use it, not only traps |
| **BlueprintPlacer** | the Player | An upgrade's blueprint sits exactly on the placed building of its Upgrade Of kind nearest the crosshair (within Upgrade Reach); building it removes that one. With none near it's red and a click says why |
| **BuildingButton** | each button in the B menu | an upgrade's recipe starts with the trap it goes on ("on an Iron Bear Trap") and its picture |
| **Blueprint / BuildSnap** (Tony's building) | placing | Piece Type **Trap**: the ground under a trap doesn't count as "in the way" (so it can go on a slope), it can also sit on a built floor, and nothing snaps onto or next to a trap. Category Auto puts it in the Traps tab |
| **StandOnGround** | each trap | lies it flat on the ground when it's built |

## Problems

| Problem | Fix |
|---|---|
| No Bear Traps in the B menu | Run **Tools > Building > Build Traps**. The B menu lists every Building Data by itself; or run **Tools > Building > Update Build Menu List**. |
| No tabs or no info box in the B menu | Run **Tools > UI > Build Build Menu UI** once (Rebuild). |
| A trap button is grey | You're missing materials: the button says what's missing. You can still rest the mouse on it to read its info box. |
| An upgrade stays red: "Place it on a Copper Bear Trap" (or an Iron / a Gold one) | It's an upgrade: it only goes on the trap it names (Iron on Copper, Gold on Iron, Diamond on Gold). Build that one first and aim right at it (within about 1 m; BlueprintPlacer's **Upgrade Reach**). |
| It shows red: "Something is in the way" | A tree, rock, wall or other blueprint is in its spot. The ground itself never blocks a trap. Right after placing one, the next blueprint is on top of it: move the aim. |
| A monster walks over it and nothing happens | Its feet must come within Catch Radius (0.35 m) of the middle: make Catch Radius bigger, or place the trap on the monsters' path. Flying monsters fly over it (Catch Flying). |
| It snapped on me | That's on purpose (Hurts Player). Untick **Hurts Player** on BearTrap if you don't want it. |
| My recipe or damage change went back | The tool writes the four traps' numbers and recipes every run: change them in TrapBuilder.cs's `Tiers` table instead. |
| The jaws close the wrong way, into the ground | Run the tool again. It works out each jaw's turn; you can also swap the signs of Front Turn / Back Turn on BearTrap. |
| The trap floats or sinks | StandOnGround puts it on the ground when it's built: check the prefab has it (run the tool again). |

## By hand (what the tool does)

1. Cut the model into a base and two jaws in a 3D program (Blender), each jaw with its pivot on the hinge, and import
   them. Or use the tool: cutting the mesh by hand in Unity isn't possible.
2. Make an empty `Copper Bear Trap` (layer **Ignore Raycast**) with a **Box Collider** (Is Trigger, about 0.7 x 0.5 x
   0.55, Center Y 0.25), **Bear Trap** and **Stand On Ground** (Foot Offset 0).
3. Under it make `Model` with the base and the two jaws (each jaw's position = its hinge point) and a copper material.
   Drag the jaws into BearTrap's **Front Jaw** / **Back Jaw**, set **Hinge Axis** along the trap's long side, Front /
   Back Turn so each jaw turns up, and **Damage** 10, **Hold Seconds** 2, **Uses** 1, **Player Damage** 10.
4. Drag it into `Assets/Prefabs/Building/Traps` to make the prefab.
5. Project window > right click > **Create > Building > Building Data**, name it `Copper Bear Trap`. Set its Prefab,
   Piece Type **Trap**, a Description and the Recipe (4 Copper).
6. For each upgrade (e.g. Iron): copy the prefab, give it its material and numbers (20 / 3 / 1 / 20), and make its
   Building Data with Recipe 3 Iron and **Upgrade Of** = the `Copper Bear Trap` Building Data (Gold: Upgrade Of = Iron
   Bear Trap, Recipe 4 Gold; Diamond: Upgrade Of = Gold Bear Trap, Recipe 4 Diamond). They show in the B
   menu's Traps tab by themselves.
