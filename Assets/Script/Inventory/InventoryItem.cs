[System.Serializable]
public class InventoryItem
{
    public Item item; // ScriptableObject
    public int count;

    public InventoryItem(Item item, int count = 1)
    {
        this.item = item;
        this.count = count;
    }
}
