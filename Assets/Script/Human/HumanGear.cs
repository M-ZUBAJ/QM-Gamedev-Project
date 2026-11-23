/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: A human enemy's top object. Tools > Enemies > Build Human Enemies puts it there and fills in every list (the
        Polytope Studio body is its Model child).
Unity Editor: Parts, Heads, Hairs, Beards, Capes, the two slots, Weapons, Shields, Axes and Pickaxes are filled in by
              the tool. The numbers under Dressing, Tools and Metal Colors can be changed.
              Testing: Play in the new world, select a human (a top-level object named "Human Type 1 Male(Clone)"...)
              and read Outfit: what it wears and holds, by tier ("tools: iron axe + pickaxe" when it has some). ⋮ >
              Test: Report Body writes in the Console whether each worn body part is drawn. If its weapon doesn't sit right
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
           - a random head, hair (hidden under a helmet) and beard; a cape now and then for the richer ones;
           - Wears Hood (Type 2's look): always its cloth hood (the tool makes one to fit its head) instead of a helmet,
             in one of the Hood Colors; the hair goes under it.
         Every other piece of the body is removed, so only what it wears is drawn. The Human Spawner dresses it while
         it's still switched off; a human put in a scene by hand is switched off for the moment it's dressed in Start.
         The pieces it doesn't wear are deleted at once then, so its body is finished before its animator and body parts
         start and nothing in it changes while they run. On a human already switched on a removed piece is only switched
         off. It never wears a part with nothing to draw (no mesh): Polytope's male prefab has four bare-skin parts
         whose meshes are missing, and a man dressed in them was missing those pieces (only a hood floating, or no torso
         and arms, 23/11/2026). Build Human Enemies gives those parts the FBX's meshes; on a prefab built before that it
         wears plain clothes there instead.
         Body check (Check Body): about once a second, while the camera is near and the human is on screen, each worn
         body part whose box is in the camera's view must have been drawn. One that wasn't is mended (it works out its
         box from its real pose every frame, is linked to its bones again and restarted) and the Console says what it
         found (a part without a mesh shows as "NO MESH"), so the cause can be fixed.
         HumanLoot reads what it wore and carried for the drops and coins (1 of the tier's material and 1 - 4 coins per
         piece and tool).
         Its weapon decides how hard it hits (HumanBrain asks WeaponDamage): each weapon's own Damage (Weapons list:
         a mace hits harder than a sword, a longsword harder still) at copper, 25 % more for each tier above.
         Its armor takes its share off every hit it gets, with the player's armor numbers (a full set of copper 10 %,
         iron 15 %, gold 25 %, diamond 40 %; ProtectionPercent): Marko's EnemyCombat asks Reduce (a [Tony] line).
