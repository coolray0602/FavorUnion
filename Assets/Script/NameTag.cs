using UnityEngine;

public class NameTag : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = CameraManager.Current;
        if (cam == null) return;

        transform.forward = cam.transform.forward;
    }
}