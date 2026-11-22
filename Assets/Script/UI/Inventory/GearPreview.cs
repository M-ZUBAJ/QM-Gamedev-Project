/*
First Date: 20/11/2026
Date: 22/11/2026
Author: Tony
Use by: The Raw Image showing the character on the left of the inventory's Gear page (GearPage > Preview > Character).
        Tools > UI > Build Gear Page puts it there and fills in Character Prefab.
Unity Editor: Character Prefab = Assets/Prefabs/Player/PlayerCharacter (the player's look, with its armor parts inside).
              Preview Layer = a layer only the preview uses (29; 30 is the first-person hands', 31 IconCapture's).
              Testing: Play, Tab > Gear: the character stands there, wearing what the player wears. Drag it with the
              mouse to turn it round.
Purpose: A live picture of the player's character for the Gear page. When the page first shows, it puts a copy of the
         character in a little studio far below the world (its own camera, its own light, on its own layer, so the
         world never sees it and the sun, the moon and the night never light it), standing in its idle pose. The copy
         wears the same armor as the player (its ArmorLook listens to PlayerArmor). Every frame the page is open, the
         studio's camera takes a picture into a texture shown by this Raw Image (with an even light from everywhere,
         like IconCapture). The page closed = the studio sleeps (nothing is drawn or animated).
Requirement: GearPreview.cs x-> RawImage (Unity)
             GearPreview.cs o-> PlayerAnimator.cs, ToolInHand.cs, FirstPersonHands.cs
             GearPreview.cs <-x GearPageBuilder.cs
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;   // SphericalHarmonicsL2 (the light from everywhere), ShadowCastingMode
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
[DisallowMultipleComponent]
public class GearPreview : MonoBehaviour, IDragHandler
{
    // ===== Fields =====
    [Tooltip("The character to show: Assets/Prefabs/Player/PlayerCharacter (with its armor parts inside it).")]
    [SerializeField] GameObject characterPrefab;

    [Tooltip("A layer only the preview uses, so the game's camera never draws it and the world's lights never light it " +
             "(30 is the first-person hands', 31 IconCapture's).")]
    [Range(8, 31)]
    [SerializeField] int previewLayer = 29;

    [Header("View")]
    [Tooltip("How the character is turned at first, in degrees (0 = facing you).")]
    [Range(-180f, 180f)]
    [SerializeField] float startTurn = 25f;

    [Tooltip("Degrees it turns per pixel the mouse is dragged.")]
    [Range(0.05f, 2f)]
    [SerializeField] float turnSpeed = 0.4f;

    [Tooltip("The studio camera's field of view, in degrees (smaller = flatter, like a photo from further away).")]
    [Range(10f, 60f)]
    [SerializeField] float fieldOfView = 22f;

    [Header("Light")]
    [Tooltip("The studio light's color.")]
    [SerializeField] Color lightColor = new Color(1f, 0.96f, 0.9f);

    [Tooltip("The studio light's brightness.")]
    [Range(0f, 3f)]
    [SerializeField] float lightIntensity = 1.1f;

    [Tooltip("The even light from everywhere that fills the shadows (what a white wall in the shade looks like).")]
    [SerializeField] Color ambientColor = new Color(0.62f, 0.64f, 0.7f);

    static readonly Vector3 StudioPlace = new Vector3(0f, -2000f, 0f);   // far below the world: nothing else is there

    RawImage view;
    GameObject studio;
    Transform model;       // the character copy (turned by dragging)
    Camera cam;
    RenderTexture picture;
    Vector2Int pictureSize;
    float turn;
    readonly List<Light> mutedLights = new List<Light>();   // the world's lights, told not to light the preview layer
    Camera mainCamera;     // told not to draw the preview layer

    // ===== Unity messages (Unity calls these) =====

    void Awake()
    {
        view = GetComponent<RawImage>();
        if (view.texture == null) view.enabled = false;   // an empty Raw Image is a white box: hidden until the first picture
        turn = startTurn;
    }

    void OnEnable()
    {
        if (studio == null) MakeStudio();
        if (studio != null) studio.SetActive(true);
    }

    void OnDisable()
    {
        if (studio != null) studio.SetActive(false);   // asleep while the page is closed
    }

    void OnDestroy()
    {
        foreach (Light lamp in mutedLights)
        {
            if (lamp != null) lamp.cullingMask |= 1 << previewLayer;
        }
        if (mainCamera != null) mainCamera.cullingMask |= 1 << previewLayer;
        if (studio != null) Destroy(studio);
        if (picture != null)
        {
            picture.Release();
            Destroy(picture);   // (only emptied before 22/11/2026)
        }
    }

    // LateUpdate: after the character's animation moved it this frame.
    void LateUpdate()
    {
        if (cam == null || model == null) return;
        FitPicture();
        model.localRotation = Quaternion.Euler(0f, turn, 0f);
        TakePicture();
    }

    // Dragging the picture turns the character.
    public void OnDrag(PointerEventData eventData)
    {
        turn -= eventData.delta.x * turnSpeed;
    }

    // ===== My methods =====

    // The studio: the character copy, a camera framing it from head to toe, and a light only for this layer.
    void MakeStudio()
    {
        if (characterPrefab == null)
        {
            Debug.LogWarning(name + ": GearPreview has no Character Prefab. Run Tools > UI > Build Gear Page.");
            return;
        }

        studio = new GameObject("Gear Preview Studio");
        studio.transform.position = StudioPlace;

        // The copy is made inside a switched-off holder, so its game scripts never wake up: the character's
        // PlayerAnimator would take over from the real one (and follow a body it doesn't have), ToolInHand would put the
        // held tool in its hand. They're removed first; the Animator (idle pose) and ArmorLook (the armor) stay.
        GameObject holder = new GameObject("Holder");
        holder.SetActive(false);
        holder.transform.SetParent(studio.transform, false);
        GameObject copy = Instantiate(characterPrefab, holder.transform);
        foreach (PlayerAnimator part in copy.GetComponentsInChildren<PlayerAnimator>(true)) DestroyImmediate(part);
        foreach (ToolInHand part in copy.GetComponentsInChildren<ToolInHand>(true)) DestroyImmediate(part);
        foreach (FirstPersonHands part in copy.GetComponentsInChildren<FirstPersonHands>(true)) DestroyImmediate(part);
        copy.transform.SetParent(studio.transform, false);
        copy.transform.localPosition = Vector3.zero;
        copy.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
        Destroy(holder);
        model = copy.transform;

        foreach (Transform part in copy.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = previewLayer;
        foreach (Renderer part in copy.GetComponentsInChildren<Renderer>(true))
        {
            part.shadowCastingMode = ShadowCastingMode.Off;   // no floor to throw a shadow on
            if (part is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen = true;   // never skipped
        }
        Animator animator = copy.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        cam = new GameObject("Preview Camera").AddComponent<Camera>();
        cam.transform.SetParent(studio.transform, false);
        cam.enabled = false;                          // it only draws when TakePicture asks
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);   // see-through: the page's panel shows behind the character
        cam.cullingMask = 1 << previewLayer;
        cam.fieldOfView = fieldOfView;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 30f;
        cam.allowHDR = false;
        Frame(animator);

        Light lamp = new GameObject("Preview Light").AddComponent<Light>();
        lamp.transform.SetParent(studio.transform, false);
        lamp.type = LightType.Directional;
        lamp.color = lightColor;
        lamp.intensity = lightIntensity;
        lamp.shadows = LightShadows.None;
        lamp.cullingMask = 1 << previewLayer;         // lights only the preview
        lamp.transform.rotation = Quaternion.Euler(35f, 200f, 0f);   // from above, over the camera's left shoulder

        // The world's lights don't light the preview, and the game's camera doesn't draw it.
        foreach (Light other in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (other == lamp || (other.cullingMask & (1 << previewLayer)) == 0) continue;
            other.cullingMask &= ~(1 << previewLayer);
            mutedLights.Add(other);
        }
        mainCamera = Camera.main;
        if (mainCamera != null) mainCamera.cullingMask &= ~(1 << previewLayer);
    }

    // Puts the camera in front of the character so it fits from head to toe, measured from its own bones (whatever
    // size the model is).
    void Frame(Animator animator)
    {
        float bottom = 0f;
        float top = 1.8f;
        if (animator != null && animator.isHuman)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (head != null && left != null && right != null)
            {
                float feet = Mathf.Min(left.position.y, right.position.y) - studio.transform.position.y;
                float headY = head.position.y - studio.transform.position.y;
                float tall = Mathf.Max(0.5f, headY - feet);
                bottom = feet - tall * 0.08f;   // the soles are a little below the foot bones
                top = headY + tall * 0.16f;     // the top of the head (and a helmet) is above the head bone
            }
        }

        float height = (top - bottom) * 1.08f;   // a little room around
        float middle = (top + bottom) * 0.5f;
        float distance = height * 0.5f / Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        cam.transform.localPosition = new Vector3(0f, middle, distance);
        cam.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // looking back at the character (it faces +z)
    }

    // A texture as big as the Raw Image is on screen (made again when that changes), with smooth edges.
    void FitPicture()
    {
        Rect rect = ((RectTransform)transform).rect;
        Canvas canvas = view.canvas;
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2Int size = new Vector2Int(Mathf.Max(64, Mathf.RoundToInt(rect.width * scale)), Mathf.Max(64, Mathf.RoundToInt(rect.height * scale)));
        if (picture != null && size == pictureSize) return;

        if (picture != null)
        {
            picture.Release();
            Destroy(picture);   // (only emptied before 22/11/2026: each window resize left one behind)
        }
        RenderTextureDescriptor shape = new RenderTextureDescriptor(size.x, size.y, RenderTextureFormat.ARGB32, 24) { msaaSamples = 4 };
        shape.msaaSamples = Mathf.Max(1, SystemInfo.GetRenderTextureSupportedMSAASampleCount(shape));   // what this computer can do
        picture = new RenderTexture(shape) { name = "Gear Preview" };
        pictureSize = size;
        cam.targetTexture = picture;
        cam.aspect = (float)size.x / size.y;
        view.texture = picture;
        view.enabled = true;
    }

    // One picture of the studio, with the studio's own even light from everywhere (the world's, e.g. a dark blue
    // night, is put back right after).
    void TakePicture()
    {
        SphericalHarmonicsL2 worldLight = RenderSettings.ambientProbe;
        bool worldFog = RenderSettings.fog;

        SphericalHarmonicsL2 even = new SphericalHarmonicsL2();
        even.AddAmbientLight(QualitySettings.activeColorSpace == ColorSpace.Linear ? ambientColor.linear : ambientColor);
        RenderSettings.ambientProbe = even;
        RenderSettings.fog = false;

        cam.Render();

        RenderSettings.ambientProbe = worldLight;
        RenderSettings.fog = worldFog;
    }
}
