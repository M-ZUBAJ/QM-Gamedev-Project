/*
First Date: 9/10/2026
Date: 9/10/2026
Author: Tony
Use by: Nothing to set up. ItemDropper makes these with DroppedStack.Spawn(...) when the player drops items.
Unity Editor: Nothing to set. Its look is chosen on the player's ItemDropper (Drop Style and the sizes / colors).
              Needs TMP Essentials for the amount text.
Purpose: A dropped stack lying in the world, made in code (no prefab needed). Two looks:
         - Icon (like Minecraft): the item's picture floating above the ground, always facing the camera,
           bobbing up and down, with the amount under it.
         - Bundle: a small colored box with the item's picture and the amount on its four sides. It tumbles when thrown.
         The whole stack is ONE object: picking it up (F, or touching it for AutoPickup items) gives the whole amount.
         Two stacks of the same item that lie close together merge into one (like Minecraft).
         It can disappear by itself after a while (Despawn), like Minecraft's 5 minutes.
Requirement: DroppedStack.cs x-> ItemPickup.cs, PlayerInventory.cs
             DroppedStack.cs <-x ItemDropper.cs
*/

using TMPro;
using UnityEngine;

// How a dropped stack looks. New looks get a new line here, and then show in ItemDropper's dropdown.
public enum DropStyle
{
    Icon,     // the item's picture floating above the ground (like Minecraft)
    Bundle,   // a small box with the item's picture and amount on its sides
}

[DisallowMultipleComponent]
public class DroppedStack : MonoBehaviour
{
    // How a new stack should look and behave (filled in by ItemDropper from its Inspector settings).
    public struct Look
    {
        public DropStyle style;
        public float iconSize;         // Icon: width of the picture, in meters
        public float bundleSize;       // Bundle: width of the box, in meters
        public Color bundleColor;      // Bundle: the box's color
        public float mergeRadius;      // stacks of the same item this close merge (0 = never)
        public float despawnSeconds;   // disappears after this long (0 = never)
    }

    // ===== Fields =====
    const float BobHeight = 0.08f;     // Icon: how far it floats up and down, in meters
    const float BobSpeed = 2f;         // Icon: bobs per second (x 2 pi)
    const float MergeCheckEvery = 0.5f;

    ItemPickup pickup;                 // what F / touching reads: the item and the amount
    Look look;
    Transform visual;                  // Icon: the floating picture (turned toward the camera)
    TextMeshPro[] counts;              // the amount labels (one for Icon, four for Bundle)
    float visualBaseY;
    float bornTime;
    float nextMergeCheck;
    bool merged;                       // already merged into another stack (about to be removed)
    Camera cam;

    public string ItemId => pickup.ItemId;
    public int Amount => pickup.Amount;

    // ===== Unity messages (Unity calls these) =====

    void Start()
    {
        cam = Camera.main;
        bornTime = Time.time;
        nextMergeCheck = Time.time + MergeCheckEvery;
    }

    void Update()
    {
        if (look.despawnSeconds > 0f && Time.time - bornTime >= look.despawnSeconds)
        {
            Destroy(gameObject);   // lay there too long (like Minecraft's 5 minutes)
            return;
        }

        if (look.mergeRadius > 0f && Time.time >= nextMergeCheck)
        {
            nextMergeCheck = Time.time + MergeCheckEvery;
            MergeNearby();
        }
    }

    // LateUpdate: after the camera moved this frame, so the picture faces it exactly.
    void LateUpdate()
    {
        if (visual == null) return;   // Bundle: nothing floats

        Vector3 p = visual.localPosition;
        p.y = visualBaseY + Mathf.Sin((Time.time - bornTime) * BobSpeed * Mathf.PI) * BobHeight;
        visual.localPosition = p;

        if (cam != null) visual.rotation = cam.transform.rotation;   // face the camera, never mirrored
    }

    // ===== My methods =====

    // Makes a dropped stack of "count" of this inventory stack at "position". Returns it (or null if nothing to drop).
    public static DroppedStack Spawn(PlayerInventory.Slot stack, int count, Vector3 position, Look look)
    {
        if (stack == null || stack.IsEmpty || count <= 0) return null;

        // Named exactly after the Item Id: an item without Item Data uses its object's name as its Item Id.
        GameObject root = new GameObject(stack.id);
        root.transform.position = position;

        DroppedStack dropped = root.AddComponent<DroppedStack>();
        dropped.look = look;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;   // smooth on screen
        rb.mass = 0.2f;

        if (look.style == DropStyle.Bundle) dropped.BuildBundle(stack.icon);
        else dropped.BuildIcon(stack.icon, rb);

        // Last: ItemPickup's Awake thickens a too-thin collider, so the collider must already be there.
        dropped.pickup = root.AddComponent<ItemPickup>();
        dropped.pickup.Setup(stack.data, count);
        dropped.pickup.SetIcon(stack.icon);   // the inventory picture stays the same when it's picked up again
        dropped.RefreshCount();
        return dropped;
    }

