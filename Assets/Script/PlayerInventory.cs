using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public List<InventoryItem> items = new List<InventoryItem>();

    /// <summary>
    /// 撿物品
    /// amount: 道具數量（鑰匙永遠 1）
    /// allowStack: 是否允許堆疊（鑰匙傳 false）
    /// </summary>
    public void AddItem(Item itemToAdd, int amount = 1, bool allowStack = true)
    {
        // 如果允許堆疊，且物品不是重複的（id 不同就不堆疊）
        if (allowStack)
        {
            InventoryItem existing = items.Find(i => i.item == itemToAdd);
            if (existing != null)
            {
                existing.count += amount;
                Debug.Log($"Updated {existing.item.itemName} count: {existing.count}");
                return;
            }
        }

        // 鑰匙或不堆疊物品，每次都生成新 InventoryItem
        items.Add(new InventoryItem(itemToAdd, amount));
        Debug.Log($"Added new item: {itemToAdd.itemName} x{amount}");
    }

    public void RemoveItem(Item itemToRemove, int amount = 1)
    {
        if (itemToRemove == null)
        {
            Debug.LogError("RemoveItem：itemToRemove 為 null");
            return;
        }

        // ⚠️ 用 id 來找，不用物件參考
        InventoryItem existing = items.Find(i => i.item.id == itemToRemove.id);

        if (existing != null)
        {
            existing.count -= amount;
            if (existing.count <= 0)
                items.Remove(existing);
            Debug.Log($"Removed {itemToRemove.itemName} x{amount}");
        }
        else
        {

            Debug.LogWarning($"Item {itemToRemove.itemName} not found in inventory. id={itemToRemove.id}");
        }
    }
    public int GetItemCount(string id)
    {
        int total = 0;

        foreach (var item in items)
        {
            if (item.item.id == id)
                total += item.count;
        }

        return total;
    }
    public void RemoveAllById(string id)
    {
        int removedCount = 0;

        // 先統計數量（可選）
        foreach (var item in items)
        {
            if (item.item.id == id)
                removedCount += item.count;
        }

        // 移除所有符合條件的物件
        items.RemoveAll(i => i.item.id == id);

        Debug.Log($"Removed ALL items with id={id}, total removed count={removedCount}");
    }
}
