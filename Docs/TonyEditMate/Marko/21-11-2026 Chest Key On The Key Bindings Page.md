# Tony's change to Marko's PerkChest, 21/11/2026: the chest key comes from KeyBindings

**Why:** Tony asked for the title screen's **Settings > Key Bindings** page to show the key that opens chests. Your
chest had its own `interactKey` field (E), so the page couldn't show it or change it: the page only knew Grab (F,
picking up items from the ground), while players open chests (and pick berries) with E. Now the chests read
`KeyBindings.Interact`, which is **E** by default, listed on the page as **Open chest / Pick berries**. Changing it there
changes it for every chest (and Turki's berry bushes, which use the same key).

## File changed

| File (yours) | What changed |
|---|---|
| `Assets/Script/Perks/PerkChest.cs` | (1) Your field `[SerializeField] KeyCode interactKey = KeyCode.E;` is commented out (`[Tony] replaced`), so it no longer shows in the Inspector. (2) Tony's getter `InteractKey` (added 19/11/2026) now returns `KeyBindings.Interact` (the old line is kept in a comment). (3) Your two `Input.GetKeyDown(interactKey)` checks (opening the chest, and closing its panel) use `Input.GetKeyDown(InteractKey)` instead (`[Tony] replaced`, the originals in comments above them). |

Everything that showed the chest's key follows by itself: Tony's ChestPrompt under the crosshair ("[E] Open chest"),
the perk panel's hint and the roll pop-up's hint and skip key all read `InteractKey`. The chest prefabs still have the
old `interactKey` value saved in them; Unity ignores it.

## How to undo

Put your field back (remove the `//` in front of it), and in the three places marked `[Tony] replaced` / `[Tony] edited`
put back the commented original line and delete the new one. Then also take the "Open chest / Pick berries" row out of
Tony's `KeyBindingMenu.cs` (Rows table), or it would change a key nothing reads.
