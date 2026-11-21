/*
First Date: 19/11/2026
Date: 21/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Not used directly. Tools > UI > Build HUD and Tools > UI > Build Perk Panel use it for their pictures.
              A picture is only drawn if its file isn't there yet, so your own art saved under the same name is kept
              (delete a file to get the drawn one back).
Purpose: Draws the small game pictures in code and saves them as PNG sprites, so nothing has to be drawn by hand:
           Assets/UI/Common/Coin.png          a gold coin (the HUD's coins, a chest's price)
           Assets/UI/Common/RoundedBox.png    a white box with round corners (9-sliced: any size, tinted any color)
           Assets/UI/Common/Glow.png          a soft round glow (behind the perk icons)
           Assets/UI/Perks/PerkHealth.png     a red heart          (Max Health)
           Assets/UI/Perks/PerkStamina.png    a yellow lightning   (Max Stamina)
           Assets/UI/Perks/PerkSpeed.png      blue double arrows   (Move Speed)
           Assets/UI/Perks/PerkFireball.png   an orange flame      (Fireball)
           Assets/UI/Perks/Perk<Name>.png     the loot boxes' new perks: wind lines (Second Wind), a red burst star
                                              (Critical Strike), a target (Precision), an arrow on a spring (Spring
                                              Legs), a coin and a plus (Bounty Hunter), a drumstick with a sparkle
                                              (Gourmet), a log (Lumberjack), a rock with ore (Prospector), a running
                                              boot (Sprinter), a fist (Brawler), a gold shield with a heart (Last
                                              Stand), a feather (Glide), a sword with swing lines (Attack Speed), a
                                              blood drop with a green plus (Vampire), a thorny green ball (Thorns),
                                              a crescent moon with a star (Night Power), two gold arrows round a die
                                              (Reroll), an eye in a yellow ring (Keen Eye), a spear with a green
                                              plus (Long Reach)
           Assets/UI/Stats/Hunger.png         a drumstick          (the inventory's Info tab: hunger)
           Assets/UI/Stats/Jump.png           a green up arrow     (the inventory's Info tab: jump height)
           Assets/UI/Stats/Armor.png          a steel breastplate  (the inventory's Info tab: armor)
           Assets/UI/Armor/<Slot>.png         a white outline of each armor piece (helmet, shoulders, chestplate, gloves,
                                              leggings, boots): shown faintly in an empty slot on the Gear page
         Each pixel is the average of 4 x 4 points, so the edges come out smooth, not jagged.
         GivePerkIcons puts the perk pictures in the player's PlayerPerks (its Icons list), for the HUD's perk icons
         and the Info tab. ForPerk gives each perk kind (Marko's ChestPerkType) its picture.
Requirement: IconArt.cs x-> UIBuildHelper.cs, UIFXSprites.cs, PlayerPerks.cs, ArmorStats.cs, PerkData.cs
             IconArt.cs <-x CraftingMenuBuilder.cs, GearPageBuilder.cs, HUDBuilder.cs, InventoryUIBuilder.cs, LootChestBuilder.cs, PerkPanelBuilder.cs, PerkRollBuilder.cs
*/

using System.IO;
using UnityEditor;
using UnityEngine;
using static UIBuildHelper;   // MakeFolders, FolderOf

public static class IconArt
{
    // ===== Fields =====
    public const string CommonFolder = "Assets/UI/Common";
    public const string PerkFolder = "Assets/UI/Perks";
    public const string StatFolder = "Assets/UI/Stats";
    public const string ArmorFolder = "Assets/UI/Armor";

    const int IconSize = 128;    // pixels: sharp up to about 128 pixels on screen
    const int BoxSize = 64;      // the round box is small: 9-slicing stretches it to any size
    const int BoxRadius = 20;    // its corners, in pixels
    const int Samples = 4;       // 4 x 4 points per pixel

    static readonly Color Shadow = new Color(0f, 0f, 0f, 0.35f);

