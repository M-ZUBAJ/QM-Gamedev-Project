# Perk Panel Setup (the chest's "CHOOSE YOUR REWARD" panel)

**What the player sees:**
- **Looking at a chest** from close by (where E works): a yellow outline, and under the crosshair
  **[E] Open chest (coin) 2**. The price is red while you have fewer coins than that.
- **E:** the panel opens. Three perk cards (icon, name, what it does), each with a **CHOOSE** button; at the top the
  chest's price and your coins. Looking is free.
- **CHOOSE:** now the coins are paid, the perk is given and shows in the HUD's perk row (top middle), and the chest
  disappears.
- **Not enough coins:** the CHOOSE buttons are faded, say **NEED 2 COINS** and can't be clicked. A red line says how
  many more coins you need.
- **Closing without choosing** (nothing is paid, the chest stays):
  - **E again**, wherever you look;
  - **walking away**, farther than the chest's reach (**Interaction Range** 3 m, plus 0.5 m);
  - **dying** or **winning the game** (every menu closes).
- **Opening it again:** walk back, look at it, E: the same 3 perks. Every chest has its own 3 random perks.
- E while not looking at a chest does nothing to chests (it still picks berries).

## Quick way: one click

1. Open `Assets/Scenes/GameScene.unity`.
2. Menu **Tools > UI > Build Perk Panel**. It:
   - draws the pictures (only if the files aren't there yet): the coin, the round box and the glow in
     `Assets/UI/Common`, the four perk icons in `Assets/UI/Perks` (heart = Max Health, lightning = Max Stamina,
     double arrows = Move Speed, flame = Fireball);
   - gives each Perk Data in `Assets/Config/Perks` without an icon the matching one;
   - saves `Assets/Prefabs/UI/Perks/PerkSelectCanvas.prefab` and puts it in the scene **instead of** Marko's old
     PerkSelectCanvas (there can only be one; it asks first; **Ctrl + Z** brings the old one back).
3. Menu **Tools > UI > Build HUD**, for the prompt under the crosshair and the new coin counter (`Docs/HUD Setup.md`).
4. Save the scene (**Ctrl + S**).

```
PerkSelectCanvas              Marko's PerkSelectionUI + Tony's PerkPanelInfo (Sort Order 20)
  PerkSelectionPanel          the dark backdrop (Marko's script switches it on and off)
    Window
      Title                   CHOOSE YOUR REWARD
      Line
      PriceRow                Price (coin) 2 coins      You have (coin) 20
      Cards > PerkOption1..3  Glow, Icon, PerkName, PerkDescription, ChooseButton > Label (ButtonFX)
      NeedText                red, only while you can't pay
      Hint                    E: close, or walk away
```

## How it works

- Marko's **PerkChest** (on each chest): E opens and closes the panel, walking away closes it, the 3 perks are rolled
  the first time and kept, and the coins are taken when a perk is picked (Tony's `[Tony]` lines; log
  `Docs/TonyEditMate/Marko/19-11-2026 Perk Panel, E Toggle And Pay On Pick.md`).
- Marko's **PerkSelectionUI** (unchanged) fills the cards and calls the chest back when a CHOOSE button is clicked.
- Tony's **PerkPanelInfo** writes the price and your coins, fades out the CHOOSE buttons while you can't pay, and
  makes the window pop in.
- Marko's **PerkManager** gives the perk; a `[Tony]` line also puts it in the HUD's perk row (**PlayerPerks**).
- Tony's **ChestPrompt** (in the HUD) shows the key and the price while you look at a chest.

## By hand (instead of the tool)

1. Hierarchy > right click > **UI > Canvas**, name it **PerkSelectCanvas**, **Sort Order 20**. Canvas Scaler:
   **Scale With Screen Size**, 1920 x 1080, Match 0.5.
2. Child **PerkSelectionPanel**: UI > Image stretched over the whole screen, black, Alpha about 140. Switch it off.
3. In it **Window**: UI > Image, 1260 x 740 in the middle, dark. In the Window:
   - **Title** (TextMeshPro "CHOOSE YOUR REWARD") and a **PriceRow** (Horizontal Layout Group) with the texts
     PriceLabel, PriceText, HaveLabel, CoinsText and two coin Images;
   - **Cards** (Horizontal Layout Group, 1170 x 420) with **PerkOption1**, **PerkOption2**, **PerkOption3**: each an
     Image with **Icon** (Image), **PerkName** and **PerkDescription** (TextMeshPro) and **ChooseButton** (Image +
     Button + **ButtonFX**, with a TextMeshPro child named **Label**);
   - **NeedText** (red, switched off) and **Hint**.
4. On PerkSelectCanvas, Add Component > **PerkSelectionUI** (Marko's): Selection Panel = PerkSelectionPanel; Perk
   Buttons = the three ChooseButtons; Perk Icons, Perk Names, Perk Descriptions = the three of each.
5. Add Component > **PerkPanelInfo**: Price Text, Coins Text, Need Text, Hint Text, Choose Buttons (the three
   ChooseButtons), Choose Labels (their Labels), Window.
6. Delete the old PerkSelectCanvas (only one may exist). For icons, drag a picture into each Perk Data's **Icon**
   (`Assets/Config/Perks`).

## Settings

| On | Setting | What it does |
|---|---|---|
| PerkChest (each chest) | **Min / Max Cost** | its price, random between the two (rolled when the game starts) |
| | **Interaction Range** (3) | how close E works; the panel closes 0.5 m farther away |
| | **Interact Key** (E) | opens and closes the panel (the prompt and the hint show this key) |
| | **Available Perks** | the perks it can roll (at least 3) |
| PerkPanelInfo | **Choose Text** | the CHOOSE buttons' text |
| | **Can Pay / Can't Pay Color** | the price's color |
| | **Pop Seconds** | how long the window's pop-in takes (0 = no pop) |
| Perk Data (`Assets/Config/Perks`) | **Display Name**, **Description**, **Icon** | what a card shows |
| ChestPrompt (in the HUD) | **Open Text**, the colors, **Fade Seconds** | the prompt under the crosshair |

## Testing

1. Play (from MainTitleScene or GameScene). Look at a chest from close by: yellow outline and "E Open chest (coin) 2".
2. **E**: the panel opens with the price and your coins. **E** again: it closes, and your coins didn't change.
3. **E** again: the same three perks. Walk backwards (**S**): it closes at about 3.5 m.
4. Open another chest: different perks.
5. Click **CHOOSE**: the coins go down (the coin counter pops), the perk shows in the HUD's perk row, the chest is gone.
6. With fewer coins than the price: the CHOOSE buttons are faded and say what's needed.
7. With the panel open, select GameEndCanvas > VictoryScreen > **⋮** > **Test: Win**: the panel closes.

## Problems

| Problem | Why / fix |
|---|---|
| "More than one PerkSelectionUI exists!" | Two perk panels are in the scene: delete the old one (the tool does it). |
| The cards show white squares | The Perk Data have no Icon: run the tool (it gives the drawn ones), or drag your own pictures in. |
| E does nothing at a chest | Look straight at it from closer than Interaction Range. In third person the camera is too far back. An open menu (inventory, building) blocks it too. |
| A picked perk doesn't show in the HUD's perk row | The Player needs **PlayerPerks** (Tools > UI > Build HUD adds it). |
| No prompt under the crosshair | Run **Tools > UI > Build HUD** (it adds HUDCanvas > ChestPrompt). |
