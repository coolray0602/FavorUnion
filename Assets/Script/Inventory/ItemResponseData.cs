using UnityEngine;
using UnityEngine.Events;
[System.Serializable]
public class ItemResponseData
{
    [Header("玩家給的物品")]
    public Item requiredItem;

    [Header("NPC 回應文字")]
    [TextArea(2, 4)]
    public string responseText;

    [Header("NPC 回饋給玩家的物品（可空）")]
    public Item rewardItem;
    [Header("達成條件時執行的事件")]
    public UnityEvent onConditionSuccess;
}