    // The shapes, with x and y from -1 to 1 (y up).
    static readonly Vector2[] Bolt =
    {
        new Vector2(0.16f, 0.84f), new Vector2(-0.46f, -0.04f), new Vector2(-0.05f, -0.04f), new Vector2(-0.21f, -0.84f),
        new Vector2(0.49f, 0.11f), new Vector2(0.07f, 0.11f), new Vector2(0.32f, 0.84f),
    };
    static readonly Vector2[] ChevronBack = Chevron(-0.24f);
    static readonly Vector2[] ChevronFront = Chevron(0.26f);
    static readonly Vector2[] FlameTip = { new Vector2(-0.5f, -0.22f), new Vector2(0.5f, -0.22f), new Vector2(0.06f, 0.92f) };
    static readonly Vector2[] FlameCoreTip = { new Vector2(-0.27f, -0.38f), new Vector2(0.27f, -0.38f), new Vector2(0.03f, 0.36f) };
    static readonly Vector2[] Breastplate =
    {
        new Vector2(-0.72f, 0.5f), new Vector2(-0.34f, 0.72f), new Vector2(-0.16f, 0.5f), new Vector2(0.16f, 0.5f),
        new Vector2(0.34f, 0.72f), new Vector2(0.72f, 0.5f), new Vector2(0.56f, 0.1f), new Vector2(0.5f, -0.72f),
        new Vector2(-0.5f, -0.72f), new Vector2(-0.56f, 0.1f),
    };
    static readonly Vector2[] Palm = { new Vector2(-0.36f, -0.7f), new Vector2(0.36f, -0.7f), new Vector2(0.38f, 0.12f), new Vector2(-0.38f, 0.12f) };
    static readonly Vector2[] Cuff = { new Vector2(-0.44f, -0.92f), new Vector2(0.44f, -0.92f), new Vector2(0.4f, -0.62f), new Vector2(-0.4f, -0.62f) };
    static readonly Vector2[] Trousers =
    {
        new Vector2(-0.52f, 0.72f), new Vector2(0.52f, 0.72f), new Vector2(0.56f, -0.82f), new Vector2(0.14f, -0.82f),
        new Vector2(0f, 0.08f), new Vector2(-0.14f, -0.82f), new Vector2(-0.56f, -0.82f),
    };
    static readonly Vector2[] Boot =
    {
        new Vector2(-0.42f, 0.76f), new Vector2(0.12f, 0.76f), new Vector2(0.14f, -0.22f), new Vector2(0.56f, -0.34f),
        new Vector2(0.72f, -0.54f), new Vector2(0.7f, -0.78f), new Vector2(-0.44f, -0.78f),
    };
    static readonly Vector2[] UpArrow =
    {
        new Vector2(0f, 0.86f), new Vector2(0.62f, 0.18f), new Vector2(0.24f, 0.18f), new Vector2(0.24f, -0.82f),
        new Vector2(-0.24f, -0.82f), new Vector2(-0.24f, 0.18f), new Vector2(-0.62f, 0.18f),
    };
    static readonly Vector2[] Star = StarPoints(8, 0.9f, 0.42f);       // a burst with 8 points
    static readonly Vector2[] Sparkle = StarPoints(4, 0.3f, 0.09f);    // a small 4-point twinkle
    static readonly Vector2[] SpringHead = { new Vector2(0f, 0.92f), new Vector2(0.5f, 0.4f), new Vector2(-0.5f, 0.4f) };
    static readonly Vector2[] Rock =
    {
        new Vector2(-0.78f, -0.58f), new Vector2(-0.6f, 0.18f), new Vector2(-0.22f, 0.6f), new Vector2(0.34f, 0.54f),
        new Vector2(0.78f, 0.08f), new Vector2(0.66f, -0.58f),
    };
    static readonly Vector2[] Blade =   // a sword blade from the hilt (down left) to its point (up right)
    {
        new Vector2(-0.378f, -0.222f), new Vector2(0.561f, 0.716f), new Vector2(0.78f, 0.78f), new Vector2(0.716f, 0.561f),
        new Vector2(-0.222f, -0.378f),
    };
    static readonly Vector2[] DropTip = { new Vector2(-0.47f, -0.05f), new Vector2(0.47f, -0.05f), new Vector2(0f, 0.9f) };
    static readonly Vector2[] ThornBall = StarPoints(12, 0.9f, 0.56f);   // a ball with 12 thorns
    static readonly Vector2[] Shield =
    {
        new Vector2(-0.66f, 0.74f), new Vector2(0.66f, 0.74f), new Vector2(0.62f, 0.02f), new Vector2(0f, -0.88f),
        new Vector2(-0.62f, 0.02f),
    };
    static readonly Vector2[] RerollHeadTop = ArrowHead(150f);      // the top arrow's head (it turns to the left)
    static readonly Vector2[] RerollHeadBottom = ArrowHead(330f);   // the bottom arrow's head (to the right)

    // ===== My methods =====

    public static Sprite Coin() => Save(CommonFolder + "/Coin.png", () => Draw(IconSize, CoinPaint), Vector4.zero);
    public static Sprite RoundedBox() => Save(CommonFolder + "/RoundedBox.png", () => Draw(BoxSize, BoxPaint), Vector4.one * (BoxRadius + 4));
    public static Sprite Glow() => Save(CommonFolder + "/Glow.png", () => UIFXSprites.Draw(UIFXSprites.Shape.SoftDot), Vector4.zero);
    public static Sprite PerkHealth() => Save(PerkFolder + "/PerkHealth.png", () => Draw(IconSize, HeartPaint), Vector4.zero);
    public static Sprite PerkStamina() => Save(PerkFolder + "/PerkStamina.png", () => Draw(IconSize, BoltPaint), Vector4.zero);
    public static Sprite PerkSpeed() => Save(PerkFolder + "/PerkSpeed.png", () => Draw(IconSize, SpeedPaint), Vector4.zero);
    public static Sprite PerkFireball() => Save(PerkFolder + "/PerkFireball.png", () => Draw(IconSize, FlamePaint), Vector4.zero);
    public static Sprite StatHunger() => Save(StatFolder + "/Hunger.png", () => Draw(IconSize, HungerPaint), Vector4.zero);
    public static Sprite StatJump() => Save(StatFolder + "/Jump.png", () => Draw(IconSize, JumpPaint), Vector4.zero);
    public static Sprite StatArmor() => Save(StatFolder + "/Armor.png", () => Draw(IconSize, ArmorPaint), Vector4.zero);
    public static Sprite PerkSecondWind() => Save(PerkFolder + "/PerkSecondWind.png", () => Draw(IconSize, WindPaint), Vector4.zero);
    public static Sprite PerkCritical() => Save(PerkFolder + "/PerkCritical.png", () => Draw(IconSize, StarPaint), Vector4.zero);
    public static Sprite PerkPrecision() => Save(PerkFolder + "/PerkPrecision.png", () => Draw(IconSize, TargetPaint), Vector4.zero);
    public static Sprite PerkSpringLegs() => Save(PerkFolder + "/PerkSpringLegs.png", () => Draw(IconSize, SpringPaint), Vector4.zero);
    public static Sprite PerkBountyHunter() => Save(PerkFolder + "/PerkBountyHunter.png", () => Draw(IconSize, BountyPaint), Vector4.zero);
    public static Sprite PerkGourmet() => Save(PerkFolder + "/PerkGourmet.png", () => Draw(IconSize, GourmetPaint), Vector4.zero);
    public static Sprite PerkLumberjack() => Save(PerkFolder + "/PerkLumberjack.png", () => Draw(IconSize, LogPaint), Vector4.zero);
    public static Sprite PerkProspector() => Save(PerkFolder + "/PerkProspector.png", () => Draw(IconSize, OrePaint), Vector4.zero);
    public static Sprite PerkSprinter() => Save(PerkFolder + "/PerkSprinter.png", () => Draw(IconSize, SprintPaint), Vector4.zero);
    public static Sprite PerkBrawler() => Save(PerkFolder + "/PerkBrawler.png", () => Draw(IconSize, FistPaint), Vector4.zero);
    public static Sprite PerkLastStand() => Save(PerkFolder + "/PerkLastStand.png", () => Draw(IconSize, ShieldPaint), Vector4.zero);
    public static Sprite PerkGlide() => Save(PerkFolder + "/PerkGlide.png", () => Draw(IconSize, FeatherPaint), Vector4.zero);
    public static Sprite PerkAttackSpeed() => Save(PerkFolder + "/PerkAttackSpeed.png", () => Draw(IconSize, SwordPaint), Vector4.zero);
    public static Sprite PerkVampire() => Save(PerkFolder + "/PerkVampire.png", () => Draw(IconSize, VampirePaint), Vector4.zero);
    public static Sprite PerkThorns() => Save(PerkFolder + "/PerkThorns.png", () => Draw(IconSize, ThornsPaint), Vector4.zero);
    public static Sprite PerkNightPower() => Save(PerkFolder + "/PerkNightPower.png", () => Draw(IconSize, MoonPaint), Vector4.zero);
    public static Sprite PerkReroll() => Save(PerkFolder + "/PerkReroll.png", () => Draw(IconSize, RerollPaint), Vector4.zero);
    public static Sprite PerkKeenEye() => Save(PerkFolder + "/PerkKeenEye.png", () => Draw(IconSize, EyePaint), Vector4.zero);
    public static Sprite PerkLongReach() => Save(PerkFolder + "/PerkLongReach.png", () => Draw(IconSize, SpearPaint), Vector4.zero);

