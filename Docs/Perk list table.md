# Perk list table

Every perk in the game, what it gives, and which loot box offers it in each chest system (picked on the New Game page:
TESTING > **Old chests** / **New chests**, New is the default). Numbers as of 21/11/2026; each one is the **Value** on
the perk's Perk Data in `Assets/Config/Perks`, so change it there (and here). How the boxes work: `Docs/Loot Chests Setup.md`.

**Attribute words:**
- **Stackable**: you can get it again and again; each pick adds the amount once more.
- **Always active (one at a time)**: an ability that is always on. A box won't offer it again while you have it.
- **Use once**: it works one time, then it's gone (and a box can offer it again).
- **Night only**: it only works at night.

## All perks

| Perk | Attribute | Unlocked after | Old system | New system |
|---|---|---|---|---|
| Vitality | +20% max health, **stackable** | - | Common | Common, Rare |
| Stamina | +25 max stamina, **stackable** | - | Common | Common, Rare |
| Agility | +15% move speed, **stackable** | - | Common | Common, Rare |
| Fireball | 1st pick: unlocks the fireball (Q with a sword in hand); each more pick: cooldown -20% (3 s, down to 1 s), **stackable** | - | Common | Common, Rare |
| Second Wind | +30% stamina recharge, **stackable** | - | Common | Common, Rare |
| Precision | +1.25% critical strike chance, **stackable** | Critical Strike | Common | Common, Rare |
| Spring Legs | +25% jump height, **stackable** | Glide | Common | Common, Rare |
| Bounty Hunter | +1 coin for every monster killed, **stackable** | - | Rare | Common, Rare |
| Gourmet | +10% from food (bad food doesn't get worse), **stackable** | - | Rare | Common, Rare |
| Lumberjack | +1 wood from every tree, **stackable** | - | Rare | Common, Rare |
| Prospector | +1 stone or ore from every rock, **stackable** | - | Rare | Common, Rare |
| Sprinter | +15% sprint speed and -25% sprint stamina, **stackable** | - | Rare | Common, Rare |
| Brawler | +10% punch knockback, **stackable** | - | Rare | Common, Rare |
| Attack Speed | +10% attack speed (shorter wait between attacks), **stackable** | - | Rare | Common, Rare |
| Long Reach | +10% melee reach (2.5 m -> 2.75 m), **stackable** | Keen Eye | Rare | Common, Rare |
| Last Stand | a killing blow leaves you at 1 health, safe for 2 s, **use once** (then it can be found again) | - | Epic | Epic, Legendary |
| Critical Strike | 5% of your hits do double damage (a red burst), **always active (one at a time)** | - | Epic | Epic, Legendary |
| Glide | hold Space in the air to float down, tap it twice to dive, **always active (one at a time)** | - | Epic | Epic, Legendary |
| Vampire | heal 5% of the damage you deal to monsters and animals, **stackable** | - | Epic | Epic, Legendary |
| Thorns | a monster that hits you takes 10% of that hit back, **stackable** | - | Epic | Epic, Legendary |
| Night Power | +10% max health, max stamina, speed and melee damage (not the fireball), **stackable**, **night only** | - | Epic | Epic, Legendary |
| Reroll | unlocks the REROLL button at loot boxes, **always active (one at a time)** | - | Epic | Epic, Legendary |
| Keen Eye | the crosshair turns yellow when your attack will hit what you aim at, **always active (one at a time)** | - | Epic | Epic, Legendary |

23 perks: Vitality, Stamina, Agility and Fireball are Marko's; the other 19 come with the loot boxes.

## The boxes

| Box | Where | Price | Old system | New system |
|---|---|---|---|---|
| Common | grassland (20), desert (30) | 2, then +1 each | choose 1 of 3 from its 7 perks | **one random perk** from the 15 common + rare perks |
| Rare | grassland (6), desert (12) | 10, then x1.5 each | choose 1 of 3 from its 8 perks | choose 1 of 3 from the same 15 |
| Epic | desert only (4) | 100, then x2 each | choose 1 of 3 from its 8 perks, REROLL | **one random perk** from its 8 perks |
| Legendary | desert only (2) | 150, then x2 each | not in the old system | choose 1 of 3 from the epic perks |

- **REROLL** only works once you have the **Reroll** perk. Then: old system = the epic box only, new system = every box.
  It costs a tenth of the box's price, and twice as much each time at the same box.
- **Unlocked after:** such a perk only shows up once you have the perk named there.
- **When you die** every perk is lost, and every box's price starts again.
- **The whole gold armor set** makes every box 10 % cheaper (rounded down: 10 -> 9, 100 -> 90).
