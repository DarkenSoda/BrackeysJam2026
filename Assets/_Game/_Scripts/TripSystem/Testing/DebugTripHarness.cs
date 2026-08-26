#if UNITY_EDITOR
using UnityEngine;

public class DebugTripHarness : MonoBehaviour
{
    [Tooltip("Turn off to stop these keys from doing anything, without deleting the component.")]
    [SerializeField] private bool enableDebugKeys = true;

    private void Update()
    {
        if (!enableDebugKeys) return;

        if (Input.GetKeyDown(KeyCode.P)) TripController.Instance.StartTrip();
        if (Input.GetKeyDown(KeyCode.F)) TripController.Instance.ResolveChoice(true);
        if (Input.GetKeyDown(KeyCode.I)) TripController.Instance.ResolveChoice(false);
    }
}
#endif