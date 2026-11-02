# Notifications Setup ("+3 Wood" messages)

**What the player sees:** short messages on the side of the screen telling them what they just got, e.g.
**"+3 Wood"** after harvesting a tree, or **"+2 Coins"** after defeating an enemy. Each message shows for
3 seconds, then fades away. The newest is at the bottom.

- The item's picture is shown in front of the text (the same picture as in the inventory).
- The same thing again while its message is still showing adds up: "+3 Wood", then 2 more Wood = **"+5 Wood"**.
- Every way items come in counts: harvesting, picking up (F or walking over), crafting.
  Items only moving around inside the inventory don't.
- Spending coins shows nothing.

## Quick way: one click

1. Open the scene (e.g. `Assets/Scenes/GameScene.unity`).
2. Menu **Tools > UI > Build Notifications**.
   - It saves `Assets/Prefabs/UI/Notifications/NotificationCanvas.prefab` and puts a NotificationCanvas in the scene.
   - If the prefab or a NotificationCanvas in the scene already exists, it asks first. **Ctrl + Z** brings the old one back.
   - If it asks for TMP Essentials: **Window > TextMeshPro > Import TMP Essential Resources**, Import, then try again.
3. Save the scene (**Ctrl + S**).

```
NotificationCanvas      Canvas (Sort Order 5) + GainNotifier
  Feed                  Vertical Layout Group + NotificationFeed: where the messages stack
    Message             switched OFF: the look every message is copied from
      Icon              Image (the item's picture)
      Text              TextMeshPro ("+3 Wood")
```

## Turn it on / off, left / right (GameRules)

Select the **GameRules** object in the scene. Under **Notifications**:

| Rule | What it does |
|---|---|
| **Show Notifications** | Ticked (default) = the messages show. Unticked = no messages. |
| **Notification Side** | **Left** (default) or **Right**: which side of the screen they show on. Only shows while Show Notifications is ticked. |

Both work while playing too: change them and the messages move (or disappear) straight away.

## Settings

**Feed > NotificationFeed**

| Field | What it does |
|---|---|
| **Show Seconds** | How long a message stays before it fades (3). |
| **Fade Seconds** | How long the fading takes (0.6). 0 = it just disappears. |
| **Max Messages** | Most messages on screen at once (5). One more and the oldest goes. |
| **Gain Format** | How a gain is written. `{0}` = the amount, `{1}` = the name. `+{0} {1}` = "+3 Wood", `+ {0} {1}` = "+ 3 Wood". |
| Template | The switched-off **Message** (filled in by the tool; empty = the child named "Message"). |

**NotificationCanvas > GainNotifier**

| Field | What it does |
|---|---|
| **Items** | Ticked = "+3 Wood" when items come in. |
| **Coins** | Ticked = "+2 Coins" when the player gets coins. |
| **Coin Name** | The word after the coin amount ("Coins"). |
| **Coin Icon** | The coin picture. Empty = the same coin as the HUD's coin counter (top right). |

**Where the messages show:** select **Feed** and move / resize it (it starts 30 px from the left edge and 340 px
from the bottom). **Notification Side = Right** flips it to the same distance from the right edge.
**The look:** switch **Message** on, change its box color, text size or font, switch it off again, then
**Overrides > Apply All** to save it into the prefab.

## Show your own message (for everyone's scripts)

No finding needed, call it from any script:

```csharp
NotificationFeed.Show("Night is coming!");                     // a plain message
NotificationFeed.Show("Furnace built", furnaceSprite);         // with a picture
NotificationFeed.ShowGain("Coins", "Coins", 5, coinSprite);    // "+5 Coins", adds up with other "Coins" gains
```

Items given with `PlayerInventory.Instance.Add(...)` and coins given with `PlayerCoins.Instance.AddCoins(...)`
already show a message by themselves, nothing to add. Nothing shows while **Show Notifications** is off.

## By hand (instead of the tool)

1. Hierarchy > right click > **UI > Canvas**, name it **NotificationCanvas**. Canvas: **Sort Order 5**.
   Canvas Scaler: **Scale With Screen Size**, 1920 x 1080, Match **0.5**.
   Add Component > **GainNotifier**.
2. Right click NotificationCanvas > **Create Empty**, name it **Feed**.
   - Anchor Presets: **bottom left** (hold **Alt + Shift** while clicking it). Pos X **30**, Pos Y **340**,
     Width **600**, Height **380**.
   - Add Component > **Vertical Layout Group**: Child Alignment **Lower Left**, Spacing **6**,
     Control Child Size **Width** and **Height** ticked, Child Force Expand both **unticked**.
   - Add Component > **NotificationFeed**.
3. Right click Feed > **UI > Image**, name it **Message**. Color black, Alpha about **140**.
   - Add Component > **Horizontal Layout Group**: Padding Left 12, Right 18, Top 6, Bottom 6, Spacing 10,
     Child Alignment **Middle Left**, Control Child Size both ticked, Child Force Expand both unticked.
   - Add Component > **Canvas Group** (untick Interactable and Blocks Raycasts).
   - Untick **Raycast Target** on the Image.
4. Inside Message:
   - **Icon**: right click > **UI > Image**. Tick **Preserve Aspect**, untick Raycast Target.
     Add Component > **Layout Element**: Preferred Width **40**, Preferred Height **40**.
   - **Text**: right click > **UI > Text - TextMeshPro**. Text "+3 Wood", size **30**, white, bold,
     Wrapping **Disabled**.
5. Switch **Message** off (the tick box at the top of its Inspector). NotificationFeed copies it for every message.

The names matter: **Message** (the template), and its children **Icon** and **Text**.

## Test

1. Play and harvest a tree (or pick up any item): **"+3 Wood"** shows on the left and fades away.
2. Harvest again quickly: the same line grows ("+6 Wood") instead of a new one.
3. Defeat an enemy: **"+2 Coins"** with the coin picture.
4. While playing, set GameRules > **Notification Side** to **Right**: the messages jump to the right.
   Untick **Show Notifications**: they disappear.

| Problem | Fix |
|---|---|
| No messages at all | The scene needs a **NotificationCanvas** (Tools > UI > Build Notifications), and GameRules > **Show Notifications** must be ticked. The Console says when there's no feed. |
| Items show, coins don't | The player needs Marko's **PlayerCoins**; GainNotifier > **Coins** must be ticked. |
| No picture in the message | The item has no icon, or GameRules' **Item Icon Style** is **Label** (text only). For coins: set **Coin Icon**, or build the HUD (its coin counter gives the picture). |
| A message shows twice | Only one **GainNotifier** in the scene. |
