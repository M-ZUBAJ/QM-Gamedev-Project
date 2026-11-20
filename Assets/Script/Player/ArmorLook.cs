/*
First Date: 20/11/2026
Date: 20/11/2026
Author: Tony
Use by: An animated character with armor parts inside it: the root of Assets/Prefabs/Player/PlayerCharacter.prefab (so
        the Player's Character, the first-person hands and the Gear page's preview all have it).
        Tools > Items > Build Plate Armor and Tools > Player > Add Character Model put it there.
Unity Editor: Nothing to set up: the armor's Item Data says which part to show (Armor > Piece) and which to hide (Hides).
              Skin Under Clothes is filled in for Blink's character; Tools > Items > Build Plate Armor adds its trimmed
              copies (Trims). Only add a body part to a row's Hides if that clothing covers it from every side, or the
              player can see through the character there.
              Testing: Play, put armor on from Tab > Gear: it shows on the character (press the camera key for third
              person), on the first-person hands (gloves) and on the Gear page's preview. Turn the preview: the shirt's
              shoulders, the gloves and the boots don't flicker.
Purpose: Shows the armor the player wears (PlayerArmor) on this character. Blink's character has its armor already
         inside it, switched off: wearing the Iron Helmet switches its PlateSet1_Helmet part on (and switches off what
         it covers, e.g. the hair). Taking it off puts every part back exactly how it was.
         It also hides the skin under the clothes. Blink's clothes sit right on the body's skin (the shirt's sleeves are
         the arm's skin pushed out a hair, in places not at all), and two surfaces in the same spot take turns being
         drawn: the shoulders flickered between the shirt and the skin many times a second (z-fighting). Skin that a
         piece covers completely is switched off while the piece shows (the arms and chest under the shirt, the legs
         under trousers, the feet in boots). Skin it covers only partly (the hands in the plate gloves, which leave the
         fingertips out) is drawn with a copy of its mesh without the faces under the piece.
         The first-person hands only ever draw their arms, hands and gloves (FirstPersonHands), so only the gloves show
         there, and their arms stay even under the shirt (which they don't draw); the body hides everything but its
         shadow in first person (PlayerAnimator), so a helmet never blocks the view.
Requirement: ArmorLook.cs x-> PlayerArmor.cs, ItemData.cs, ArmorStats.cs
             ArmorLook.cs o-> FirstPersonHands.cs
             ArmorLook.cs <-x ArmorBuilder.cs, PlayerCharacterBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ArmorLook : MonoBehaviour
{
    // One piece of clothing or armor and the skin under it.
    [System.Serializable]
    public class SkinUnder
    {
        [Tooltip("A clothing or armor part of the character, by its name (e.g. Starter_Chest, PlateSet1_Gloves). The rest " +
                 "of this row only counts while that part shows.")]
        public string clothing = "";

        [Tooltip("Body parts it covers completely: switched off while it shows (e.g. the Arms and Chest under the shirt). " +
                 "Only list a part it covers from every side, or the player can see through the character there.")]
        public string[] hides = new string[0];

        [Tooltip("Body parts it covers only partly (e.g. the Hands in gloves that leave the fingertips out): while it shows, " +
                 "they're drawn with a copy of their mesh without the faces under it. Tools > Items > Build Plate Armor makes these.")]
        public List<TrimmedSkin> trims = new List<TrimmedSkin>();

        public SkinUnder() { }

        public SkinUnder(string clothing, params string[] hides)
        {
            this.clothing = clothing;
            this.hides = hides;
        }
    }

    // A body part's mesh without the faces under one piece of clothing.
    [System.Serializable]
    public class TrimmedSkin
    {
        [Tooltip("The body part's name (e.g. Hands).")]
        public string part = "";

        [Tooltip("The mesh the part normally has. The copy is only used while the part has this one (the first-person " +
                 "hands' arms and hands have meshes of their own, and copies of their own).")]
        public Mesh full;

        [Tooltip("The same mesh without the faces under the clothing.")]
        public Mesh trimmed;
    }

    // Blink's clothes and armor and the skin each one covers from every side (measured on the model, 20/11/2026). Not
    // the Arms under the chestplate (the tops of the shoulders show through it) or the Hands in the gloves (the
    // fingertips show): those get trimmed copies instead (Tools > Items > Build Plate Armor).
    public static List<SkinUnder> BlinkSkin() => new List<SkinUnder>
    {
        new SkinUnder("Starter_Chest", "Arms", "Chest"),
        new SkinUnder("Starter_Pants", "Legs"),
        new SkinUnder("Starter_Boots", "Feet"),
        new SkinUnder("PlateSet1_Chest", "Chest"),
        new SkinUnder("PlateSet1_Pants", "Legs"),
        new SkinUnder("PlateSet1_Boots", "Feet"),
    };

    // ===== Fields =====
    [Header("Skin under clothes")]
    [Tooltip("The body's skin under each piece of clothing or armor. Blink's clothes sit right on the skin, so where both " +
             "are drawn they take turns showing and flicker. Filled in for Blink's character; Tools > Items > Build Plate " +
             "Armor adds the trimmed copies.")]
    [SerializeField] List<SkinUnder> skinUnderClothes = BlinkSkin();

    readonly Dictionary<string, GameObject> parts = new Dictionary<string, GameObject>();   // every part of the model, by name
    readonly Dictionary<GameObject, bool> startedOn = new Dictionary<GameObject, bool>();     // parts this changed, and how they were
    readonly Dictionary<SkinnedMeshRenderer, Mesh> ownMesh = new Dictionary<SkinnedMeshRenderer, Mesh>();   // parts drawn with a trimmed copy, and their own mesh
    FirstPersonHands hands;   // only on the first-person copy: it draws nothing but its arms, hands and gloves

    public IReadOnlyList<SkinUnder> SkinUnderClothes => skinUnderClothes;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        foreach (Transform part in GetComponentsInChildren<Transform>(true))
        {
            if (!parts.ContainsKey(part.name)) parts[part.name] = part.gameObject;   // the first one if two share a name
        }
        hands = GetComponentInChildren<FirstPersonHands>(true);
    }

    void OnEnable()
    {
        PlayerArmor.Changed += Refresh;
        Refresh();   // armor may have changed while this was switched off (e.g. the Gear page's preview)
    }

    void OnDisable()
    {
        PlayerArmor.Changed -= Refresh;
    }

    // ===== My methods =====

    // Every part back to how it was, then each worn piece on and what it covers off, then the skin under the clothes
    // that now show. All in one go, so nothing shows half-changed.
    public void Refresh()
    {
        foreach (KeyValuePair<GameObject, bool> part in startedOn)
        {
            if (part.Key != null) part.Key.SetActive(part.Value);
        }
        foreach (KeyValuePair<SkinnedMeshRenderer, Mesh> part in ownMesh)
        {
            if (part.Key != null) SetMesh(part.Key, part.Value);
        }
        ownMesh.Clear();

        PlayerArmor armor = PlayerArmor.Current;
        if (armor != null)
        {
            foreach (ArmorSlot slot in PlayerArmor.Slots)
            {
                ItemData item = armor.WornIn(slot);
                if (item == null || !item.IsArmor) continue;
                Switch(item.Armor.piece, true);
                if (item.Armor.hides == null) continue;
                foreach (string covered in item.Armor.hides) Switch(covered, false);
            }
        }
        HideSkin();
    }

    // For each piece of clothing that shows: the skin it covers off, the skin it partly covers trimmed.
    void HideSkin()
    {
        if (skinUnderClothes == null) return;
        foreach (SkinUnder row in skinUnderClothes)
        {
            if (row == null || !Shows(row.clothing)) continue;

            // The first-person copy only draws arms, hands and gloves: its arms must stay under the shirt it doesn't draw.
            bool clothingDrawn = hands == null || hands.Draws(row.clothing);
            if (row.hides != null)
            {
                foreach (string skin in row.hides)
                {
                    if (!clothingDrawn && hands.Draws(skin)) continue;
                    Switch(skin, false);
                }
            }
            if (row.trims == null) continue;
            foreach (TrimmedSkin trim in row.trims) Trim(trim);
        }
    }

    // Is the part with this name switched on (and everything above it)?
    bool Shows(string partName)
    {
        return !string.IsNullOrEmpty(partName) && parts.TryGetValue(partName, out GameObject part) && part != null &&
               part.activeInHierarchy;
    }

    // Draws the body part with its trimmed copy, if it still has the mesh the copy was made from.
    void Trim(TrimmedSkin trim)
    {
        if (trim == null || trim.full == null || trim.trimmed == null) return;
        if (!parts.TryGetValue(trim.part, out GameObject part) || part == null) return;
        SkinnedMeshRenderer skin = part.GetComponent<SkinnedMeshRenderer>();
        if (skin == null) return;

        Mesh own = ownMesh.TryGetValue(skin, out Mesh before) ? before : skin.sharedMesh;   // trimmed by another row already?
        if (own != trim.full) return;   // a different mesh (e.g. the first-person hands' own): not this copy
        ownMesh[skin] = own;
        SetMesh(skin, trim.trimmed);
    }

    // Swaps a part's mesh. The copy has the same corners, so the part keeps its box (the space Unity checks to know
    // whether it's on screen), instead of Unity measuring a new one.
    static void SetMesh(SkinnedMeshRenderer skin, Mesh mesh)
    {
        Bounds box = skin.localBounds;
        skin.sharedMesh = mesh;
        skin.localBounds = box;
    }

    // Switches the part with this name on or off (remembering how it was the first time, to put it back later).
    void Switch(string partName, bool on)
    {
        if (string.IsNullOrEmpty(partName) || !parts.TryGetValue(partName, out GameObject part) || part == null) return;
        if (!startedOn.ContainsKey(part)) startedOn[part] = part.activeSelf;
        part.SetActive(on);
    }
}
