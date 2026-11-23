# Human Enemies Plan

Tony's design for the **human enemies** (23/11/2026): survivors who live in the new world by the same rules as the
player. The player's goal is to survive a number of days; the humans gather resources, have gear and try to survive too.
They use Polytope Studio's modular armor and weapons, which the player's character can't wear. This page is the agreed
plan; `Docs/Human Enemies Setup.md` explains what's built so far and how to use it.

## The five types

| Type | Size | Lives | Normally | When the player hits it | Attacks the player first? |
|---|---|---|---|---|---|
| 1 | Normal (the player's size) | Alone | Works, never attacks first | Fights back; stops following beyond 10 m, but remembers and attacks again when the player comes within 8 m | No |
| 2 | Normal | Alone | Works, never attacks | Runs, unless its health is low and it judges it can't escape: then it fights | No |
| 3 | Small (0.75x) | Group of 3, rarely 2 | Works, never attacks | The whole group attacks, not afraid to die | No |
| 4 | Big (1.4x) | Alone | Works | Fights if it can't escape or the player is weaker, else runs | Yes, when its health is low and it judges the player is weaker |
| 5 | Normal | Group of 2 - 3 | Attacks the player | Fights to the death | Always |

**Every type:**

- is as big as the player's character (1.8 m, each one a little taller or shorter; sizes above are times that),
  walks as fast as the player, and chops trees, mines rocks and picks berries **for real**, like the player, keeping what it gets; its
  gear is given when it appears. Each one carries an axe and a pickaxe (the player's tier tools) and uses the axe to
  chop and the pickaxe to mine, every swing with the player's wind burst; the player's tool rules count (only a pickaxe mines ore, the spruce needs an iron axe or a war
  axe);
- always attacks night monsters on sight, and night monsters fight back;
- at night: 50 % walk off out of the player's sight and leave, 50 % stay and fight (a group decides once for everyone).

**On Hard** every type uses the fight-or-flee check: a type 1 may fight back when it would win, and the fearless
types 3 and 5 may run away when they'd clearly lose.

## Fight or flee (the math)

Each time it has to decide, a human compares:

- **Who would win:** the time it needs to kill the player (the player's health and armor against its damage) and the
  time the player needs to kill it (its health and armor against the player's weapon, perks and critical hits).
- **Can it get away:** its running speed against the player's (sprint, perks), and how close the player is.

It runs if it can get away. If it can't and its health is low, it fights, like a cornered animal.

## Gear

- **Weapons:** random from the Polytope set: bows and melee weapons (sword, mace, war axe, longsword, pike), sometimes
  with a shield.
- **Armor:** Polytope's modular pieces, tinted by tier: copper, iron, gold, diamond. Each piece rolls on its own, so
  one human can mix tiers.
- **By area** (where the resources are): the grassland is mostly copper and iron, better gear is rare but even a
  diamond set is possible; the hills copper and iron; the desert iron and gold; the snow gold and diamond.
- Gear gets better with each day, and with the difficulty.

Draft odds of a human's best tier (weights: none, copper, iron, gold, diamond):

| Area | None | Copper | Iron | Gold | Diamond |
|---|---|---|---|---|---|
| Grassland | 40 | 40 | 15 | 4 | 1 |
| Hills | 25 | 40 | 30 | 4 | 1 |
| Desert | 10 | 15 | 45 | 25 | 5 |
| Snow | 5 | 5 | 25 | 40 | 25 |

## Drops and coins

- **Materials:** each armor piece (helmet, chestplate, leggings, boots, gauntlets), the weapon, the shield and each
  tool (axe, pickaxe) drop 1 of their tier's material (a wooden tool Wood, a stone one Stone). A cape is for looks
  only.
- **Example:** a diamond helmet, chestplate and leggings with an iron sword drop 3 diamonds + 1 iron.
- **Fighting:** its weapon decides its damage (each kind its own number, a better tier +25 %), and its armor takes
  its share off the player's hits, with the player's armor numbers (a full set 10 / 15 / 25 / 40 %).
- **What it carried:** everything it gathered while working.
- It all goes **straight into the player's inventory**, like gathering (what doesn't fit falls next to the body).
- **Coins:** copper 1, iron 2, gold 3, diamond 4 for each piece, the weapon and the shield. Unarmored pieces give
  nothing; a human that would drop nothing at all gives 3 - 5 coins.
- The player's armor set bonus (coins per kill) and the Bounty Hunter perk count. The end pages show **Humans killed**
  apart from Monsters killed.
- A human killed by a night monster drops its loot on the ground, but gives no coins (the player didn't kill it). Only
  the player's kills pay.

## Where they live

The **new world only**:

| Area | Types |
|---|---|
| Grassland | 1, 2, 3 |
| Hills | 2, 3 |
| Desert | 1 - 5 |
| Snow | 1 - 5 |

New ones come in the daytime, out of the player's sight. At most about 8 - 10 near the player at once (the web build).

## Difficulty

Easy, Normal and Hard change their health, damage, how many come, the chance of better gear, and on Hard every type
uses the fight-or-flee check. They also get stronger each day.

Draft numbers: each day +10 % health and damage; Easy x0.75 health and damage and x0.6 as many; Hard x1.3 and x1.4.

| Type | Size | Health (day 1, Normal) | Damage | Speed |
|---|---|---|---|---|
| 1 | 1x | 80 | 10 | Runs away faster than the player walks |
| 2 | 1x | 100 | 12 | Normal |
| 3 | 0.75x | 60 each | 8 | Quick |
| 4 | 1.4x | 220 | 22 | Slow |
| 5 | 1x | 110 | 14 | Normal |

## Build order

Each step is playable on its own:

1. **Done (23/11/2026):** the builder (Polytope bodies, random gear and tint, weapons in the hand slot, Blink's
   animations), type 1 (the player's size, walks as fast as the player, really chops / mines / picks with its own
   axe and pickaxe, fights back when hit with attack animations by weapon and a grudge, dies, its loot and coins go
   into the player's inventory), the new world's Human Spawner, "Humans killed" on the end pages.
2. Melee fighting (built for type 1 on 23/11/2026: attacks by weapon, weapon damage, armor protection, and fighting
   like a player: facing, sprinting with stamina, timed swings that follow a player walking away, running attacks,
   circling, no stun-lock), the fight-or-flee math: types 2 and 4. Also built: punching without a tool, picking
   berries by hand, the caves' invisible trail (CaveTrail).
3. Groups: types 3 and 5.
4. Archers and enemy arrows.
5. Night: humans and night monsters fighting both ways, the 50 / 50 leave-or-stay roll, and only the player's kills
   paying.
6. The hills / desert / snow type lists, gear odds by day and difficulty, the Hard rule, balancing.
