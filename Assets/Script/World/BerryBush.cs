/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Turki's berry bush (Assets/Prefabs/ResourceNodes/BerryNode), next to his ResourceNode. Tools > World > Build
        Berry Bushes puts it there and fills in everything under "Look".
Unity Editor: Testing: Play and find a berry bush on the grass (red berries on a leafy bush). Look at it from close by
              (the prompt under the crosshair says "E  Pick berries  x4") and press E (the Interact key): all its berries
              go into your inventory ("+4 Berry") and the bush stays. One berry grows back every Grow Seconds (60), up
              to the bush's own number; until then the prompt counts down to the next one. Punch it or hit it with a
              tool to break it: you get the berries still on it, nothing more. ⋮ (top right of this component) >
              Test: Grow All Now fills it up at once while playing.
Purpose: Makes Turki's berry bush look like a real berry bush, and lets the player pick it without breaking it:
         - Look: each bush wears one of the Yughues bushes in Looks, picked from where it stands (like the rocks: a
           bush made again in the same spot, as the new world does, looks the same), with its 4 levels of detail
           (Leaf Parts, near to far, chosen by Unity's LOD Group), and its Body (the box the player bumps into, hits
           and looks at) sized to that look's leaves.
         - Berries: small red berries in clusters of three on the outside of its leaves, at spots the tool found
           (where a leaf really is, not on the see-through edges of the leaf picture). Each bush has its own number
           (Fewest .. Most Berries) and its own order of spots, so two bushes of the same look still differ.
         - Picking (E, which Turki's ResourceNode watches for): every berry on it goes into the inventory at once, as
           the ResourceNode's Drop Item (Berry). Then they grow back one at a time, one every Grow Seconds, on game
           time (the pause menu stops it; a bush in a hidden part of the new world catches up when it shows again).
         - Breaking (fists or tools until its ResourceNode's health is 0): the berries still on it go into the
           inventory, nothing else (no perk extras).
Requirement: BerryBush.cs x-> ResourceNode.cs, PlayerInventory.cs, ItemData.cs
             BerryBush.cs o-> NotificationFeed.cs, LODGroup (Unity), BoxCollider (Unity)
             BerryBush.cs <-x BerryBushBuilder.cs, ResourceNode.cs
             BerryBush.cs <-o ChestPrompt.cs
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ResourceNode))]
public class BerryBush : MonoBehaviour
{
    // One Yughues bush the berry bush can wear (the tool fills these in).
    [System.Serializable]
    public class Look
    {
        [Tooltip("Its name and the pack's prefab it comes from, e.g. Leafy (P_Bush01).")]
        public string name;

        [Tooltip("Its leaves' meshes, one per level of detail, near to far.")]
        public Mesh[] meshes = new Mesh[0];

        [Tooltip("The leaves' material (the pack's own).")]
        public Material material;

        [Tooltip("Where the leaves sit inside the bush (the pack's own placing: its meshes are 10 times too big, so " +
                 "they're scaled to a tenth).")]
        public Vector3 leavesPosition;
        public Quaternion leavesRotation = Quaternion.identity;
        public Vector3 leavesScale = Vector3.one;

        [Tooltip("The Body box for this look (its middle and size in meters): the thick part of its leaves.")]
        public Vector3 bodyCenter = new Vector3(0f, 0.5f, 0f);
        public Vector3 bodySize = Vector3.one;

        [Tooltip("Where berries can grow on it (found by the tool). Each bush uses as many as it holds, in its own order.")]
        public List<Spot> spots = new List<Spot>();
    }

    // A place for one berry cluster: on a leaf, turned to face out of the bush.
    [System.Serializable]
    public struct Spot
    {
        public Vector3 position;
        public Quaternion turn;
    }

    // ===== Fields =====
    [Header("Berries")]
    [Tooltip("Fewest berries a full bush holds: each bush picks its own number between Fewest and Most (the tool " +
             "copies Turki's Drop Min / Max the first time: 3 - 5, as many as the old bush gave).")]
    [Min(0)]
    [SerializeField] int fewestBerries = 3;

    [Tooltip("Most berries a full bush holds. Not more than its Berry Parts: run Tools > World > Build Berry Bushes " +
             "again after raising it, and it makes that many.")]
    [Min(1)]
    [SerializeField] int mostBerries = 5;