    // A white outline of an armor piece, shown faintly in an empty slot on the Gear page (tinted by the slot).
    public static Sprite ArmorSlotIcon(ArmorSlot slot) =>
        Save(ArmorFolder + "/" + slot + ".png", () => Draw(IconSize, (x, y) => InArmor(slot, x, y) ? Color.white : Color.clear), Vector4.zero);

    // The drawn picture of a perk kind (Marko's ChestPerkType, which PlayerPerks uses too). Null for a kind with no
    // drawing yet.
    public static Sprite ForPerk(ChestPerkType type)
    {
        switch (type)
        {
            case ChestPerkType.MaxHealth:      return PerkHealth();
            case ChestPerkType.MaxStamina:     return PerkStamina();
            case ChestPerkType.MoveSpeed:      return PerkSpeed();
            case ChestPerkType.Fireball:       return PerkFireball();
            case ChestPerkType.SecondWind:     return PerkSecondWind();
            case ChestPerkType.CriticalStrike: return PerkCritical();
            case ChestPerkType.SpringLegs:     return PerkSpringLegs();
            case ChestPerkType.BountyHunter:   return PerkBountyHunter();
            case ChestPerkType.Gourmet:        return PerkGourmet();
            case ChestPerkType.Lumberjack:     return PerkLumberjack();
            case ChestPerkType.Prospector:     return PerkProspector();
            case ChestPerkType.Sprinter:       return PerkSprinter();
            case ChestPerkType.Brawler:        return PerkBrawler();
            case ChestPerkType.LastStand:      return PerkLastStand();
            case ChestPerkType.Glide:          return PerkGlide();
            case ChestPerkType.AttackSpeed:    return PerkAttackSpeed();
            case ChestPerkType.Vampire:        return PerkVampire();
            case ChestPerkType.Thorns:         return PerkThorns();
            case ChestPerkType.NightPower:     return PerkNightPower();
            case ChestPerkType.Reroll:         return PerkReroll();
            case ChestPerkType.KeenEye:        return PerkKeenEye();
            case ChestPerkType.LongReach:      return PerkLongReach();
            default:                           return null;
        }
    }

