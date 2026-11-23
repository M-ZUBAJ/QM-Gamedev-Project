/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: A human enemy's top object. Tools > Enemies > Build Human Enemies puts it there and fills in every list (the
        Polytope Studio body is its Model child).
Unity Editor: Parts, Heads, Hairs, Beards, Capes, the two slots, Weapons, Shields, Axes and Pickaxes are filled in by
              the tool. The numbers under Dressing, Tools and Metal Colors can be changed.
              Testing: Play in the new world, select a human (Human Spawner > its children) and read Outfit: what it
              wears and holds, by tier ("tools: iron axe + pickaxe" when it has some). If its weapon doesn't sit right
              in the hand, move it with Weapon Offset / Turn while playing (an axe or pickaxe while it works: Tool
              Offset / Turn; they apply to the next human that appears), then copy the numbers onto the prefab.
Purpose: What a human enemy wears and holds. Each human is dressed once, when it appears, by its area's odds (the
         Human Spawner hands them over: the grassland is poorer than the snow):
           - one "wealth" tier is rolled (none, copper, iron, gold, diamond); each of its 5 armor pieces (helmet,
             chestplate, leggings, boots, gauntlets) is worn with Piece Chance, at that tier (sometimes one lower);
             the pieces it doesn't wear are plain clothes or bare skin;
           - one armor style per human (Polytope's two sets), its metal tinted by tier: copper, steel, gold, ice blue;
           - a random weapon in its right hand (none for the poorest), sometimes a shield with a one-handed weapon;
           - an axe and a pickaxe (Tool Chance; the player's tier tools: wooden for the poorest, then stone, iron, gold,
             diamond by wealth), kept out of sight until it works: HumanBrain holds the axe to chop and the pickaxe to
             mine (its weapon goes away meanwhile). They're its tools for the player's tool rules: only a pickaxe mines
             ore, an iron axe chops the snow's spruce (a war axe counts as an axe of its tier);
           - a random head, hair (hidden under a helmet) and beard; a cape now and then for the richer ones.
         Every other piece of the body is removed, so only what it wears is drawn.
         HumanLoot reads what it wore and carried for the drops and coins (1 of the tier's material and 1 - 4 coins per
         piece and tool).
         Its weapon decides how hard it hits (HumanBrain asks WeaponDamage): each weapon's own Damage (Weapons list:
         a mace hits harder than a sword, a longsword harder still) at copper, 25 % more for each tier above.
         Its armor takes its share off every hit it gets, with the player's armor numbers (a full set of copper 10 %,
         iron 15 %, gold 25 %, diamond 40 %; ProtectionPercent): Marko's EnemyCombat asks Reduce (a [Tony] line).
Requirement: HumanGear.cs x-> ToolStats.cs, ToolRules.cs
             HumanGear.cs <-x EnemyCombat.cs, HumanEnemyBuilder.cs, HumanLoot.cs, HumanSpawner.cs
             HumanGear.cs <-o HumanBrain.cs
*/

using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]   // dressed before the other scripts on the human start (DeathEffect measures the body it wears)
public class HumanGear : MonoBehaviour
{
    // The metal tiers, weakest first. The number is also what one piece is worth in coins (copper 1 ... diamond 4).
    public enum Tier { None, Copper, Iron, Gold, Diamond }

    // The 5 armor pieces that count (for the drops, the coins and the protection).
    public enum Piece { Helmet, Chestplate, Leggings, Boots, Gauntlets }

    public const int PieceCount = 5;

    // One body or armor piece of the model (the tool lists them all).
    [System.Serializable]
    public class Part
    {
        public Piece piece;
        [Tooltip("Its set: 01_a or 05_c (Polytope's two armors), cloth (plain clothes) or naked (bare skin).")]
        public string style;
        [Tooltip("On: armor (it counts for the drops, the coins and the protection). Off: plain clothes or bare skin.")]
        public bool armored;
        public GameObject part;
    }