    [Tooltip("Seconds for one picked berry to grow back (game time: the pause menu stops it). 60 = one berry a " +
             "minute, so a bush of 5 is full again 5 minutes after it was picked.")]
    [Min(1f)]
    [SerializeField] float growSeconds = 60f;

    [Tooltip("Message when E is pressed on a bush with no berries yet. Empty = no message.")]
    [SerializeField] string emptyText = "No berries yet: they grow back.";

    [Header("Look (Tools > World > Build Berry Bushes fills these)")]
    [Tooltip("The bushes it can wear; each berry bush picks one from where it stands.")]
    [SerializeField] List<Look> looks = new List<Look>();

    [Tooltip("Its leaves' 4 levels of detail (near to far): each a Mesh Filter with a Mesh Renderer, in its LOD Group.")]
    [SerializeField] MeshFilter[] leafParts = new MeshFilter[0];

    [Tooltip("Its berries: one cluster of three small red berries each, as many as Most Berries.")]
    [SerializeField] MeshRenderer[] berryParts = new MeshRenderer[0];

    [Tooltip("The box the player bumps into, hits and looks at; sized to the look's leaves.")]
    [SerializeField] BoxCollider body;

    const float MessageGap = 2f;   // seconds between two messages from the same bush

    ResourceNode node;          // Turki's: watches for E, takes the hits, knows the Drop Item (Berry)
    int capacity;               // berries this bush holds when full
    int ripe;                   // berries on it now
    float nextBerryAt;          // game time (Time.time) when the next berry grows, while it isn't full
    Coroutine growing;          // shows each berry when it grows
    float lastMessage = -10f;

    public int Berries          // berries on it right now
    {
        get
        {
            CatchUp();
            return ripe;
        }
    }

    public int MostOnIt => capacity;   // berries it holds when full

    public float SecondsToNextBerry    // until the next berry grows (0 while it's full)
    {
        get
        {
            CatchUp();
            return ripe >= capacity ? 0f : Mathf.Max(0f, nextBerryAt - Time.time);
        }
    }

    public float PickRange => node != null ? node.CollectRange : 3f;   // meters: how close the player must be for E

    // ===== Unity messages (Unity calls these) =====

    // Awake: the look and the berries are chosen when the bush appears (placed in the scene, spawned by ObjectScatter
    // or made again by the new world), before anything else on it starts.
    void Awake()
    {
        node = GetComponent<ResourceNode>();
        int seed = SeedFor(transform.position);
        Look look = looks.Count > 0 ? looks[(seed & int.MaxValue) % looks.Count] : null;
        if (look != null) Wear(look);
        PlaceBerries(look, new System.Random(seed));
        ripe = capacity;   // a new bush is full
        Show();

        LODGroup levels = GetComponent<LODGroup>();
        if (levels != null) levels.RecalculateBounds();   // its look changed: Unity measures it again for the levels of detail
    }

    // A bush switched back on (a hidden part of the new world showing again) catches up on the berries that grew meanwhile.
    void OnEnable()
    {
        CatchUp();
        Show();
        KeepGrowing();
    }

    // Stops the growing timer (Unity stops it by itself when the object is switched off, but not when only this
    // component is), so OnEnable never starts a second one.
    void OnDisable()
    {
        if (growing != null) StopCoroutine(growing);
        growing = null;
    }

