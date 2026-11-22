/*
First Date: 19/11/2026
Date: 22/11/2026
Author: Tony
Use by: Nothing to set up (code only, no component). ToolInHand (tools, and a bow in the left fist) and HeldItemView
        (every other item) ask it where a character's fist holds things.
Purpose: Works out where a Humanoid character's right fist holds something, from the hand's own bones (the wrist, the
         knuckles, the index and little fingers), so a held tool or item fits any Humanoid character:
           Center    = inside the closed fist
           ThumbSide = the way a handle sticks out of the fist (on the thumb's side)
           Rotation  = turns a held thing so its y (a handle) points out of the thumb side, its x along the fingers
                       and its z out of the palm
         All three are in the hand bone's own space. The knuckles don't move in that space whatever the pose (only the
         fingers turn), so measuring once is enough.
         The left fist (Measure with left = true, for a bow) works the same; its Rotation still turns y to the thumb
         side and x along the fingers (a bow shoots the way the fingers point), which puts z out of the BACK of the
         left hand (it's the right hand's mirror image).
Requirement: HandGrip.cs <-x HeldItemView.cs, ToolInHand.cs
*/

using UnityEngine;

public struct HandGrip
{
    // ===== Fields =====
    public Transform hand;        // the right hand bone (the wrist)
    public Vector3 center;        // inside the closed fist, in the hand's own space
    public Vector3 thumbSide;     // the way a handle sticks out of the fist, in the hand's own space
    public Quaternion rotation;   // a held thing: y (handle) -> thumb side, x -> fingers, z -> out of the palm

    // ===== My methods =====

    // The right fist of this character. False = not a Humanoid with a right hand (nothing can be held).
    public static bool TryMeasure(Animator animator, out HandGrip grip)
    {
        grip = default;
        Transform hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (hand == null) return false;

        grip = Measure(animator, hand);
        return true;
    }

    // The right fist's directions in the hand's own space, from where the knuckles and fingers are.
    public static HandGrip Measure(Animator animator, Transform hand)
    {
        return Measure(animator, hand, false);
    }

    // The same for either fist: left = true reads the left hand's fingers ("hand" = that hand's bone).
    public static HandGrip Measure(Animator animator, Transform hand, bool left)
    {
        Transform middle = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
        Transform index = animator.GetBoneTransform(left ? HumanBodyBones.LeftIndexProximal : HumanBodyBones.RightIndexProximal);
        Transform little = animator.GetBoneTransform(left ? HumanBodyBones.LeftLittleProximal : HumanBodyBones.RightLittleProximal);
        Transform thumb = animator.GetBoneTransform(left ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);
        Transform elbow = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        float defaultLength = 0.09f / Mathf.Max(0.0001f, hand.lossyScale.x);   // about a hand, if there are no finger bones

        // Fingers: from the wrist to the knuckles (no finger bones: straight on from the forearm).
        Vector3 knuckles = middle != null ? hand.InverseTransformPoint(middle.position)
                         : elbow != null ? hand.InverseTransformDirection(hand.position - elbow.position).normalized * defaultLength
                         : Vector3.right * defaultLength;
        Vector3 fingers = knuckles.normalized;
        float length = knuckles.magnitude;

        // The thumb's side: from the little finger to the index finger (or toward the thumb).
        Vector3 side = index != null && little != null ? hand.InverseTransformPoint(index.position) - hand.InverseTransformPoint(little.position)
                     : thumb != null ? hand.InverseTransformPoint(thumb.position)
                     : Vector3.forward;
        Vector3 thumbSide = Vector3.ProjectOnPlane(side, fingers).normalized;
        if (thumbSide.sqrMagnitude < 0.5f) thumbSide = Vector3.Cross(fingers, Vector3.up).normalized;   // an odd rig
        Vector3 palm = Vector3.Cross(fingers, thumbSide);   // the side the fingers close toward (right hand)
        Vector3 inward = left ? -palm : palm;               // the same for either hand (the left one is a mirror image)

        return new HandGrip
        {
            hand = hand,
            center = fingers * (length * 0.55f) + inward * (length * 0.3f),   // inside the closed fist
            thumbSide = thumbSide,
            rotation = Quaternion.LookRotation(palm, thumbSide),            // z -> palm, y (handle) -> thumb side, x -> fingers
        };
    }
}
