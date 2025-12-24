using UnityEngine;

public class RotateMatcher : MonoBehaviour
{
    //made for Chimped

    [Header("Rotation Match")]
    [Tooltip("The target copies the rotation of the source")]
    public Transform source;
    [Tooltip("The target copies the rotation of the source")]
    public Transform target;

    void LateUpdate()
    {
        if (source == null || target == null) return;
        target.rotation = source.rotation;
    }
}
