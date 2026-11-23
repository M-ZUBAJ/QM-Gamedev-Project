/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Every human enemy's top object. Tools > Enemies > Build Human Enemies puts it there, with its type's name and
        color.
Unity Editor: Label = the name on the tag ("Type 2"), Color = its color. It shows only while the scene's GameRules >
              Human Tags (Testing) is ticked: tick it before or while playing (save the scene to keep it for games
              started from the title screen).
              Testing: tick Human Tags and play the new world: a tag floats over every human's head ("Type 1" in
              blue, "Type 2" in orange, "Type 3" in green) with what it's doing under it.
Purpose: A test name tag over a human enemy: its type in its color and (Show Doing) what it's doing right now
         (HumanBrain's Doing, e.g. "Watching the player"), turned toward the camera and the same size on screen near or
         far, up to Show Distance. Only while the test rule GameRules.HumanTags is on, so players never see it. Made the
         first time it shows, as an object of its own that follows the human (not its child: nothing is added to a
         human's body while it plays, see HumanGear); hidden while the human dies, gone with it.
Requirement: HumanTag.cs x-> GameRules.cs
             HumanTag.cs o-> HumanBrain.cs, EnemyCombat.cs, CapsuleCollider (Unity)
             HumanTag.cs <-x HumanEnemyBuilder.cs
*/

using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class HumanTag : MonoBehaviour
{
    // ===== Fields =====
    [Tooltip("The name on the tag (the tool sets it to the human's type, e.g. Type 2).")]
    [SerializeField] string label = "Human";

    [Tooltip("The name's color (the tool gives each type its own: Type 1 blue, Type 2 orange, Type 3 green).")]
    [SerializeField] Color color = Color.white;

    [Tooltip("On: a smaller line under the name says what it's doing (HumanBrain > Doing).")]
    [SerializeField] bool showDoing = true;

    [Tooltip("On: the tag shows through trees, rocks and walls (easier to find humans while testing).")]
    [SerializeField] bool throughWalls = true;

    [Range(0f, 2f)]
    [Tooltip("Meters above the top of its head.")]
    [SerializeField] float above = 0.35f;

    [Range(5f, 200f)]
    [Tooltip("Tags farther from the camera than this (meters) are hidden.")]
    [SerializeField] float showDistance = 60f;

    [Range(0.5f, 6f)]
    [Tooltip("How big the text is (TextMeshPro font size at 6 m from the camera; it keeps that size on screen nearer or farther).")]
    [SerializeField] float size = 2.2f;

    const float TextEvery = 0.25f;        // seconds between updates of the "doing" line
    const float SizeDistance = 6f;        // at this distance the text has Size; nearer or farther it's scaled to look the same
    const float SmallestScale = 0.6f;     // ... but never smaller than this (very close)

    TextMeshPro text;
    Material ownMaterial;   // the copy TextMeshPro makes for the outline (TextMeshPro doesn't delete it: we do)
    HumanBrain brain;
    EnemyCombat combat;
    float height = 1.8f;    // its capsule's height (meters, before its own scale)
    float nextText;

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        brain = GetComponent<HumanBrain>();
        combat = GetComponent<EnemyCombat>();
        CapsuleCollider shape = GetComponent<CapsuleCollider>();
        if (shape != null) height = shape.center.y + shape.height * 0.5f;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        bool show = GameRules.HumanTags && cam != null && (combat == null || combat.currentHealth > 0f) &&
                    (transform.position - cam.transform.position).sqrMagnitude <= showDistance * showDistance;
        if (!show)
        {
            if (text != null && text.gameObject.activeSelf) text.gameObject.SetActive(false);
            return;
        }
        if (text == null) Make();
        if (!text.gameObject.activeSelf)
        {
            text.gameObject.SetActive(true);
            nextText = 0f;
        }

        Vector3 top = transform.position + Vector3.up * (height * transform.lossyScale.y + above);
        text.transform.position = top;
        text.transform.rotation = Quaternion.LookRotation(top - cam.transform.position);   // reads the right way round
        float scale = Mathf.Max(SmallestScale, Vector3.Distance(top, cam.transform.position) / SizeDistance);
        text.transform.localScale = Vector3.one * scale;

        if (Time.time < nextText) return;
        nextText = Time.time + TextEvery;
        text.text = Words();
    }

    void OnDisable()
    {
        if (text != null) text.gameObject.SetActive(false);   // a switched-off human shows no tag
    }

    void OnDestroy()
    {
        if (text != null) Destroy(text.gameObject);   // its own object: it goes with the human
        if (ownMaterial != null) Destroy(ownMaterial);
    }

    // ===== My methods =====

    // The tag: a 3D TextMeshPro object of its own (not a child of the human), bold, outlined (readable on the bright
    // desert and at night), centered.
    void Make()
    {
        GameObject go = new GameObject("Human Tag (" + name + ")");
        text = go.AddComponent<TextMeshPro>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.color = Color.white;
        text.outlineWidth = 0.25f;                     // this makes its own material copy...
        text.outlineColor = new Color32(0, 0, 0, 220);
        text.isOverlay = throughWalls;                 // ... which this then changes (not the font's shared material)
        text.rectTransform.sizeDelta = new Vector2(6f, 1.5f);
        Material own = text.fontSharedMaterial;
        ownMaterial = text.font != null && own == text.font.material ? null : own;
        text.text = Words();
    }

    // "<b>Type 2</b>" in its color, and under it what it's doing (smaller).
    string Words()
    {
        string name = "<b><color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + label + "</color></b>";
        if (!showDoing || brain == null || string.IsNullOrEmpty(brain.Doing)) return name;
        return name + "\n<size=55%>" + brain.Doing + "</size>";
    }
}
