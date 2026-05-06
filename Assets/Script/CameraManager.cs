using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static Camera Current;

    public static void SetCamera(Camera cam)
    {
        Current = cam;
    }
}
