using TMPro;
using UnityEngine;

public class SpeechBubble : MonoBehaviour
{
    [Header("文字設定")]
    public TMP_Text text;

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();
    }


    private void Update()
    {
        Camera cam = CameraManager.Current;
        if (cam == null) return;

        transform.forward = cam.transform.forward;
    }

    /// <summary>
    /// 設定氣泡文字，使用 TMP AutoSizing 自動調整字體
    /// </summary>
    /// <param name="msg"></param>
    public void SetText(string msg)
    {
        if (text == null) return;

        text.text = msg;

        // ====== TMP AutoSizing 設定 ======
        text.enableAutoSizing = true;
        text.fontSizeMax = 1.0f;   // 最大字體
        text.fontSizeMin = 0.3f;   // 最小字體
        text.overflowMode = TextOverflowModes.Overflow;
        text.maxVisibleLines = 2;
        text.ForceMeshUpdate();
    }
}
