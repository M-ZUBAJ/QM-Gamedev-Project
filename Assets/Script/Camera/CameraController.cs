/*
First Date: 29/9/2026
Date: 23/11/2026
Author: Tony
Use by: Drag this script onto the Main Camera, then drag the Player sphere into the "Player" slot.
Unity Editor: Third Person Distance = how far behind the ball the camera stays after F5 (it can be changed while
              playing).
Purpose: Move the mouse to turn the camera.
         Press F5 to switch between third person (orbit around the ball) and first person (view from the ball).
         Press Esc to show the cursor again, click in the Game view to hide it. With a pause menu in the scene
         (PauseMenu), Esc opens that instead (it frees the mouse while it's open).
         (These keys can be changed in KeyBindings.)
         The player's mouse settings (Settings > CONTROLS, title screen or Esc menu; MouseSettings) change how it turns:
         the speed is Sensitivity x the player's choice (5 of 1 - 10 = as set here), and Invert mouse turns up / down.
Requirement: CameraController.cs x-> KeyBindings.cs, MenuState.cs, MouseSettings.cs
             CameraController.cs o-> PauseMenu.cs
             CameraController.cs <-x FirstPersonHandsBuilder.cs
             CameraController.cs <-o BowShooter.cs, FirstPersonHands.cs, HeldItemView.cs, PlayerAnimator.cs, ToolInHand.cs
*/
 
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
 
public class CameraController : MonoBehaviour
{
    // ===== Fields =====
    [Header("Target")]
    [Tooltip("The object the camera follows (drag the Player sphere here).")]
    [SerializeField] Transform player;
 
    [Header("Mouse")]
    [Tooltip("How fast the camera turns when you move the mouse, at the player's sensitivity 5 (Settings > CONTROLS: " +
             "1 - 10, 10 = twice this).")]
    [Range(0.1f, 10f)]
    [SerializeField] float sensitivity = 3f;
 
    [Tooltip("Ticked: moving the mouse up makes the camera look down (the player can also choose it: Settings > CONTROLS).")]
    [SerializeField] bool invertY = false;
 
    [Header("Third person")]
    [Tooltip("How far behind the ball the camera stays in third person (meters). It can be changed while playing.")]
    [Range(1f, 20f)]
    [SerializeField] float thirdPersonDistance = 5f;

    [Tooltip("Lowest angle: 0 = level with the ball.")]
    [Range(-89f, 89f)]
    [SerializeField] float minPitch = 10f;
 
    [Tooltip("Highest angle: 89 = almost straight down on the ball.")]
    [Range(-89f, 89f)]
    [SerializeField] float maxPitch = 80f;
 
    [Header("First person")]
    [Tooltip("Start the game in first person.")]
    [SerializeField] bool firstPerson = false;
 
    [Tooltip("How high above the ball's center the eyes are. 0.5 = top of a normal sphere.")]
    [SerializeField] float eyeHeight = 0.5f;
 
    [Tooltip("How far you can look down (negative = up, positive = down).")]
    [Range(-89f, 89f)]
    [SerializeField] float fpsMinPitch = -80f;
 
    [Tooltip("How far you can look down.")]
    [Range(-89f, 89f)]
    [SerializeField] float fpsMaxPitch = 80f;
 
    float yaw;        // left/right angle
    float pitch;      // up/down angle

    public bool IsFirstPerson => firstPerson;   // other scripts can read it (e.g. HeldItemView hides in third person)
 
    // ===== Unity messages (Unity calls these) =====
 
    void Start()
    {
        // Start at the angle the camera has in the scene. (The third-person distance used to be measured here too,
        // from the camera to the ball, but in the new world the World Generator moves the ball to the start before
        // this runs, while the camera still stands where it was saved: the camera ended up about 2 km behind the ball,
        // out of sight. Now it's Third Person Distance.)
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;   // e.g. 350° really means -10°
 
        LockCursor(true);
    }
 
    void Update()
    {
        // A menu controls the cursor while it's open.
        if (MenuState.AnyOpen) return;
 
        if (F5Pressed()) ToggleView();
 
        // Esc frees the cursor (e.g. to click UI). Clicking hides it again. With a pause menu in the scene, Esc opens it
        // instead (it frees the cursor while it's open and locks it again when it closes).
        if (EscapePressed() && !PauseMenu.Active) LockCursor(false);
        else if (ClickPressed()) LockCursor(true);
    }
 
    // LateUpdate runs after the ball has moved this frame.
    void LateUpdate()
    {
        // Only turn the camera while the cursor is hidden and no menu is open.
        if (Cursor.lockState == CursorLockMode.Locked && !MenuState.AnyOpen)
        {
            Vector2 mouse = ReadMouseDelta() * MouseSettings.Multiplier;   // the player's sensitivity (5 = 1x)
            bool invert = invertY || MouseSettings.InvertY;                  // the player's Invert mouse
            yaw += mouse.x * sensitivity;
            pitch += (invert ? mouse.y : -mouse.y) * sensitivity;
        }
 
        // Each view has its own up/down limits.
        float min = firstPerson ? fpsMinPitch : minPitch;
        float max = firstPerson ? fpsMaxPitch : maxPitch;
        pitch = Mathf.Clamp(pitch, min, max);
 
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
 
        if (firstPerson)
        {
            // Sit on top of the ball. Only the position follows, so the view doesn't spin as it rolls.
            transform.position = player.position + Vector3.up * eyeHeight;
        }
        else
        {
            // Stand at the ball, then step back along the direction the camera is facing.
            transform.position = player.position - rotation * Vector3.forward * thirdPersonDistance;
        }
    }
 
    // ===== My methods =====
 
    // Switches between first and third person. Also usable from a button's On Click () list.
    public void ToggleView()
    {
        firstPerson = !firstPerson;
    }
 
    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
 
    Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return Vector2.zero;
        return Mouse.current.delta.ReadValue() * 0.1f;   // scaled to match the old system
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }
 
    // The keys below are chosen in KeyBindings.
    bool F5Pressed()
    {
        return Input.GetKeyDown(KeyBindings.ToggleView);
    }
 
    bool EscapePressed()
    {
        return Input.GetKeyDown(KeyBindings.UnlockCursor);
    }
 
    bool ClickPressed()
    {
        return Input.GetKeyDown(KeyBindings.LockCursor);   // KeyCode.Mouse0 = left mouse button
    }
}