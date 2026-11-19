# Held Item Setup (the selected item in the character's hand)

**What the player sees:** select wood, stone, food... in the hotbar and the character holds it in the right fist,
like a short stick through the hand (Minecraft-style): in first person on the hands, in third person on the body
(with a shadow). An empty slot = empty fists. Tools (pickaxe, axe, sword) are held by their own script,
**ToolInHand**, with a proper grip.

Before 19/11/2026 the item floated at the bottom right of the screen, where the first-person fists (drawn on top of
everything) hid it.

## Setup

Nothing to do: **HeldItemView** is already on the Player. It finds by itself:
- the body: the Player's **Character** (Tools > Player > Add Character Model);
- the first-person hands: **Main Camera > FirstPersonHands** (Tools > Player > Add First Person Hands).

A new player: drag **HeldItemView** onto it (next to PlayerInventory).

## How it works

- The item in the hand is a look-only copy (meshes and colors, no scripts or physics), photographed by IconCapture.
- Its longest side goes through the fist like a handle, sized so that side is **Size In Hand** long.
- Where the fist is comes from the hand's own bones (**HandGrip**, the same as a tool's grip), so it fits any Humanoid
  character.

## Settings (HeldItemView, on the Player)

| Setting | What it does |
|---|---|
| **Size In Hand** (0.2) | the item's longest side, in meters |
| **Item Nudge** | moves it in the fist, in meters: y toward the thumb, x along the fingers, z out of the palm |
| **Item Turn** | turns it in the fist (degrees) |

Change them **while playing** with an item in the hand, then right-click the component title > **Copy Component**,
stop playing, and right-click > **Paste Component Values** (changes made while playing are lost otherwise).

## Testing

1. Play, get some wood (punch a tree) and select its slot: the log is in the right fist.
2. Switch to third person: the body holds it too.
3. Select a tool: the tool shows instead (ToolInHand). Select an empty slot: empty fists.

## Problems

| Problem | Why / fix |
|---|---|
| Nothing in the hand | The Player needs a Humanoid **Character**, and the scene **FirstPersonHands** (the Console says if they're missing). |
| Too big or too small | **Size In Hand**. |
| It sticks out the wrong way | **Item Turn** (e.g. x 90). |
