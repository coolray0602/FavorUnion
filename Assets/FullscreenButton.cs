using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 继承自Button，并实现IPointerDownHandler接口
public class FullscreenButton : Button, IPointerDownHandler
{
    // 重写OnPointerDown方法，在手指按下时立即触发
    public override void OnPointerDown(PointerEventData eventData)
    {
        // 先调用基类方法，保留按钮原有的高亮反馈等效果
        base.OnPointerDown(eventData);

        // 执行全屏切换操作
        ToggleFullscreen();
    }

    // 全屏切换的具体逻辑
    private void ToggleFullscreen()
    {
        Debug.Log("尝试切换全屏模式，当前模式: " + Screen.fullScreen);
        // 切换全屏状态
        Screen.fullScreen = !Screen.fullScreen;
        Debug.Log("切换后全屏模式: " + Screen.fullScreen);
    }
}