Requirement: HumanGear.cs x-> ToolStats.cs, ToolRules.cs
             HumanGear.cs o-> EnemyCombat.cs, Animator (Unity), CapsuleCollider (Unity)
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
    // 3 (23/11/2026): the same again: prefabs saved before this number existed took 2 as theirs (its starting value), so
    //   the Type 1 prefabs kept their old Tool Chance 0.5.
    public const int SettingsVersion = 3;

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

    [Header("Hood (Type 2)")]
    [Tooltip("On: it always wears its hood instead of a helmet (Type 2's look). The tool sets it for each type.")]
    [SerializeField] bool wearsHood;

    [Tooltip("Its hood: a child of the head bone, switched off until it's worn (the tool makes it to fit the head).")]
    [SerializeField] GameObject hood;

    [Tooltip("The hood's colors: one is picked for each human.")]
    [SerializeField] List<Color> hoodColors = new List<Color>
    {
        new Color(0.22f, 0.32f, 0.16f),   // forest green
        new Color(0.36f, 0.24f, 0.14f),   // brown
        new Color(0.22f, 0.27f, 0.36f),   // slate blue
        new Color(0.40f, 0.14f, 0.14f),   // wine red
    };

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

    [Header("Body check")]
    [Tooltip("On: about once a second, while the camera is near and the human is on screen, it checks that the body parts it wears are drawn. If none is (its hood floating alone), they're mended and the Console says what it found.")]
    [SerializeField] bool checkBody = true;

    [Range(5f, 100f)]
    [Tooltip("The body is only checked while the camera is closer than this (meters).")]
    [SerializeField] float checkDistance = 40f;

    [Header("Now (while playing)")]
    [Tooltip("What it wears and holds, written when it's dressed (only to read).")]
    [SerializeField] string outfit = "";

    // Polytope's shaders mark which parts of the texture are metal. The armor's plates are METAL 2 and 3 (METAL 1 is its
    // gold trims), the weapons' steel is METAL 1, 3 and 4 (METAL 2 is their gold trims).
    static readonly int HoodColor = Shader.PropertyToID("_Color");   // the hood's Standard material
    static readonly int ArmorPlates = Shader.PropertyToID("_METAL2COLOR");
    static readonly int ArmorPlatesDark = Shader.PropertyToID("_METAL3COLOR");
    static readonly int WeaponSteel = Shader.PropertyToID("_METAL1COLOR");
    static readonly int WeaponSteelDark = Shader.PropertyToID("_METAL3COLOR");
    static readonly int WeaponSteelLight = Shader.PropertyToID("_METAL4COLOR");

    const float CheckEvery = 1f;        // seconds between body checks
    const float FirstCheckAfter = 2f;   // seconds after it's dressed before the first one (it settles in first)
    const int MissesToMend = 2;         // checks in a row with a part not drawn before it's mended (a moment of lag is fine)
    const int MostReports = 2;          // Console lines per human at most
    const float OnScreenMargin = 0.05f; // its middle must be this far inside the screen's edges (a share of the screen)

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
    Renderer hoodLook;         // the hood's renderer, once it's worn
    readonly List<SkinnedMeshRenderer> skins = new List<SkinnedMeshRenderer>();   // the worn body parts (for the body check)
    CapsuleCollider shape;     // its body's capsule (where its middle is)
    EnemyCombat combat;        // Marko's: no checks once it's dying (DeathEffect hides it then)
    float nextBodyCheck;
    int misses;                // body checks in a row with a worn part not drawn
    int reports;               // Console lines written about its body
    int mends;                 // how many times its body was mended
    readonly List<SkinnedMeshRenderer> hidden = new List<SkinnedMeshRenderer>();   // worn parts in view but not drawn (last check)
    static readonly Plane[] frustum = new Plane[6];   // the camera's view, for the body check
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

    // A human put in a scene by hand: dressed with the default odds, switched off for that moment, so its animator and
    // body parts start again with only what it wears. (The Human Spawner dresses its own before they switch on.)
    void Start()
    {
        if (dressed) return;
        gameObject.SetActive(false);
        Dress(defaultOdds);
        gameObject.SetActive(true);
    }

    // The body check (Check Body): a worn part in the camera's view that wasn't drawn gets mended (see Purpose).
    void Update()
    {
        if (!checkBody || !dressed || Time.time < nextBodyCheck) return;
        nextBodyCheck = Time.time + CheckEvery;
        Camera cam = OnScreen();
        if (cam == null || FindHidden(cam) == 0)
        {
            misses = 0;
            return;
        }
        if (++misses < MissesToMend) return;
        misses = 0;

        string found = BodyReport();   // what was wrong, before mending
        Mend(hidden);
        mends++;
        if (reports >= MostReports) return;
        reports++;
        Debug.LogWarning(name + ": " + hidden.Count + " of its " + skins.Count + " worn body parts weren't drawn although they " +
                         "were in view" + (mends > 1 ? " (again)" : "") + ", so they were mended. Found: " + found +
                         ". (Copy this line when you report it.)", this);
    }

    [ContextMenu("Test: Report Body")]
    void TestReportBody()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Press Play first.");
            return;
        }
        Camera cam = OnScreen();
        if (cam != null) FindHidden(cam);
        else
        {
            FindSkins();
            hidden.Clear();
        }
        Debug.Log(name + ": " + (cam != null ? "on screen" : "not on screen (or dying)") + " | " + BodyReport(), this);
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
            if (piece == Piece.Helmet && HoodOn)   // its hood instead of any helmet
            {
                worn[i] = Tier.None;
                KeepOnly(piece, null);
                continue;
            }
            Tier tier = wealth != Tier.None && Random.value < pieceChance ? MaybeLower(wealth) : Tier.None;
            GameObject chosen = tier != Tier.None ? PartOf(piece, armorStyle, true) : PartOf(piece, plainStyle, false);
            if (tier != Tier.None && chosen == null)   // this body has no such armor piece: plain clothes there instead
            {
                tier = Tier.None;
                chosen = PartOf(piece, plainStyle, false);
            }
            if (chosen == null) chosen = AnyPlain(piece);   // its plain style has no such piece to draw: another plain one
            worn[i] = tier;
            KeepOnly(piece, chosen);
            if (tier != Tier.None) Tint(chosen, tier, true);
        }

        KeepOne(heads, Pick(heads));
        KeepOne(hairs, worn[(int)Piece.Helmet] != Tier.None || HoodOn ? null : Pick(hairs));   // a helmet or hood hides the hair
        PutOnHood();
        KeepOne(beards, Random.value < beardChance ? Pick(beards) : null);
        KeepOne(capes, wealth >= Tier.Iron && Random.value < capeChance ? Pick(capes) : null);

        Arm();
        GiveTools();
        outfit = Describe();
        nextBodyCheck = Time.time + FirstCheckAfter;
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

    // Type 2's hood on (in one of the Hood Colors), or away for good when it doesn't wear one.
    void PutOnHood()
    {
        if (hood == null) return;
        if (!HoodOn)
        {
            Remove(hood);
            hood = null;
            return;
        }
        hood.SetActive(true);
        hoodLook = hood.GetComponent<Renderer>();
        if (hoodLook == null || hoodColors.Count == 0) return;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        hoodLook.GetPropertyBlock(block);
        block.SetColor(HoodColor, hoodColors[Random.Range(0, hoodColors.Count)]);
        hoodLook.SetPropertyBlock(block);
    }

    // ----- The body check -----

    // The camera, while the human is alive, near it and its middle is on screen; null otherwise (nothing to check).
    Camera OnScreen()
    {
        if (combat == null) combat = GetComponent<EnemyCombat>();
        if (combat != null && combat.currentHealth <= 0f) return null;   // dying: DeathEffect hides the body on purpose
        Camera cam = Camera.main;
        if (cam == null) return null;
        if (shape == null) shape = GetComponent<CapsuleCollider>();
        Vector3 middle = shape != null && shape.enabled ? shape.bounds.center : transform.position + Vector3.up * transform.lossyScale.y;
        if ((middle - cam.transform.position).sqrMagnitude > checkDistance * checkDistance) return null;
        Vector3 v = cam.WorldToViewportPoint(middle);
        bool inside = v.z > cam.nearClipPlane && v.x > OnScreenMargin && v.x < 1f - OnScreenMargin &&
                      v.y > OnScreenMargin && v.y < 1f - OnScreenMargin;
        return inside ? cam : null;
    }

    // The worn body parts whose box is in the camera's view but that no camera drew in the last frame (switched-off
    // renderers count too), into Hidden. Returns how many.
    int FindHidden(Camera cam)
    {
        FindSkins();
        hidden.Clear();
        GeometryUtility.CalculateFrustumPlanes(cam, frustum);
        foreach (SkinnedMeshRenderer skin in skins)
        {
            if (skin == null || (skin.sharedMesh != null && skin.enabled && skin.isVisible)) continue;   // (no mesh: nothing drawn)
            if (GeometryUtility.TestPlanesAABB(frustum, skin.bounds)) hidden.Add(skin);
        }
        return hidden.Count;
    }

    // The body parts it wears: its switched-on skinned parts (the removed ones are switched off, then gone).
    void FindSkins()
    {
        bool stale = skins.Count == 0;
        foreach (SkinnedMeshRenderer skin in skins)
        {
            if (skin == null || !skin.gameObject.activeInHierarchy) stale = true;
        }
        if (!stale) return;
        skins.Clear();
        GetComponentsInChildren(skins);
    }

    // What brings a skinned part back when it stops being drawn: its box worked out from its real pose every frame
    // (not the one from the file), its links to the bones made again, and its renderer started again.
    static void Mend(List<SkinnedMeshRenderer> parts)
    {
        foreach (SkinnedMeshRenderer skin in parts)
        {
            if (skin == null) continue;
            skin.updateWhenOffscreen = true;
            skin.bones = skin.bones;
            skin.enabled = false;
            skin.enabled = true;
        }
    }

    // "animator on, CullUpdateTransforms | hood drawn | 01_A_body NOT DRAWN (in view), box 0.4 m from the head | ..."
    string BodyReport()
    {
        Animator animator = GetComponentInChildren<Animator>();
        Transform head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        Vector3 headAt = head != null ? head.position : transform.position + Vector3.up * 1.6f * transform.lossyScale.y;
        StringBuilder s = new StringBuilder("animator ");
        if (animator == null) s.Append("none");
        else s.Append(animator.enabled ? "on, " : "OFF, ").Append(animator.cullingMode).Append(animator.isInitialized ? "" : ", NOT started");
        s.Append(" | hood ").Append(hoodLook == null ? "none" : !hood.activeInHierarchy ? "off" : hoodLook.isVisible ? "drawn" : "on, not drawn");
        if (skins.Count == 0) s.Append(" | no worn body parts found");
        foreach (SkinnedMeshRenderer skin in skins)
        {
            if (skin == null) continue;
            int missingBones = 0;
            foreach (Transform bone in skin.bones)
            {
                if (bone == null) missingBones++;
            }
            s.Append(" | ").Append(skin.name.Replace("PT_Male_Armor_", "").Replace("PT_Female_Armor_", ""))
             .Append(skin.enabled ? "" : " OFF")
             .Append(hidden.Contains(skin) ? " NOT DRAWN (in view)" : skin.isVisible ? " drawn" : " not drawn (out of view)")
             .Append(", box ").Append(Vector3.Distance(skin.bounds.center, headAt).ToString("0.0")).Append(" m from the head")
             .Append(skin.sharedMesh == null ? ", NO MESH" : "")
             .Append(skin.rootBone == null ? ", no root bone" : "")
             .Append(missingBones > 0 ? ", " + missingBones + " bones missing" : "")
             .Append(skin.updateWhenOffscreen ? ", box from its pose" : "");
        }
        return s.ToString();
    }

    bool HoodOn => wearsHood && hood != null;

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

    // The styles of this body's pieces: the armor ones (01_a, 05_c) or the plain ones (cloth, naked). Only pieces it can
    // draw count.
    List<string> Styles(bool armored)
    {
        List<string> found = new List<string>();
        foreach (Part p in parts)
        {
            if (p != null && p.part != null && p.armored == armored && HasMesh(p.part) && !found.Contains(p.style)) found.Add(p.style);
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

    // The piece of this style (null = there's none, e.g. a plain helmet, or its part has nothing to draw).
    GameObject PartOf(Piece piece, string style, bool armored)
    {
        if (style == null) return null;
        foreach (Part p in parts)
        {
            if (p != null && p.part != null && p.piece == piece && p.armored == armored && p.style == style && HasMesh(p.part))
                return p.part;
        }
        return null;
    }

    // Any plain piece of this kind it can draw (null = none, e.g. a helmet: there are no plain ones).
    GameObject AnyPlain(Piece piece)
    {
        foreach (Part p in parts)
        {
            if (p != null && p.part != null && p.piece == piece && !p.armored && HasMesh(p.part)) return p.part;
        }
        return null;
    }

    // False for a body part with nothing to draw: Polytope's male prefab has four bare-skin parts whose meshes are
    // missing (Build Human Enemies gives them the FBX's; a prefab built before that still has them empty). Worn, such
    // a part left that piece of the body invisible.
    static bool HasMesh(GameObject part)
    {
        SkinnedMeshRenderer skin = part.GetComponent<SkinnedMeshRenderer>();
        return skin == null || skin.sharedMesh != null;
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

    void KeepOne(List<GameObject> options, GameObject keep)
    {
        foreach (GameObject option in options)
        {
            if (option == null) continue;
            if (option == keep) option.SetActive(true);
            else Remove(option);
        }
    }

    // Takes a part off for good. While the human is still switched off (being dressed: in the Human Spawner's Dressing
    // Room, or in Start for one put in a scene by hand) it's deleted at once (DestroyImmediate), so the body is finished
    // before its animator and body parts start. On a human that's already switched on it's only switched off: no piece
    // of a running body is deleted (see Purpose).
    void Remove(GameObject part)
    {
        if (part == null) return;
        part.SetActive(false);
        if (!gameObject.activeInHierarchy) DestroyImmediate(part);
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
        // true = also while the human is still switched off (the Human Spawner dresses it before it switches on)
        foreach (Collider part in held.GetComponentsInChildren<Collider>(true)) Destroy(part);
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

    // A random one that still exists and can be drawn (null if there's none).
    static GameObject Pick(List<GameObject> options)
    {
        int count = options.Count;
        int start = count > 0 ? Random.Range(0, count) : 0;
        for (int i = 0; i < count; i++)
        {
            GameObject option = options[(start + i) % count];
            if (option != null && HasMesh(option)) return option;
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
        if (HoodOn) s.Append(" | hood");
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