    // Gives the player's PlayerPerks a picture for every perk that has none yet (its Icons list), so the HUD's perk
    // icons and the inventory's Info tab can show them. Pictures you chose stay. Ctrl + Z undoes it.
    public static void GivePerkIcons(PlayerPerks perks)
    {
        if (perks == null) return;
        SerializedObject so = new SerializedObject(perks);
        SerializedProperty list = so.FindProperty("icons");   // the names in quotes must match PlayerPerks' fields
        if (list == null) return;

        foreach (ChestPerkType type in System.Enum.GetValues(typeof(ChestPerkType)))
        {
            SerializedProperty entry = null;
            for (int i = 0; i < list.arraySize && entry == null; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("type").enumValueIndex == (int)type) entry = e;
            }
            if (entry == null)   // a perk with no line yet: add one
            {
                list.arraySize++;
                entry = list.GetArrayElementAtIndex(list.arraySize - 1);
                entry.FindPropertyRelative("type").enumValueIndex = (int)type;
                entry.FindPropertyRelative("icon").objectReferenceValue = null;
            }
            SerializedProperty icon = entry.FindPropertyRelative("icon");
            if (icon.objectReferenceValue == null) icon.objectReferenceValue = ForPerk(type);
        }
        so.ApplyModifiedProperties();
    }

    // ----- The pictures: each gives the color of one point (x and y from -1 to 1, y up) -----

    static Color CoinPaint(float x, float y)
    {
        float d = Mathf.Sqrt(x * x + y * y);   // how far from the middle
        Color c = Color.clear;
        if (Circle(x - 0.05f, y + 0.07f, 0.9f)) c = Over(c, Shadow);                                 // a soft shadow, down right
        if (d <= 0.88f) c = Over(c, new Color(0.6f, 0.37f, 0.05f));                                   // the dark rim
        if (d <= 0.74f) c = Over(c, Color.Lerp(new Color(0.93f, 0.6f, 0.1f), new Color(1f, 0.89f, 0.38f), (y + 0.74f) / 1.48f));   // gold, lighter at the top
        if (d > 0.5f && d < 0.58f) c = Over(c, new Color(0.8f, 0.52f, 0.08f, 0.9f));                // a ring pressed into it
        if (d <= 0.74f && Circle(x + 0.3f, y - 0.32f, 0.16f)) c = Over(c, new Color(1f, 1f, 0.9f, 0.6f));   // a shine, top left
        return c;
    }

    // White inside a box with round corners (the whole picture), clear outside the corners.
    static Color BoxPaint(float x, float y)
    {
        float r = BoxRadius / (BoxSize * 0.5f);   // the corners' radius, on the -1 to 1 scale
        float cornerX = Mathf.Abs(x) - (1f - r);
        float cornerY = Mathf.Abs(y) - (1f - r);
        bool inside = cornerX <= 0f || cornerY <= 0f || cornerX * cornerX + cornerY * cornerY <= r * r;
        return inside ? Color.white : Color.clear;
    }

    static Color HeartPaint(float x, float y) =>
        Icon(x, y, InHeart, new Color(0.75f, 0.1f, 0.16f), new Color(1f, 0.45f, 0.48f), new Color(0.42f, 0.04f, 0.08f));

    static Color BoltPaint(float x, float y) =>
        Icon(x, y, (px, py) => InPolygon(px, py, Bolt), new Color(1f, 0.6f, 0.05f), new Color(1f, 0.94f, 0.4f), new Color(0.55f, 0.3f, 0.02f));

    static Color SpeedPaint(float x, float y) =>
        Icon(x, y, InChevrons, new Color(0.1f, 0.5f, 0.9f), new Color(0.55f, 0.95f, 1f), new Color(0.04f, 0.22f, 0.45f));

    static Color FlamePaint(float x, float y)
    {
        Color c = Icon(x, y, InFlame, new Color(0.85f, 0.15f, 0.05f), new Color(1f, 0.58f, 0.1f), new Color(0.45f, 0.06f, 0.02f));
        if (InFlameCore(x, y)) c = Over(c, Color.Lerp(new Color(1f, 0.78f, 0.2f), new Color(1f, 0.98f, 0.75f), (y + 1f) * 0.5f));   // the hot middle
        return c;
    }

    // A drumstick: a bone behind a piece of meat.
    static Color HungerPaint(float x, float y)
    {
        Color bone = Icon(x, y, InBone, new Color(0.8f, 0.76f, 0.68f), new Color(1f, 0.98f, 0.92f), new Color(0.45f, 0.4f, 0.32f));
        Color meat = Icon(x, y, InMeat, new Color(0.6f, 0.28f, 0.1f), new Color(0.92f, 0.55f, 0.27f), new Color(0.35f, 0.14f, 0.04f));
        return Over(bone, meat);
    }

    static Color JumpPaint(float x, float y) =>
        Icon(x, y, (px, py) => InPolygon(px, py, UpArrow), new Color(0.18f, 0.58f, 0.28f), new Color(0.55f, 0.92f, 0.5f), new Color(0.07f, 0.28f, 0.11f));

    // A steel breastplate (the same shape as the Gear page's chestplate outline).
    static Color ArmorPaint(float x, float y) =>
        Icon(x, y, (px, py) => InPolygon(px, py, Breastplate), new Color(0.42f, 0.47f, 0.55f), new Color(0.86f, 0.89f, 0.94f), new Color(0.18f, 0.2f, 0.26f));

    // ----- The loot boxes' perks -----

    // Three wind lines, the middle one curling up at its end.
    static Color WindPaint(float x, float y) =>
        Icon(x, y, InWind, new Color(0.05f, 0.55f, 0.5f), new Color(0.55f, 1f, 0.88f), new Color(0.02f, 0.25f, 0.24f));

    // A red burst star with a hot middle.
    static Color StarPaint(float x, float y)
    {
        Color c = Icon(x, y, (px, py) => InPolygon(px, py, Star), new Color(0.75f, 0.06f, 0.05f), new Color(1f, 0.5f, 0.25f), new Color(0.38f, 0.02f, 0.02f));
        if (Circle(x, y, 0.2f)) c = Over(c, new Color(1f, 0.92f, 0.7f));
        return c;
    }

    // A red target: a ring, a dot and four ticks.
    static Color TargetPaint(float x, float y) =>
        Icon(x, y, InTarget, new Color(0.8f, 0.1f, 0.08f), new Color(1f, 0.5f, 0.42f), new Color(0.38f, 0.03f, 0.03f));

    // A green arrow on a spring.
    static Color SpringPaint(float x, float y) =>
        Icon(x, y, InSpring, new Color(0.25f, 0.62f, 0.12f), new Color(0.72f, 1f, 0.4f), new Color(0.1f, 0.3f, 0.04f));

    // A gold coin (down left) and a green plus (up right).
    static Color BountyPaint(float x, float y)
    {
        Color coin = CoinPaint((x + 0.14f) * 1.3f, (y + 0.14f) * 1.3f);
        Color plus = Icon(x, y, InPlus, new Color(0.15f, 0.6f, 0.2f), new Color(0.55f, 0.95f, 0.45f), new Color(0.05f, 0.28f, 0.08f));
        return Over(coin, plus);
    }

    // The drumstick (a little smaller) with a gold twinkle.
    static Color GourmetPaint(float x, float y)
    {
        Color food = HungerPaint(x * 1.1f + 0.05f, y * 1.1f + 0.05f);
        Color shine = Icon(x, y, (px, py) => InPolygon(px - 0.58f, py - 0.58f, Sparkle), new Color(1f, 0.78f, 0.15f), new Color(1f, 0.97f, 0.6f), new Color(0.55f, 0.38f, 0.05f));
        return Over(food, shine);
    }

    // A log: brown bark, a light cut end with rings.
    static Color LogPaint(float x, float y)
    {
        Color c = Icon(x, y, InLogBody, new Color(0.38f, 0.22f, 0.1f), new Color(0.62f, 0.4f, 0.2f), new Color(0.2f, 0.11f, 0.04f));
        Color end = Icon(x, y, InLogEnd, new Color(0.86f, 0.66f, 0.4f), new Color(0.96f, 0.82f, 0.56f), new Color(0.2f, 0.11f, 0.04f));
        c = Over(c, end);
        if (InLogEnd(x, y))
        {
            float d = Mathf.Sqrt((x - 0.36f) * (x - 0.36f) + (y + 0.08f) * (y + 0.08f));
            if (Mathf.Abs(d - 0.21f) < 0.025f || Mathf.Abs(d - 0.1f) < 0.022f) c = Over(c, new Color(0.62f, 0.42f, 0.2f));   // the rings
            if (d > 0.3f) c = Over(c, new Color(0.45f, 0.27f, 0.12f));   // the bark around the cut
        }
        return c;
    }

    // A grey rock with copper and gold nuggets in it.
    static Color OrePaint(float x, float y)
    {
        Color c = Icon(x, y, (px, py) => InPolygon(px, py, Rock), new Color(0.36f, 0.37f, 0.4f), new Color(0.72f, 0.74f, 0.78f), new Color(0.15f, 0.16f, 0.18f));
        bool nugget = Circle(x + 0.25f, y - 0.05f, 0.14f) || Circle(x - 0.25f, y - 0.2f, 0.12f) || Circle(x - 0.05f, y + 0.3f, 0.11f);
        if (nugget) c = Over(c, Color.Lerp(new Color(0.85f, 0.45f, 0.1f), new Color(1f, 0.85f, 0.35f), (y + 1f) * 0.5f));
        return c;
    }

    // An orange boot with speed lines behind it.
    static Color SprintPaint(float x, float y) =>
        Icon(x, y, InSprint, new Color(0.85f, 0.32f, 0.08f), new Color(1f, 0.7f, 0.3f), new Color(0.42f, 0.13f, 0.02f));

    // A fist: the back of the hand with four knuckles, the gaps between the fingers, and the thumb across the front.
    static Color FistPaint(float x, float y)
    {
        Color dark = new Color(0.86f, 0.58f, 0.38f), light = new Color(1f, 0.84f, 0.66f), edge = new Color(0.42f, 0.24f, 0.12f);
        Color c = Icon(x, y, InFistBack, dark, light, edge);
        bool gap = y > 0.02f && y < 0.48f && (Mathf.Abs(x - 0.26f) < 0.022f || Mathf.Abs(x) < 0.022f || Mathf.Abs(x + 0.26f) < 0.022f);
        if (gap && InFistBack(x, y)) c = Over(c, edge);
        return Over(c, Icon(x, y, InThumb, dark, light, edge));
    }

    // A gold shield with a red heart on it.
    static Color ShieldPaint(float x, float y)
    {
        Color c = Icon(x, y, (px, py) => InPolygon(px, py, Shield), new Color(0.75f, 0.52f, 0.08f), new Color(1f, 0.88f, 0.42f), new Color(0.38f, 0.24f, 0.02f));
        Color heart = Icon(x, y, (px, py) => InHeart(px / 0.52f, (py - 0.04f) / 0.52f), new Color(0.75f, 0.1f, 0.16f), new Color(1f, 0.45f, 0.48f), new Color(0.42f, 0.04f, 0.08f));
        return Over(c, heart);
    }

    // A steel sword swung up right, a gold hilt, and two swing lines behind it.
    static Color SwordPaint(float x, float y)
    {
        Color c = Icon(x, y, InSwingLines, new Color(0.2f, 0.6f, 0.95f), new Color(0.6f, 0.9f, 1f), new Color(0.06f, 0.25f, 0.45f));
        c = Over(c, Icon(x, y, (px, py) => InPolygon(px, py, Blade), new Color(0.55f, 0.6f, 0.68f), new Color(0.96f, 0.97f, 1f), new Color(0.2f, 0.22f, 0.28f)));
        return Over(c, Icon(x, y, InHilt, new Color(0.7f, 0.45f, 0.08f), new Color(1f, 0.82f, 0.35f), new Color(0.35f, 0.2f, 0.03f)));
    }

    // A red drop of blood with a shine, and a green plus (healing).
    static Color VampirePaint(float x, float y)
    {
        float dx = x + 0.1f, dy = y + 0.05f;   // the drop sits a little down left, so the plus has room
        Color c = Icon(dx, dy, InDrop, new Color(0.5f, 0.02f, 0.06f), new Color(0.95f, 0.2f, 0.24f), new Color(0.28f, 0.01f, 0.03f));
        if (Circle(dx + 0.2f, dy + 0.08f, 0.1f)) c = Over(c, new Color(1f, 1f, 1f, 0.7f));   // the shine, up left on the round part
        return Over(c, Icon(x, y, InPlus, new Color(0.15f, 0.6f, 0.2f), new Color(0.55f, 0.95f, 0.45f), new Color(0.05f, 0.28f, 0.08f)));
    }

    // A green ball of thorns with a darker middle.
    static Color ThornsPaint(float x, float y)
    {
        Color c = Icon(x, y, (px, py) => InPolygon(px, py, ThornBall), new Color(0.16f, 0.36f, 0.1f), new Color(0.52f, 0.78f, 0.3f), new Color(0.07f, 0.17f, 0.04f));
        if (Circle(x, y, 0.3f)) c = Over(c, new Color(0.1f, 0.25f, 0.06f, 0.55f));
        return c;
    }

    // A pale crescent moon with a gold star in its curve.
    static Color MoonPaint(float x, float y)
    {
        Color c = Icon(x, y, InCrescent, new Color(0.5f, 0.52f, 0.85f), new Color(0.95f, 0.95f, 1f), new Color(0.18f, 0.18f, 0.42f));
        return Over(c, Icon(x, y, (px, py) => InPolygon(px - 0.48f, py - 0.42f, Sparkle), new Color(1f, 0.78f, 0.15f), new Color(1f, 0.97f, 0.6f), new Color(0.55f, 0.38f, 0.05f)));
    }

    // Two gold arrows chasing each other round a white die: a fresh roll.
    static Color RerollPaint(float x, float y)
    {
        Color c = Icon(x, y, InReroll, new Color(0.8f, 0.5f, 0.05f), new Color(1f, 0.88f, 0.35f), new Color(0.4f, 0.24f, 0.02f));
        c = Over(c, Icon(x, y, InDie, new Color(0.82f, 0.84f, 0.9f), Color.white, new Color(0.25f, 0.27f, 0.35f)));
        bool pip = Circle(x + 0.12f, y - 0.12f, 0.05f) || Circle(x, y, 0.05f) || Circle(x - 0.12f, y + 0.12f, 0.05f);   // a 3, corner to corner
        if (pip && InDie(x, y)) c = Over(c, new Color(0.15f, 0.15f, 0.2f));
        return c;
    }

    // A white eye with a gold iris inside a yellow ring (the crosshair turning yellow).
    static Color EyePaint(float x, float y)
    {
        Color c = Icon(x, y, InRing, new Color(0.95f, 0.72f, 0.05f), new Color(1f, 0.95f, 0.4f), new Color(0.45f, 0.3f, 0.02f));
        c = Over(c, Icon(x, y, InEye, new Color(0.78f, 0.82f, 0.9f), Color.white, new Color(0.2f, 0.22f, 0.32f)));
        if (!InEye(x, y)) return c;
        if (Circle(x, y, 0.25f)) c = Over(c, Color.Lerp(new Color(0.75f, 0.45f, 0.02f), new Color(1f, 0.85f, 0.25f), (y + 0.25f) / 0.5f));   // the iris
        if (Circle(x, y, 0.11f)) c = Over(c, new Color(0.08f, 0.06f, 0.05f));                  // the pupil
        if (Circle(x + 0.09f, y - 0.09f, 0.055f)) c = Over(c, new Color(1f, 1f, 1f, 0.85f));   // a shine, up left
        return c;
    }

    // A spear pointing up right (a long reach), with a dark binding under its head, and a green plus.
    static Color SpearPaint(float x, float y)
    {
        Color c = Icon(x, y, InSpearShaft, new Color(0.45f, 0.28f, 0.12f), new Color(0.72f, 0.5f, 0.27f), new Color(0.22f, 0.12f, 0.04f));
        float along = (x + y) * 0.7071f, across = (x - y) * 0.7071f;
        if (Mathf.Abs(across) <= 0.075f && along >= 0.42f && along <= 0.53f) c = Over(c, new Color(0.3f, 0.18f, 0.08f));
        c = Over(c, Icon(x, y, InSpearHead, new Color(0.55f, 0.6f, 0.68f), new Color(0.96f, 0.97f, 1f), new Color(0.2f, 0.22f, 0.28f)));
        return Over(c, Icon(x, y, (px, py) => InPlus(-px, py), new Color(0.15f, 0.6f, 0.2f), new Color(0.55f, 0.95f, 0.45f), new Color(0.05f, 0.28f, 0.08f)));
    }

    // A white feather, tilted.
    static Color FeatherPaint(float x, float y) =>
        Icon(x, y, InFeather, new Color(0.7f, 0.82f, 1f), new Color(1f, 1f, 1f), new Color(0.25f, 0.35f, 0.55f));

    // A shape with a shadow (down right), a darker edge around it, and a fill that is lighter at the top.
    static Color Icon(float x, float y, System.Func<float, float, bool> shape, Color bottom, Color top, Color edge)
    {
        Color c = Color.clear;
        if (shape(x - 0.06f, y + 0.08f)) c = Over(c, Shadow);
        if (Grown(shape, x, y, 0.06f)) c = Over(c, edge);
        if (shape(x, y)) c = Over(c, Color.Lerp(bottom, top, (y + 1f) * 0.5f));
        return c;
    }

    // ----- Shapes: true = the point is inside -----

    // A circle of radius r, where dx / dy = how far the point is from its middle.
    static bool Circle(float dx, float dy, float r) => dx * dx + dy * dy <= r * r;

    // The heart curve (x² + y² - 1)³ - x² y³ <= 0, sized to fit the picture.
    static bool InHeart(float x, float y)
    {
        float hx = x * 1.4f;
        float hy = y * 1.4f + 0.1f;
        float a = hx * hx + hy * hy - 1f;
        return a * a * a - hx * hx * hy * hy * hy <= 0f;
    }

    static bool InChevrons(float x, float y) => InPolygon(x, y, ChevronBack) || InPolygon(x, y, ChevronFront);

    // The armor outlines: a helmet with a T-shaped visor, two round shoulder plates, a breastplate, a glove, leggings, a boot.
    static bool InArmor(ArmorSlot slot, float x, float y)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet:
                bool shell = (Circle(x, y + 0.02f, 0.66f) && y > -0.12f) || (Mathf.Abs(x) <= 0.66f && y <= -0.12f && y >= -0.7f);
                bool eyes = y > -0.26f && y < -0.1f && Mathf.Abs(x) < 0.48f;   // the eye slit
                bool nose = Mathf.Abs(x) < 0.09f && y < -0.1f;                  // the slit down the middle
                return shell && !eyes && !nose;
            case ArmorSlot.Shoulders:
                bool plates = (Circle(x + 0.42f, y, 0.44f) || Circle(x - 0.42f, y, 0.44f)) && y > -0.18f;
                return plates && !Circle(x, y - 0.42f, 0.24f) && Mathf.Abs(x) > 0.04f;   // a gap for the neck
            case ArmorSlot.Chest:
                return InPolygon(x, y, Breastplate);
            case ArmorSlot.Gloves:
                bool fingers = Segment(x, y, -0.27f, 0.05f, -0.29f, 0.66f, 0.085f) || Segment(x, y, -0.09f, 0.05f, -0.09f, 0.76f, 0.085f) ||
                               Segment(x, y, 0.09f, 0.05f, 0.1f, 0.74f, 0.085f) || Segment(x, y, 0.27f, 0.05f, 0.3f, 0.6f, 0.08f);
                return InPolygon(x, y, Palm) || InPolygon(x, y, Cuff) || fingers || Segment(x, y, -0.34f, -0.12f, -0.66f, 0.24f, 0.1f);
            case ArmorSlot.Legs:
                return InPolygon(x, y, Trousers);
            case ArmorSlot.Boots:
                return InPolygon(x, y, Boot);
            default:
                return Circle(x, y, 0.6f);
        }
    }
    static bool InMeat(float x, float y) => Circle(x - 0.18f, y - 0.2f, 0.52f) || Circle(x + 0.06f, y + 0.06f, 0.32f);
    static bool InBone(float x, float y) =>
        Segment(x, y, -0.1f, -0.1f, -0.6f, -0.6f, 0.08f) || Circle(x + 0.72f, y + 0.5f, 0.13f) || Circle(x + 0.5f, y + 0.72f, 0.13f);

    // Within r of the straight line from (ax, ay) to (bx, by): a thick line with round ends.
    static bool Segment(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float dx = bx - ax, dy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));   // the nearest point on the line
        float px = ax + dx * t - x, py = ay + dy * t - y;
        return px * px + py * py <= r * r;
    }
    static bool InFlame(float x, float y) => Circle(x, y + 0.32f, 0.55f) || InPolygon(x, y, FlameTip);

    static bool InWind(float x, float y)
    {
        bool lines = Segment(x, y, -0.78f, 0.42f, 0.3f, 0.42f, 0.1f) || Segment(x, y, -0.5f, 0f, 0.55f, 0f, 0.1f) ||
                     Segment(x, y, -0.78f, -0.42f, 0.2f, -0.42f, 0.1f);
        float d = Mathf.Sqrt((x - 0.55f) * (x - 0.55f) + (y - 0.22f) * (y - 0.22f));
        return lines || (x >= 0.55f && Mathf.Abs(d - 0.22f) <= 0.1f);   // the curl: half a ring on the right
    }

    static bool InTarget(float x, float y)
    {
        float d = Mathf.Sqrt(x * x + y * y);
        bool rings = Mathf.Abs(d - 0.6f) <= 0.09f || Circle(x, y, 0.16f);
        bool ticks = (Mathf.Abs(x) <= 0.065f && Mathf.Abs(y) >= 0.38f && Mathf.Abs(y) <= 0.92f) ||
                     (Mathf.Abs(y) <= 0.065f && Mathf.Abs(x) >= 0.38f && Mathf.Abs(x) <= 0.92f);
        return rings || ticks;
    }

    static bool InSpring(float x, float y) =>
        InPolygon(x, y, SpringHead) || Segment(x, y, 0f, 0.44f, 0f, 0.22f, 0.1f) ||
        Segment(x, y, 0f, 0.22f, 0.36f, 0.08f, 0.085f) || Segment(x, y, 0.36f, 0.08f, -0.36f, -0.12f, 0.085f) ||
        Segment(x, y, -0.36f, -0.12f, 0.36f, -0.32f, 0.085f) || Segment(x, y, 0.36f, -0.32f, -0.36f, -0.52f, 0.085f) ||
        Segment(x, y, -0.36f, -0.52f, 0f, -0.66f, 0.085f) || Segment(x, y, 0f, -0.66f, 0f, -0.8f, 0.085f) ||
        Segment(x, y, -0.42f, -0.82f, 0.42f, -0.82f, 0.09f);

    static bool InPlus(float x, float y) =>
        Segment(x, y, 0.58f, 0.32f, 0.58f, 0.86f, 0.1f) || Segment(x, y, 0.31f, 0.59f, 0.85f, 0.59f, 0.1f);

    static bool InLogBody(float x, float y) => Segment(x, y, -0.62f, -0.08f, 0.3f, -0.08f, 0.36f);

    // The sword's guard (across the blade), grip and round pommel.
    static bool InHilt(float x, float y) =>
        Segment(x, y, -0.532f, -0.108f, -0.108f, -0.532f, 0.075f) || Segment(x, y, -0.36f, -0.36f, -0.62f, -0.62f, 0.06f) ||
        Circle(x + 0.7f, y + 0.7f, 0.1f);

    // Two lines along the sword, above it: the swing.
    static bool InSwingLines(float x, float y) =>
        Segment(x, y, -0.62f, 0.3f, -0.18f, 0.74f, 0.05f) || Segment(x, y, -0.84f, 0.02f, -0.5f, 0.36f, 0.05f);

    // A drop: a round bottom and a point at the top.
    static bool InDrop(float x, float y) => Circle(x, y + 0.25f, 0.52f) || InPolygon(x, y, DropTip);

    // A crescent: a circle with a smaller circle cut out of its upper right.
    static bool InCrescent(float x, float y) => Circle(x, y, 0.74f) && !Circle(x - 0.3f, y - 0.22f, 0.6f);

    // Two arcs of a ring (top and bottom, gaps on the left and right) with an arrowhead at the end of each.
    static bool InReroll(float x, float y)
    {
        float d = Mathf.Sqrt(x * x + y * y);
        float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;   // 0 = right, 90 = up
        if (angle < 0f) angle += 360f;
        bool arcs = d >= 0.46f && d <= 0.68f && ((angle >= 20f && angle <= 150f) || (angle >= 200f && angle <= 330f));
        return arcs || InPolygon(x, y, RerollHeadTop) || InPolygon(x, y, RerollHeadBottom);
    }

    // An arrowhead at the end of an arc that turns to the left: its base across the ring at "degrees", its point 30
    // degrees further round.
    static Vector2[] ArrowHead(float degrees)
    {
        float a = degrees * Mathf.Deg2Rad, tip = (degrees + 30f) * Mathf.Deg2Rad;
        return new[]
        {
            new Vector2(Mathf.Cos(a) * 0.3f, Mathf.Sin(a) * 0.3f), new Vector2(Mathf.Cos(a) * 0.84f, Mathf.Sin(a) * 0.84f),
            new Vector2(Mathf.Cos(tip) * 0.57f, Mathf.Sin(tip) * 0.57f),
        };
    }

    // A small box with round corners in the middle: the die.
    static bool InDie(float x, float y)
    {
        const float half = 0.26f, r = 0.08f;   // half its width, its corners' radius
        float cx = Mathf.Abs(x) - (half - r), cy = Mathf.Abs(y) - (half - r);
        if (cx <= 0f && Mathf.Abs(y) <= half) return true;
        if (cy <= 0f && Mathf.Abs(x) <= half) return true;
        return cx * cx + cy * cy <= r * r;
    }

    // An almond (where two big circles overlap): the eye.
    static bool InEye(float x, float y) => Circle(x, y - 0.42f, 0.74f) && Circle(x, y + 0.42f, 0.74f);

    // A thin ring round the edge.
    static bool InRing(float x, float y) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 0.8f) <= 0.065f;

    // The spear's wooden shaft, from the bottom left corner up right ("along" and "across" the spear).
    static bool InSpearShaft(float x, float y)
    {
        float along = (x + y) * 0.7071f, across = (x - y) * 0.7071f;
        return Mathf.Abs(across) <= 0.055f && along >= -1.12f && along <= 0.62f;
    }

    // The spear's steel head: a leaf shape, widest a third of the way to its point.
    static bool InSpearHead(float x, float y)
    {
        float along = (x + y) * 0.7071f, across = (x - y) * 0.7071f;
        if (along < 0.5f || along > 1.2f) return false;
        float width = along < 0.72f ? (along - 0.5f) / 0.22f : (1.2f - along) / 0.48f;
        return Mathf.Abs(across) <= 0.18f * width;
    }
    static bool InLogEnd(float x, float y) => Circle(x - 0.36f, y + 0.08f, 0.36f);

    static bool InSprint(float x, float y) =>
        InPolygon(x - 0.18f, y, Boot) || Segment(x, y, -0.92f, 0.3f, -0.45f, 0.3f, 0.07f) ||
        Segment(x, y, -0.95f, 0f, -0.42f, 0f, 0.07f) || Segment(x, y, -0.92f, -0.3f, -0.45f, -0.3f, 0.07f);

    static bool InFistBack(float x, float y) =>
        (Mathf.Abs(x) <= 0.54f && y >= -0.45f && y <= 0.3f) ||
        Circle(x + 0.39f, y - 0.3f, 0.17f) || Circle(x + 0.13f, y - 0.36f, 0.17f) ||
        Circle(x - 0.13f, y - 0.36f, 0.17f) || Circle(x - 0.39f, y - 0.3f, 0.17f) ||
        (Mathf.Abs(x) <= 0.32f && y >= -0.88f && y <= -0.4f);
    static bool InThumb(float x, float y) => Segment(x, y, -0.48f, -0.1f, 0.15f, -0.22f, 0.14f);

    // A lens (two overlapping circles) along a line tilted 35 degrees, with a thin stem: a feather.
    static bool InFeather(float x, float y)
    {
        const float S = 0.574f, C = 0.819f;    // sin and cos of 35 degrees
        float along = S * x + C * y;            // along the feather (up right)
        float across = C * x - S * y;           // across it
        bool vane = Circle(across - 0.34f, along - 0.05f, 0.72f) && Circle(across + 0.34f, along - 0.05f, 0.72f);
        return vane || Segment(across, along, 0f, -0.92f, 0f, 0.5f, 0.05f);
    }

    // A star with "points" tips: corners alternating "outer" and "inner" from the middle, the first tip straight up.
    static Vector2[] StarPoints(int points, float outer, float inner)
    {
        Vector2[] corners = new Vector2[points * 2];
        for (int i = 0; i < corners.Length; i++)
        {
            float angle = Mathf.PI * 0.5f + i * Mathf.PI / points;
            float r = i % 2 == 0 ? outer : inner;
            corners[i] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }
        return corners;
    }
    static bool InFlameCore(float x, float y) => Circle(x, y + 0.42f, 0.3f) || InPolygon(x, y, FlameCoreTip);

    // One thick ">" pointing right, its middle at x = cx.
    static Vector2[] Chevron(float cx) => new[]
    {
        new Vector2(cx - 0.32f, 0.62f), new Vector2(cx - 0.06f, 0.62f), new Vector2(cx + 0.3f, 0f),
        new Vector2(cx - 0.06f, -0.62f), new Vector2(cx - 0.32f, -0.62f), new Vector2(cx + 0.04f, 0f),
    };

    // Inside a polygon (corners in order): a line from the point to the right crosses its edges an odd number of times.
    static bool InPolygon(float x, float y, Vector2[] p)
    {
        bool inside = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        {
            if ((p[i].y > y) != (p[j].y > y) && x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                inside = !inside;
        }
        return inside;
    }

    // Inside the shape made a little bigger (by d): this point, or one of 8 points around it, is inside.
    static bool Grown(System.Func<float, float, bool> shape, float x, float y, float d)
    {
        if (shape(x, y)) return true;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            if (shape(x + Mathf.Cos(angle) * d, y + Mathf.Sin(angle) * d)) return true;
        }
        return false;
    }

    // ----- Drawing and saving -----

    // Puts "top" over "bottom" like layers of paint (see-through colors mix).
    static Color Over(Color bottom, Color top)
    {
        float a = top.a + bottom.a * (1f - top.a);
        if (a <= 0f) return Color.clear;
        float keep = bottom.a * (1f - top.a);
        return new Color((top.r * top.a + bottom.r * keep) / a, (top.g * top.a + bottom.g * keep) / a,
                         (top.b * top.a + bottom.b * keep) / a, a);
    }

    // A size x size picture. Each pixel is the average of Samples x Samples points of "paint", so edges are smooth.
    static Texture2D Draw(int size, System.Func<float, float, Color> paint)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float step = 1f / Samples;
        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;   // the colors, each counted as much as it is solid
                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        float x = (px + (sx + 0.5f) * step) / size * 2f - 1f;
                        float y = (py + (sy + 0.5f) * step) / size * 2f - 1f;
                        Color c = paint(x, y);
                        r += c.r * c.a;
                        g += c.g * c.a;
                        b += c.b * c.a;
                        a += c.a;
                    }
                }
                pixels[py * size + px] = a > 0f ? new Color(r / a, g / a, b / a, a / (Samples * Samples)) : Color.clear;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Saves a picture as a PNG sprite, only if the file isn't there yet. border = the 9-slice edges in pixels (0 = none).
    static Sprite Save(string path, System.Func<Texture2D> draw, Vector4 border)
    {
        if (!File.Exists(path))
        {
            MakeFolders(FolderOf(path));
            Texture2D texture = draw();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;   // sharp edges, no blocky compression
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