    // A weapon it may hold.
    [System.Serializable]
    public class Weapon
    {
        public GameObject prefab;
        [Tooltip("Held with both hands: no shield with it.")]
        public bool twoHanded;
        [Tooltip("An axe (Polytope's war axe): it chops trees like an axe of its tier.")]
        public bool isAxe;
        [Tooltip("One hit's damage with it at copper tier; each tier above adds 25 % (iron x1.25, gold x1.56, diamond x1.95). 0 = 10.")]
        public float damage;
    }

    // How likely each wealth tier is in an area. They're weights: they don't have to add up to 100.
    [System.Serializable]
    public struct TierOdds
    {
        public float none, copper, iron, gold, diamond;

        public TierOdds(float none, float copper, float iron, float gold, float diamond)
        {
            this.none = none;
            this.copper = copper;
            this.iron = iron;
            this.gold = gold;
            this.diamond = diamond;
        }

        // A random tier with these weights (all 0 = None).
        public Tier Roll()
        {
            float total = none + copper + iron + gold + diamond;
            if (total <= 0f) return Tier.None;
            float r = Random.value * total;
            if ((r -= none) < 0f) return Tier.None;
            if ((r -= copper) < 0f) return Tier.Copper;
            if ((r -= iron) < 0f) return Tier.Iron;
            return r - gold < 0f ? Tier.Gold : Tier.Diamond;
        }
    }

    // Raised when the default numbers change, so Build Human Enemies gives old prefabs the new ones once.
    // 2 (23/11/2026): every human carries an axe and a pickaxe (Tool Chance 1).
    public const int SettingsVersion = 2;

    const float TierBoost = 1.25f;         // each metal tier above copper hits 25 % harder (like the player's tools)
    const float DefaultWeaponDamage = 10f; // a weapon whose Damage is 0

    // ===== Fields =====
    [Header("The body (the tool fills these in)")]
    [Tooltip("Every helmet, body, legs, boots and gauntlets piece of the model: the armor ones and the plain ones.")]
    [SerializeField] List<Part> parts = new List<Part>();
    [SerializeField] List<GameObject> heads = new List<GameObject>();
    [SerializeField] List<GameObject> hairs = new List<GameObject>();
    [Tooltip("Empty for a woman.")]
    [SerializeField] List<GameObject> beards = new List<GameObject>();
    [SerializeField] List<GameObject> capes = new List<GameObject>();

    [Header("Weapons (the tool fills these in)")]
    [Tooltip("The bone a weapon goes into (Polytope's right hand weapon slot).")]
    [SerializeField] Transform weaponSlot;
    [Tooltip("The bone a shield goes into (Polytope's left hand shield slot).")]
    [SerializeField] Transform shieldSlot;
    [SerializeField] List<Weapon> weapons = new List<Weapon>();
    [SerializeField] List<GameObject> shields = new List<GameObject>();

    [Header("Tools (the tool fills these in)")]
    [Tooltip("The game's axes, weakest first: Wooden, Stone, Iron, Gold, Diamond (the player's tool items).")]
    [SerializeField] List<GameObject> axes = new List<GameObject>();
    [Tooltip("The game's pickaxes, weakest first: Wooden, Stone, Iron, Gold, Diamond.")]
    [SerializeField] List<GameObject> pickaxes = new List<GameObject>();

    [Range(0f, 1f)]
    [Tooltip("The chance it carries an axe and a pickaxe, of its wealth's tier (none = wooden, copper = stone, iron, gold, diamond). 1 = every human: it chops with the axe and mines with the pickaxe.")]
    [SerializeField] float toolChance = 1f;

    [Header("Dressing")]
    [Tooltip("The odds when nobody hands any over (a human put in a scene by hand): the grassland's, mostly poor.")]
    [SerializeField] TierOdds defaultOdds = new TierOdds(40f, 40f, 15f, 4f, 1f);

