using UnityEngine;
using UnityEngine.InputSystem;

/// Quick test trigger for ShockWaveManager: Space plays the correct-call
public class ShockWaveTestInput : MonoBehaviour
{
    private void Update()
    {
        // Keyboard.current is null if no keyboard device is connected -
        // guard against that instead of crashing.
        if (Keyboard.current == null || ShockWaveManager.Instance == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ShockWaveManager.Instance.PlayCorrectVFX();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ShockWaveManager.Instance.PlayWrongVFX();
        }
    }
}