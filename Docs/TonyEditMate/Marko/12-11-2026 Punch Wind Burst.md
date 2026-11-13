# Tony's edit to Marko's code, 12/11/2026: punch wind burst, and punches land with the fist

Every changed line is marked `// [Tony]` in the file, and each line of yours that was replaced is kept in a comment
right above its new version.

**Why:** Tony asked for:
1. A grey "wind explosion" where a bare-handed punch hits an enemy, or anything else with health.
2. The burst on a deer even while it runs. It used to appear in the air where the deer had been.
3. Trees and stones staying until the last punch's burst. They used to vanish the moment you clicked, a third of a
   second before the fist landed and the burst appeared.

Only `PlayerCombat.Attack()` knows when a punch hits something and where, so the change is there.

---

## `Assets/Script/Player/PlayerCombat.cs`

**1. One `punching` flag.** In Tony's punch-animation lines (10/11/2026), the check "not the Sword, Axe or PickAxe" is
stored once, `bool punching = ...`, and used for the animation and for the two hits below. Nothing else changed there.

**2. A punch hurts when the fist lands.** Your two damage lines are replaced, only for bare-handed punches:

```csharp
// [Tony] replaced (12/11/2026): enemy.TakeDamage(damage);
if (punching) PunchEffect.Punch(enemy, hit, () => enemy.TakeDamage(damage));          // [Tony] replaced
else enemy.TakeDamage(damage);                                                        // [Tony] replaced

// [Tony] replaced (12/11/2026): resource.TakeDamage(damage);
if (punching) PunchEffect.Punch(resource, hit, () => resource.TakeDamage(damage));    // [Tony] replaced
else resource.TakeDamage(damage);                                                     // [Tony] replaced
```

**What `PunchEffect.Punch` does:**
1. **Waits until the fist lands.** Tony's PunchEffect on the Player waits its **Impact Delay**, measured from the punch
   animation (about a quarter of a second). Your attack cooldown (0.5 s) is longer, so punches never overlap.
2. **Then hurts the target** with your `damage`, through your `TakeDamage`. A tree or stone this punch breaks breaks now,
   together with the burst. The enemy's flinch, coins and "+3 Wood" come at the same moment.
3. **Bursts the wind** on the spot that was hit. The spot is remembered on the target, so a deer or slime that moved
   still gets the burst on its body.
4. **If the target is gone by then** (e.g. killed by something else), it isn't hurt again. The burst still shows where
   it was.

**Good to know:**
- **What's unchanged:** your damage numbers, the ray, the stamina and the cooldown. Only *when* a bare-handed punch
  hurts changed.
- **Weapons still hit at once:** the Sword, Axe and PickAxe have no swing animation yet. Once they do, the same
  `PunchEffect.Punch` (or a copy of it) can time them too.
- **Without Tony's PunchEffect** on the Player, `Punch` hurts at once, exactly like before.

**3. The first version's burst block is gone.** The earlier `[Tony]` block after the damage code (it called
`PunchEffect.Play(hit.point, hit.normal)`) was Tony's own, not yours. `Punch` does its job now.

**To undo everything:**
1. Put back your two lines from the `[Tony] replaced` comments: `enemy.TakeDamage(damage);` and
   `resource.TakeDamage(damage);`.
2. Delete the lines marked `[Tony] replaced` and their comments.
3. Change `if (punching)` back to `if (inHand != "Sword" && inHand != "Axe" && inHand != "PickAxe")`.
4. Delete the `bool punching` line.

---

## For Marko: a burst for your other hits

`PunchEffect.Play(point, normal)` makes only the burst, from any script, e.g. for sword hits or the fireball:

```csharp
PunchEffect.Play(hit.point, hit.normal);   // normal = the way out of the surface (RaycastHit.normal)
```

It always shows the grey wind burst. For a different look (sparks for the sword), copy
`Assets/Prefabs/Effects/PunchWindBurst.prefab` and ask Tony to add a second slot.