    [Range(0f, 1f)]
    [Tooltip("Once it has a tier: the chance it wears each armor piece. All 5 at once = a full set (at 0.6, about 8 in 100 of them).")]
    [SerializeField] float pieceChance = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("The chance a piece, the weapon or the shield is one tier below its wealth (copper stays copper).")]
    [SerializeField] float lowerChance = 0.25f;

    [Range(0f, 1f)]
    [Tooltip("With a one-handed weapon: the chance it also carries a shield.")]
    [SerializeField] float shieldChance = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("Iron or richer: the chance it wears a cape (looks only, drops nothing).")]
    [SerializeField] float capeChance = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("A man: the chance of a beard.")]
    [SerializeField] float beardChance = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("Pieces without armor: the chance they're bare skin instead of plain clothes.")]
    [SerializeField] float bareChance = 0.3f;

    [Header("Metal colors")]
    [Tooltip("The metal of copper armor and weapons.")]
    [SerializeField] Color copperMetal = new Color(0.8f, 0.42f, 0.22f);
    [SerializeField] Color ironMetal = new Color(0.6f, 0.62f, 0.68f);
    [SerializeField] Color goldMetal = new Color(1f, 0.72f, 0.18f);
    [SerializeField] Color diamondMetal = new Color(0.4f, 0.88f, 1f);

    [Header("Weapon in the hand")]
    [Tooltip("Moves the weapon in the hand (meters, in the slot's own directions) if it doesn't sit right.")]
    [SerializeField] Vector3 weaponOffset;
    [Tooltip("Turns the weapon in the hand (degrees).")]
    [SerializeField] Vector3 weaponTurn;
    [SerializeField] Vector3 shieldOffset;
    [SerializeField] Vector3 shieldTurn;
    [Tooltip("Moves an axe or pickaxe in the hand. The game's tools have the handle's end at their pivot, so they're pushed up the handle a bit.")]
    [SerializeField] Vector3 toolOffset = new Vector3(0f, 0.1f, 0f);
    [Tooltip("Turns an axe or pickaxe in the hand. Polytope's slot holds a weapon pointing down its -Y with an axe's edge toward -X; the game's tools point up +Y with the edge toward +X, so they're turned half round (Z 180).")]
    [SerializeField] Vector3 toolTurn = new Vector3(0f, 0f, 180f);
    [Tooltip("Axes only: spun this many degrees around the handle after Tool Turn, so the sharp edge faces what it chops (like the player's ToolInHand > Axe Turn). 0 = the edge the other way.")]
    [SerializeField] float axeTurn = 180f;

    [Header("Now (while playing)")]
    [Tooltip("What it wears and holds, written when it's dressed (only to read).")]
    [SerializeField] string outfit = "";

    // Polytope's shaders mark which parts of the texture are metal. The armor's plates are METAL 2 and 3 (METAL 1 is its
    // gold trims), the weapons' steel is METAL 1, 3 and 4 (METAL 2 is their gold trims).
    static readonly int ArmorPlates = Shader.PropertyToID("_METAL2COLOR");
    static readonly int ArmorPlatesDark = Shader.PropertyToID("_METAL3COLOR");
    static readonly int WeaponSteel = Shader.PropertyToID("_METAL1COLOR");
    static readonly int WeaponSteelDark = Shader.PropertyToID("_METAL3COLOR");
    static readonly int WeaponSteelLight = Shader.PropertyToID("_METAL4COLOR");

    readonly Tier[] worn = new Tier[PieceCount];   // by Piece: None = not wearing armor there
    Tier wealth;
    Tier weaponTier;
    Tier shieldTier;
    GameObject weapon;
    GameObject shield;
    bool weaponIsAxe;          // its weapon is Polytope's war axe: it chops like an axe
    bool weaponTwoHanded;      // its weapon is held with both hands (longsword, pike): its own attack animation
    float weaponDamage;        // its weapon's Damage at copper tier
    GameObject axe;            // its axe and pickaxe (hidden until it works), or null
    GameObject pickaxe;
    ToolTier kitTier;          // their tier (None = it has no tools)
    bool dressed;
    [SerializeField, HideInInspector] int settingsVersion = SettingsVersion;   // which defaults this prefab was made with

    public bool Dressed => dressed;
    public Tier Wealth => wealth;
    public Tier WeaponTier => weaponTier;   // None = bare hands
    public Tier ShieldTier => shieldTier;   // None = no shield
    public bool HasWeapon => weapon != null;
    public bool TwoHanded => weapon != null && weaponTwoHanded;   // a longsword or pike (HumanBrain picks its attack by it)

    // One hit with its weapon: the weapon's Damage, 25 % more for each tier above copper (0 = no weapon).
    public float WeaponDamage => weapon == null ? 0f
        : (weaponDamage > 0f ? weaponDamage : DefaultWeaponDamage) * Mathf.Pow(TierBoost, Mathf.Max(0, (int)weaponTier - 1));

    // A hit after its armor: the protection's share taken off (a full diamond set: 40 % less). Marko's EnemyCombat asks
    // this when it's hit (a [Tony] line), so every attack of the player counts it: punches, tools, arrows, fireball.
    public float Reduce(float damage) => damage * (1f - Mathf.Clamp01(ProtectionPercent / 100f));
    public string Outfit => outfit;
    public int MadeWithSettings => settingsVersion;

    // Something is in its hand right now: its weapon, or the axe or pickaxe it took out to work (false = bare hands).
    public bool HoldsSomething => Shown(weapon) || Shown(axe) || Shown(pickaxe);

    // Its tools for the player's tool rules: the tier of what it chops with (its axe, or a war axe of its weapon's tier)
    // and mines with (only a pickaxe). None = bare hands or a weapon that isn't for that job.
    public ToolTier AxeTier => axe != null ? kitTier : weaponIsAxe ? ToolTierOf(weaponTier) : ToolTier.None;
    public ToolTier PickaxeTier => pickaxe != null ? kitTier : ToolTier.None;

    // The tools it carries (for the drops): each one's tier, None = it doesn't have that one.
    public ToolTier KitAxeTier => axe != null ? kitTier : ToolTier.None;
    public ToolTier KitPickaxeTier => pickaxe != null ? kitTier : ToolTier.None;

    public Tier WornTier(Piece piece) => worn[(int)piece];

    // How much its armor takes off a hit (percent), with the player's armor numbers: a full set of copper 10 %, iron
    // 15 %, gold 25 %, diamond 40 % (the chestplate counts most).
    public float ProtectionPercent
    {
        get
        {
            float sum = 0f;
            for (int i = 0; i < PieceCount; i++) sum += Share((Piece)i) * SetTotal(worn[i]);
            return sum;
        }
    }

    // ===== Unity messages (Unity calls these) =====

    // A human put in a scene by hand: dressed with the default odds. (The Human Spawner dresses its own right after
    // making them, before this.)
    void Start()
    {
        if (!dressed) Dress(defaultOdds);
    }

    // ===== My methods =====

    // Dresses it once with an area's odds: its wealth, armor, weapon, shield and looks. A second call does nothing.
    public void Dress(TierOdds odds)
    {
        if (dressed) return;
        dressed = true;
        wealth = odds.Roll();

        string armorStyle = Pick(Styles(true));   // one armor look for all its pieces
        string plainStyle = PlainStyle();          // plain clothes or bare skin for the rest

        for (int i = 0; i < PieceCount; i++)
        {
            Piece piece = (Piece)i;
            Tier tier = wealth != Tier.None && Random.value < pieceChance ? MaybeLower(wealth) : Tier.None;
            GameObject chosen = tier != Tier.None ? PartOf(piece, armorStyle, true) : PartOf(piece, plainStyle, false);
            if (tier != Tier.None && chosen == null)   // this body has no such armor piece: plain clothes there instead
            {
                tier = Tier.None;
                chosen = PartOf(piece, plainStyle, false);
            }
            worn[i] = tier;
            KeepOnly(piece, chosen);
            if (tier != Tier.None) Tint(chosen, tier, true);
        }

        KeepOne(heads, Pick(heads));
        KeepOne(hairs, worn[(int)Piece.Helmet] != Tier.None ? null : Pick(hairs));   // a helmet hides the hair
        KeepOne(beards, Random.value < beardChance ? Pick(beards) : null);
        KeepOne(capes, wealth >= Tier.Iron && Random.value < capeChance ? Pick(capes) : null);

        Arm();
        GiveTools();
        outfit = Describe();
    }

    // Takes out the tool for a job (Axe: chopping, Pickaxe: mining); its weapon goes away meanwhile. Without that tool
    // it keeps its weapon (or bare hands).
    public void HoldTool(ToolKind job)
    {
        GameObject tool = job == ToolKind.Axe ? axe : job == ToolKind.Pickaxe ? pickaxe : null;
        if (tool == null)
        {
            HoldWeapon();
            return;
        }
        if (axe != null) axe.SetActive(tool == axe);
        if (pickaxe != null) pickaxe.SetActive(tool == pickaxe);
        if (weapon != null) weapon.SetActive(false);
    }

    // Puts its tools away and takes its weapon again.
    public void HoldWeapon()
    {
        if (axe != null) axe.SetActive(false);
        if (pickaxe != null) pickaxe.SetActive(false);
        if (weapon != null) weapon.SetActive(true);
    }

    // Puts everything in its hand away (its shield stays): bare hands, to punch a tree or rock it has no tool for, or
    // to pick berries. HoldWeapon takes the weapon again.
    public void HoldNothing()
    {
        if (axe != null) axe.SetActive(false);
        if (pickaxe != null) pickaxe.SetActive(false);
        if (weapon != null) weapon.SetActive(false);
    }

    // With Tool Chance: an axe and a pickaxe of its wealth's tier, in the weapon's hand but hidden until it works.
    void GiveTools()
    {
        kitTier = ToolTier.None;
        if (weaponSlot == null || Random.value >= toolChance) return;
        ToolTier tier = KitTierFor(wealth);
        int index = (int)tier - (int)ToolTier.Wood;   // 0 = wooden ... 4 = diamond
        axe = HoldHidden(index < axes.Count ? axes[index] : null, axeTurn);
        pickaxe = HoldHidden(index < pickaxes.Count ? pickaxes[index] : null);
        if (axe != null || pickaxe != null) kitTier = tier;
    }

    // A tool item's look (its Model child, like the player's ToolInHand) in the weapon slot, switched off.
    // spin = degrees around its own handle (an axe's edge toward what it chops).
    GameObject HoldHidden(GameObject toolItem, float spin = 0f)
    {
        if (toolItem == null) return null;
        Transform look = toolItem.transform.Find("Model");
        GameObject held = Hold(look != null ? look.gameObject : toolItem, weaponSlot, toolOffset, toolTurn);
        held.transform.localRotation *= Quaternion.AngleAxis(spin, Vector3.up);   // its handle runs along its own up
        held.SetActive(false);
        return held;
    }

    // The tools it can afford: the poorest wooden ones, then stone (copper), iron, gold, diamond.
    static ToolTier KitTierFor(Tier wealth)
    {
        switch (wealth)
        {
            case Tier.Copper: return ToolTier.Stone;
            case Tier.Iron: return ToolTier.Iron;
            case Tier.Gold: return ToolTier.Gold;
            case Tier.Diamond: return ToolTier.Diamond;
            default: return ToolTier.Wood;
        }
    }

    static bool Shown(GameObject held) => held != null && held.activeSelf;

    // A metal tier as a tool tier (the player's rules: copper counts as iron).
    static ToolTier ToolTierOf(Tier tier)
    {
        switch (tier)
        {
            case Tier.Copper:
            case Tier.Iron: return ToolTier.Iron;
            case Tier.Gold: return ToolTier.Gold;
            case Tier.Diamond: return ToolTier.Diamond;
            default: return ToolTier.None;
        }
    }

    // A weapon of its tier (or one lower) in the right hand, sometimes a shield in the left. The poorest have none.
    void Arm()
    {
        weaponTier = shieldTier = Tier.None;
        if (wealth == Tier.None || weapons.Count == 0 || weaponSlot == null) return;

        Weapon pick = weapons[Random.Range(0, weapons.Count)];
        if (pick == null || pick.prefab == null) return;
        weaponTier = MaybeLower(wealth);
        weaponIsAxe = pick.isAxe;
        weaponTwoHanded = pick.twoHanded;
        weaponDamage = pick.damage;
        weapon = Hold(pick.prefab, weaponSlot, weaponOffset, weaponTurn);
        Tint(weapon, weaponTier, false);

        if (pick.twoHanded || shields.Count == 0 || shieldSlot == null || Random.value >= shieldChance) return;
        GameObject shieldPrefab = shields[Random.Range(0, shields.Count)];
        if (shieldPrefab == null) return;
        shieldTier = MaybeLower(wealth);
        shield = Hold(shieldPrefab, shieldSlot, shieldOffset, shieldTurn);
        Tint(shield, shieldTier, false);
    }

    // The styles of this body's pieces: the armor ones (01_a, 05_c) or the plain ones (cloth, naked).
    List<string> Styles(bool armored)
    {
        List<string> found = new List<string>();
        foreach (Part p in parts)
        {
            if (p != null && p.part != null && p.armored == armored && !found.Contains(p.style)) found.Add(p.style);
        }
        return found;
    }

    // Plain clothes, or bare skin with Bare Chance (null = this body has neither).
    string PlainStyle()
    {
        List<string> plain = Styles(false);
        string bare = plain.Find(style => style.Contains("naked"));
        string clothes = plain.Find(style => !style.Contains("naked"));
        if (bare != null && (clothes == null || Random.value < bareChance)) return bare;
        return clothes;
    }

    // The piece of this style (null = there's none, e.g. a plain helmet).
    GameObject PartOf(Piece piece, string style, bool armored)
    {
        if (style == null) return null;
        foreach (Part p in parts)
        {
            if (p != null && p.part != null && p.piece == piece && p.armored == armored && p.style == style) return p.part;
        }
        return null;
    }

    // Shows "keep" (null = nothing) and removes every other part for that piece, so only what it wears is drawn.
    void KeepOnly(Piece piece, GameObject keep)
    {
        foreach (Part p in parts)
        {
            if (p == null || p.part == null || p.piece != piece) continue;
            if (p.part == keep) p.part.SetActive(true);
            else Remove(p.part);
        }
    }

    static void KeepOne(List<GameObject> options, GameObject keep)
    {
        foreach (GameObject option in options)
        {
            if (option == null) continue;
            if (option == keep) option.SetActive(true);
            else Remove(option);
        }
    }

    // Switched off at once (DeathEffect, starting this frame, only looks at what's switched on), deleted at the end of
    // the frame.
    static void Remove(GameObject part)
    {
        part.SetActive(false);
        Destroy(part);
    }

    // One tier lower with Lower Chance (copper stays copper).
    Tier MaybeLower(Tier tier) => tier > Tier.Copper && Random.value < lowerChance ? tier - 1 : tier;

    // A copy of a weapon or shield in a slot bone. It's only a look: the human's own collider is what gets hit.
    static GameObject Hold(GameObject prefab, Transform slot, Vector3 offset, Vector3 turn)
    {
        GameObject held = Instantiate(prefab, slot);
        held.transform.localPosition = offset;
        held.transform.localRotation = Quaternion.Euler(turn);
        held.transform.localScale = Vector3.one;
        foreach (Collider part in held.GetComponentsInChildren<Collider>()) Destroy(part);
        return held;
    }

    // Colors the metal of an armor piece or a weapon by its tier (a property block: the shared material stays as it is).
    void Tint(GameObject target, Tier tier, bool armor)
    {
        if (target == null || tier == Tier.None) return;
        Color metal = MetalOf(tier);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Renderer look in target.GetComponentsInChildren<Renderer>(true))
        {
            look.GetPropertyBlock(block);
            if (armor)
            {
                block.SetColor(ArmorPlates, metal);
                block.SetColor(ArmorPlatesDark, Shade(metal, 0.85f));
            }
            else
            {
                block.SetColor(WeaponSteel, metal);
                block.SetColor(WeaponSteelDark, Shade(metal, 0.85f));
                block.SetColor(WeaponSteelLight, Shade(metal, 1.15f));
            }
            look.SetPropertyBlock(block);
        }
    }

    Color MetalOf(Tier tier)
    {
        switch (tier)
        {
            case Tier.Copper: return copperMetal;
            case Tier.Gold: return goldMetal;
            case Tier.Diamond: return diamondMetal;
            default: return ironMetal;
        }
    }

    static Color Shade(Color c, float times) => new Color(c.r * times, c.g * times, c.b * times, c.a);

    // A piece's share of a full set's protection: 4 x 18 % + the chestplate's 28 % = all of it.
    static float Share(Piece piece) => piece == Piece.Chestplate ? 0.28f : 0.18f;

    // A full set's protection by tier, the player's armor formula: copper 10, iron 15, gold 25, diamond 40.
    static float SetTotal(Tier tier)
    {
        int n = (int)tier;
        return n <= 0 ? 0f : 2.5f * n * n - 2.5f * n + 10f;
    }

    static string Pick(List<string> options) => options.Count > 0 ? options[Random.Range(0, options.Count)] : null;

    // A random one that still exists (null if there's none).
    static GameObject Pick(List<GameObject> options)
    {
        int count = options.Count;
        int start = count > 0 ? Random.Range(0, count) : 0;
        for (int i = 0; i < count; i++)
        {
            GameObject option = options[(start + i) % count];
            if (option != null) return option;
        }
        return null;
    }

    // "Wealth Iron | helmet iron, chestplate copper, leggings -, boots -, gauntlets iron | Sword iron + shield iron | protection 8.6 %"
    string Describe()
    {
        StringBuilder s = new StringBuilder("Wealth ").Append(wealth).Append(" | ");
        for (int i = 0; i < PieceCount; i++)
        {
            if (i > 0) s.Append(", ");
            s.Append(((Piece)i).ToString().ToLower()).Append(' ').Append(worn[i] == Tier.None ? "-" : worn[i].ToString().ToLower());
        }
        s.Append(" | ").Append(weapon != null ? Friendly(weapon.name) + " " + weaponTier.ToString().ToLower() +
                               " (" + WeaponDamage.ToString("0.#") + " a hit)" : "bare hands");
        if (shield != null) s.Append(" + shield ").Append(shieldTier.ToString().ToLower());
        if (kitTier != ToolTier.None) s.Append(" | tools: ").Append(ToolRules.TierName(kitTier).ToLower()).Append(" axe + pickaxe");
        return s.Append(" | protection ").Append(ProtectionPercent.ToString("0.#")).Append(" %").ToString();
    }

    // "PT_ShortWaraxe_01_a(Clone)" -> "ShortWaraxe"
    static string Friendly(string name)
    {
        string n = name.Replace("(Clone)", "").Replace("PT_", "");
        int end = n.IndexOf("_0");
        return end > 0 ? n.Substring(0, end) : n;
    }
}
