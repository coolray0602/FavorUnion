using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = CameraManager.Current;
        if (cam == null) return;

        transform.forward = cam.transform.forward;
    }
}