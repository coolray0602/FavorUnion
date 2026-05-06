using UnityEngine;
using UnityEngine.UI;

public class OrientationHint : MonoBehaviour
{
    [Tooltip("直屏時顯示的提示物件 (例如包含文字或Panel)")]
    public GameObject hintUI;

    void Start()
    {
        UpdateHintVisibility();
    }

    void Update()
    {
        // 每幀偵測螢幕方向
        UpdateHintVisibility();
    }

    void UpdateHintVisibility()
    {
        if (hintUI == null) return;

        // 檢查目前是橫屏還是直屏
        bool isPortrait = Screen.height > Screen.width;

        // 直屏時顯示提示
        hintUI.SetActive(isPortrait);
    }
}
