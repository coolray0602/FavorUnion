using UnityEngine;
using UnityEngine.UI;
using TMPro;
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public bool holdable = true;
    public GameObject worldPrefab;

    // 🔑 新增欄位，只有鑰匙會用
    public string keyPlateNumber; // 空字串表示不是鑰匙
}