using UnityEngine;
using UnityEngine.XR;

public class SnapTurn : MonoBehaviour
{
    [Header("References")]

    [Tooltip("Root object that should rotate when snap turning. This is usually the XR Origin or a parent of the camera.")]
    [SerializeField] private Transform turnParent;

    [Header("Input")]

    [Tooltip("XR controller used for turning. RightHand/LeftHand is recommended.")]
    [SerializeField] private XRNode turnController = XRNode.RightHand;

    [Tooltip("Minimum horizontal joystick value required to trigger a turn.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float inputThreshold = 0.75f;

    [Header("Turning")]

    [Tooltip("Degrees rotated per snap turn.")]
    [SerializeField] private float turnAmount = 45f;

    [Tooltip("Minimum time (seconds) between snap turns.")]
    [SerializeField] private float turnCooldown = 1f;

    private InputDevice device;
    private float lastTurnTime;

    private void OnEnable()
    {
        device = InputDevices.GetDeviceAtXRNode(turnController);
    }

    private void Update()
    {
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(turnController);
            return;
        }

        if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
            return;

        if (Time.time - lastTurnTime < turnCooldown)
            return;

        if (Mathf.Abs(axis.x) < inputThreshold)
            return;

        PerformTurn(Mathf.Sign(axis.x));
    }

    private void PerformTurn(float direction)
    {
        if (turnParent == null)
        {
            Debug.LogWarning("SnapTurn: Turn Parent is not assigned.");
            return;
        }

        turnParent.Rotate(0f, direction * turnAmount, 0f, Space.World);
        lastTurnTime = Time.time;
    }

    [ContextMenu("Turn Test")]
    private void TurnTest()
    {
        PerformTurn(1f);
    }
}
