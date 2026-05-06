using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class NPCResponseData
{
    [Header("條件 Flag 名稱")]
    public string requiredFlag;

    [Header("玩家說的話")]
    public string playerText;

    [Header("NPC 回應文字")]
    [TextArea(2, 4)]
    public string responseText;

    [Header("NPC 回饋給玩家的物品（可空）")]
    public Item rewardItem;

    [Header("達成條件時執行的事件")]
    public UnityEvent onConditionSuccess;

}
