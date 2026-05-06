using UnityEngine;

public class ItemHolder : MonoBehaviour
{
    public Item item;       // 指向模板
    [HideInInspector] public Item runtimeItem; // 遊戲中實際生成

    public void InitRuntimeItem(string plateNumber = "")
    {
        runtimeItem = Instantiate(item); // 每個物件都有獨立 Item
        if (!string.IsNullOrEmpty(plateNumber))
        {
            runtimeItem.keyPlateNumber = plateNumber;
            runtimeItem.id = "Key_" + plateNumber; // 確保唯一
            runtimeItem.description = $"這是一把鑰匙，上面寫著「{plateNumber}」";
        }
    }
    public void EnsureRuntimeItem()
    {
        if (runtimeItem == null && item != null)
        {
            runtimeItem = Instantiate(item);
        }
    }
}