    // Icon: a small ball collider to lie on the ground, and the picture + amount floating above it.
    void BuildIcon(Sprite icon, Rigidbody rb)
    {
        rb.freezeRotation = true;   // the ball must not roll away: it only carries the floating picture

        float size = Mathf.Max(0.05f, look.iconSize);
        SphereCollider ball = gameObject.AddComponent<SphereCollider>();
        ball.radius = size * 0.4f;

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        visualBaseY = size * 0.5f;   // a bit above the ground
        visual.localPosition = new Vector3(0f, visualBaseY, 0f);

        AddPicture(visual, icon, size, Vector3.zero, 0);
        counts = new[] { AddCount(visual, size, new Vector3(size * 0.35f, -size * 0.4f, -0.01f), 1) };
    }

    // Bundle: a colored box with the picture and amount on its four sides.
    void BuildBundle(Sprite icon)
    {
        float size = Mathf.Max(0.05f, look.bundleSize);
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.size = Vector3.one * size;

        // The box itself: Unity's cube, without its own collider (the root has one), in the chosen color.
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Box";
        Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(transform, false);
        body.transform.localScale = Vector3.one * size;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_Color", look.bundleColor);       // Built-in Render Pipeline
        block.SetColor("_BaseColor", look.bundleColor);   // URP
        body.GetComponent<Renderer>().SetPropertyBlock(block);

        // One face per side: picture in the middle, amount in the bottom right corner.
        counts = new TextMeshPro[4];
        for (int i = 0; i < 4; i++)
        {
            Transform face = new GameObject("Face " + (i + 1)).transform;
            face.SetParent(transform, false);
            face.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
            face.localPosition = face.localRotation * new Vector3(0f, 0f, -size * 0.5f - 0.002f);   // just outside the box

            AddPicture(face, icon, size * 0.7f, Vector3.zero, 0);
            counts[i] = AddCount(face, size, new Vector3(size * 0.3f, -size * 0.32f, -0.001f), 1);
        }
    }

    // The item's picture as a Sprite Renderer, "width" meters wide.
    static void AddPicture(Transform parent, Sprite icon, float width, Vector3 localPos, int order)
    {
        if (icon == null) return;

        GameObject picture = new GameObject("Icon");
        picture.transform.SetParent(parent, false);
        picture.transform.localPosition = localPos;

        SpriteRenderer sr = picture.AddComponent<SpriteRenderer>();
        sr.sprite = icon;
        sr.sortingOrder = order;
        float spriteWidth = icon.bounds.size.x;   // in the sprite's own units
        if (spriteWidth > 0f) picture.transform.localScale = Vector3.one * (width / spriteWidth);
    }

    // The amount as 3D text (TextMeshPro), sized to the picture.
    static TextMeshPro AddCount(Transform parent, float size, Vector3 localPos, int order)
    {
        GameObject label = new GameObject("Count");
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPos;

        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.fontSize = 3f * size;   // 3D text: font size 1 is about 0.1 m tall
        text.alignment = TextAlignmentOptions.BottomRight;
        text.color = Color.white;
        text.outlineWidth = 0.25f;   // dark edge, readable on any ground
        text.outlineColor = Color.black;
        text.rectTransform.sizeDelta = new Vector2(size, size * 0.5f);
        text.sortingOrder = order;
        return text;
    }

    // Shows the amount (no number for just 1, like Minecraft).
    void RefreshCount()
    {
        if (counts == null) return;
        string s = Amount > 1 ? Amount.ToString() : "";
        foreach (TextMeshPro t in counts)
        {
            if (t != null) t.text = s;
        }
    }

    // A stack of the same item lying close by: the older one takes this one's items, this one disappears.
    void MergeNearby()
    {
        if (merged) return;

        foreach (Collider c in Physics.OverlapSphere(transform.position, look.mergeRadius, Physics.AllLayers,
                                                     QueryTriggerInteraction.Collide))
        {
            DroppedStack other = c.GetComponentInParent<DroppedStack>();
            if (other == null || other == this || other.merged || other.ItemId != ItemId) continue;

            // Both see each other: only the newer one gives itself away (the older one keeps its place).
            if (other.bornTime > bornTime || (other.bornTime == bornTime && other.GetInstanceID() > GetInstanceID())) continue;

            other.pickup.SetAmount(other.Amount + Amount);
            other.RefreshCount();
            other.bornTime = Mathf.Max(other.bornTime, bornTime);   // the despawn clock restarts with the newest items
            merged = true;
            Destroy(gameObject);
            return;
        }
    }
}