    [ContextMenu("Test: Grow All Now")]
    void TestGrowAll()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Press Play first.");
            return;
        }
        ripe = capacity;
        Show();
    }

    // ===== My methods =====

    // E on this bush (Turki's ResourceNode calls this): every berry on it goes into the inventory, the bush stays.
    public void Pick()
    {
        CatchUp();
        if (ripe <= 0)
        {
            Tell(emptyText);
            return;
        }
        if (!Give(ripe)) return;

        bool wasFull = ripe >= capacity;
        ripe = 0;
        if (wasFull) nextBerryAt = Time.time + growSeconds;   // the first one starts growing now (else the one growing keeps on)
        Show();
        KeepGrowing();
    }

    // The bush broke (Turki's ResourceNode calls this just before it's removed): the berries still on it go into the
    // inventory, nothing more.
    public void GiveLeft()
    {
        CatchUp();
        if (ripe > 0 && Give(ripe)) ripe = 0;
    }

    // Puts this many of the ResourceNode's Drop Item (Berry) into the inventory, with its "+4 Berry" message.
    bool Give(int amount)
    {
        ItemData berry = node != null ? node.DropItem : null;
        PlayerInventory inventory = PlayerInventory.Instance;
        if (berry == null || inventory == null)
        {
            Debug.LogWarning(name + ": its ResourceNode has no Drop Item, or there's no PlayerInventory in the scene.", this);
            return false;
        }
        if (inventory.Add(berry.Id, berry.DisplayName, berry.GetIcon(), amount, berry)) return true;
        Tell("Your inventory is full.");
        return false;
    }

    // Its leaves become this look: meshes, material and placing, and the Body box.
    void Wear(Look look)
    {
        for (int i = 0; i < leafParts.Length; i++)
        {
            MeshFilter part = leafParts[i];
            if (part == null) continue;
            // a look with fewer levels uses its farthest one for the rest
            Mesh mesh = look.meshes.Length > 0 ? look.meshes[Mathf.Min(i, look.meshes.Length - 1)] : null;
            part.sharedMesh = mesh;
            part.transform.localPosition = look.leavesPosition;
            part.transform.localRotation = look.leavesRotation;
            part.transform.localScale = look.leavesScale;
            MeshRenderer leaves = part.GetComponent<MeshRenderer>();
            if (leaves == null) continue;
            leaves.enabled = mesh != null;
            if (look.material != null) leaves.sharedMaterial = look.material;
        }
        if (body != null)
        {
            body.center = look.bodyCenter;
            body.size = look.bodySize;
        }
    }

    // How many berries this bush holds and where they sit: its own number and its own order of the look's spots.
    void PlaceBerries(Look look, System.Random rng)
    {
        int spots = look != null ? look.spots.Count : berryParts.Length;   // no look: the berries stay where the prefab has them
        int most = Mathf.Min(mostBerries, berryParts.Length, spots);
        int fewest = Mathf.Clamp(fewestBerries, 0, most);
        capacity = most > 0 ? rng.Next(fewest, most + 1) : 0;
        if (look == null) return;

        int[] order = new int[spots];
        for (int i = 0; i < spots; i++) order[i] = i;
        for (int i = spots - 1; i > 0; i--)   // shuffled, so bushes of the same look have their berries in different places
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        for (int i = 0; i < berryParts.Length && i < spots; i++)
        {
            if (berryParts[i] == null) continue;
            Spot spot = look.spots[order[i]];
            berryParts[i].transform.localPosition = spot.position;
            berryParts[i].transform.localRotation = spot.turn;
        }
    }

    // The berries on it are drawn, the rest not.
    void Show()
    {
        for (int i = 0; i < berryParts.Length; i++)
        {
            if (berryParts[i] != null) berryParts[i].enabled = i < ripe;
        }
    }

    // Counts the berries that grew since it was last looked at (one every Grow Seconds).
    void CatchUp()
    {
        while (ripe < capacity && Time.time >= nextBerryAt)
        {
            ripe++;
            nextBerryAt += growSeconds;
        }
    }

    // Starts the timer that shows each berry when it grows (only while it isn't full and is switched on).
    void KeepGrowing()
    {
        if (growing == null && ripe < capacity && isActiveAndEnabled) growing = StartCoroutine(Growing());
    }

    IEnumerator Growing()
    {
        while (ripe < capacity)
        {
            float wait = nextBerryAt - Time.time;
            if (wait > 0f) yield return new WaitForSeconds(wait);
            else yield return null;
            CatchUp();
            Show();
        }
        growing = null;
    }

    // A message at the bottom left (Tony's NotificationFeed), at most every Message Gap seconds.
    void Tell(string words)
    {
        if (string.IsNullOrEmpty(words) || Time.unscaledTime < lastMessage + MessageGap) return;
        lastMessage = Time.unscaledTime;
        NotificationFeed.Show(words);
    }

    // A number from where the bush stands (rounded to 10 cm), the same way RockLook picks a rock's shape: the same
    // spot always gives the same look, number of berries and order of spots.
    static int SeedFor(Vector3 at)
    {
        unchecked
        {
            return Mathf.RoundToInt(at.x * 10f) * 73856093 ^ Mathf.RoundToInt(at.y * 10f) * 19349663 ^
                   Mathf.RoundToInt(at.z * 10f) * 83492791;
        }
    }
}
