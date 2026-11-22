/*
First Date: 19/11/2026
Date: 19/11/2026
Author: Tony
Use by: Nothing to set up: TreeFall adds it to the look-only copy of a tree that was just chopped down.
Purpose: Plays one tree's fall: it tips over from the foot of its trunk toward the given direction, slowly at first and
         then faster (like a real falling tree), with wood chips from the cut. When it lands, brown chips burst up all
         along the trunk; then it breaks apart into chips, shrinking toward the middle of the trunk, and removes itself
         (the chips go once the last one has faded).
Requirement: FallingTree.cs <-x TreeFall.cs
*/

using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FallingTree : MonoBehaviour
{
    // How the fall looks (TreeFall's Inspector values, copied: the tree itself is gone by now).
    public struct Settings
    {
        public float fallSeconds, fallAngle, lieSeconds, breakSeconds;
        public int cutChips, landChips;
        public float breakRate, chipSpeed;
        public Color darkWood, lightWood;
    }

    // ===== Fields =====
    Settings how;
    Vector3 direction;       // flat on the ground: the way the top falls
    float height;            // the tree's height in meters
    ParticleSystem chips;    // the chips' own copy (can be missing: then it only falls)

    // ===== My methods =====

    // TreeFall calls this right after making the copy.
    public void Begin(Settings settings, Vector3 fallDirection, float treeHeight, ParticleSystem chipSystem)
    {
        how = settings;
        direction = fallDirection;
        height = treeHeight;
        chips = chipSystem;
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        Vector3 foot = transform.position;
        Quaternion standing = transform.rotation;
        Vector3 axis = Vector3.Cross(Vector3.up, direction);   // turning around this tips the top toward direction

        EmitAlong(foot + Vector3.up * 0.3f, Vector3.up, 0.6f, how.cutChips, 0.6f);   // chips from the cut

        // Falls: slowly at first, then faster and faster (like gravity pulling a tree over).
        float time = 0f;
        while (time < how.fallSeconds)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / how.fallSeconds);
            transform.rotation = Quaternion.AngleAxis(how.fallAngle * k * k, axis) * standing;
            yield return null;
        }

        // Lands: chips burst up all along the trunk.
        Vector3 along = transform.up;   // the fallen trunk's direction, from the foot to the top
        EmitAlong(foot, along, height, how.landChips, 1f);
        if (how.lieSeconds > 0f) yield return new WaitForSeconds(how.lieSeconds);

        // Breaks apart: shrinks toward the middle of the trunk while chips come off it.
        Vector3 middle = foot + along * (height * 0.5f);
        Vector3 fullSize = transform.localScale;
        float owed = 0f;   // part-chips carried to the next frame, so the rate is right at any frame rate
        time = 0f;
        while (time < how.breakSeconds)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / how.breakSeconds);
            float scale = 1f - k * k;   // slowly at first, quickly at the end
            transform.localScale = fullSize * Mathf.Max(0.001f, scale);
            transform.position = middle + (foot - middle) * scale;

            owed += how.breakRate * Time.deltaTime;
            int count = (int)owed;
            owed -= count;
            float length = height * Mathf.Max(0.05f, scale);
            EmitAlong(middle - along * (length * 0.5f), along, length, count, 0.6f);
            yield return null;
        }

        if (chips != null) Destroy(chips.gameObject, chips.main.startLifetime.constantMax + 0.5f);
        Destroy(gameObject);
    }

    // count chips at random points on the line from "from", "length" meters along "along", flying up and outward,
    // each a random brown between dark and light.
    void EmitAlong(Vector3 from, Vector3 along, float length, int count, float speedScale)
    {
        if (chips == null || count <= 0) return;
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        float spread = Mathf.Clamp(length * 0.04f, 0.15f, 0.6f);   // how far beside the trunk (the branches are wider)
        for (int i = 0; i < count; i++)
        {
            emit.position = from + along * (length * Random.value) + Random.insideUnitSphere * spread;
            emit.velocity = (Random.insideUnitSphere * 0.8f + Vector3.up * Random.Range(0.6f, 1.4f)) * how.chipSpeed * speedScale;
            emit.startColor = Color.Lerp(how.darkWood, how.lightWood, Random.value);
            chips.Emit(emit, 1);
        }
    }
}
