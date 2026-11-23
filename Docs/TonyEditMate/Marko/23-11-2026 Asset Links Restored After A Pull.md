# Tony's fix after Marko's "new commit after pulling", 23/11/2026: 204 asset links restored

**What happened:** your commit `d723c2d` ("new commit after pulling", 23/11/2026) changed 204 `.meta` files. Every
one of them got a **new GUID** (the ID Unity links assets by) and default import settings. It looks like Unity made
fresh `.meta` files on your computer during the pull (this happens when Unity is open while git brings in new files:
Unity sees a new asset before its `.meta` arrives and makes its own), and those got committed.

Everything in the project links to those assets by their **old** GUIDs, so after your commit the links pointed at
nothing:
- the Gear page's armor slot pictures, the crafting list's tick / arrow badges, the perk pictures (all 20 Perk Data),
  the trophy and rays on the victory page, the Info tab's armor icon, the bow's draw ring (`Assets/UI/...`: also set
  from Sprite to Default, so they weren't sprites any more);
- the bows and arrows (GameLair's models: the Wooden Bow and Spruce Bow showed no model, so their icons were blank);
- the bow animations (the 3 Mixamo bow files lost their cut clips: BowDraw, BowAim, BowRelease, BowReady);
- the night 3 and night 4 monsters (Chest Monster, Beholder: their meshes, textures and animations);
- the loot box models and textures (DuNguyn), the armor colors (the 4 palettes in `Assets/Materials/Armor`);
- the four dragons (FourEvilDragonsHP: meshes, textures, animations).

No committed file used the new GUIDs, so nothing needed them.

## Files changed

| Files | What changed |
|---|---|
| The 204 `.meta` files your commit changed (`Assets/UI/Armor`, `UI/Perks`, `UI/Common`, `UI/GameEnd`, `UI/Stats`, `UI/Bow`, `Assets/Materials/Armor`, `Assets/Imported3D/GameLair`, `Mixamo`, `RPGMonsterPartnersPBRPolyart`, `DuNguyn`, `FourEvilDragonsHP`) | Put back exactly as they were in v2.22 (`git checkout e69ab4e -- <the files>`): old GUIDs and import settings. |

Nothing else of yours is changed: your `WeaponUpgrade`, `PlayerHunger` change, GameScene change and the new WebGL build
(`Webgl_New`) stay. **The WebGL build was made before this fix**, so it has the missing pictures and models: build it
again after pulling the fix.

## How to avoid it next time

1. **Close Unity before you pull** (or at least don't click on it until the pull has finished).
2. After a pull, if GitHub Desktop (or `git status`) shows changed `.meta` files you didn't change yourself, **don't
   commit them**: discard them (right click > Discard changes), then let Unity reload.
3. When you pull this fix: close Unity first. When you open it again it reimports those assets with their old GUIDs.
   (Your own work that isn't committed yet and links to one of those assets may need linking again.)

## How to undo

Not recommended (it breaks all the links again): `git checkout d723c2d -- <the 204 .meta files>`.
