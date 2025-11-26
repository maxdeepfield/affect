
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(MouseLook))]
[RequireComponent(typeof(WeaponController))]
public class FPSController : MonoBehaviour
{
    // This class is now a wrapper for the other components.
    // It ensures that all the necessary components are attached to the player object.
}
