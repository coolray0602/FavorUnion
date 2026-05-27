using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject inventoryPanel;
    public Transform slotsParent;
    public TMP_Text descText;
    [Header("Equip Status UI")]
    public TMP_Text equippedText;  // 顯示已裝備
    [Header("Player Inventory")]
    public PlayerInventory playerInventory;

    [Header("Buttons")]
    public Button holdButton;
    public Button sleepButton;

    [Header("Player Attach Points")]
    public Transform playerRightHand;

    public Transform wakeupPosition; // 用於睡覺後的醒來位置

    // ⭐ 新增：通知 PlayerCtrl 更新 heldObject
    public System.Action<GameObject> onItemHeld;

    private GameObject[] slots;
    private Image[] selectedBGs;
    private int selectedIndex = -1;
    bool initialized = false;
    void Awake()
    {

    }
    void Init()
    {
        if (initialized) return;

        initialized = true;
        // 原本 Awake 裡的初始化全部搬來
        if (slotsParent == null)
        {
            Debug.LogError("slotsParent 沒有指派！");
            return;
        }

        int count = slotsParent.childCount;
        slots = new GameObject[count];
        selectedBGs = new Image[count];

        for (int i = 0; i < count; i++)
        {
            slots[i] = slotsParent.GetChild(i).gameObject;

            Transform bg = slots[i].transform.Find("SelectedBG");
            selectedBGs[i] = bg ? bg.GetComponent<Image>() : null;

            int index = i;
            Button btn = slots[i].GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnSlotClicked(index));
            else
                Debug.LogWarning($"Slot {i} 沒有 Button");
        }

        if (holdButton != null)
            holdButton.onClick.AddListener(OnHoldClicked);
        else
            Debug.LogError("holdButton 沒有指派！");
        if (sleepButton != null)
            sleepButton.onClick.AddListener(OnSleepClicked);
        else
            Debug.LogError("sleepButton 沒有指派！");
    }

    // =========================================================
    //   按下「拿出（Hold）」按鈕
    // =========================================================
    private void OnHoldClicked()
    {
        if (selectedIndex < 0 || selectedIndex >= playerInventory.items.Count)
            return;

        InventoryItem inventoryItem = playerInventory.items[selectedIndex];
        Item item = inventoryItem.item;
        if (item == null || item.worldPrefab == null)
            return;

        // --- 移除舊手持物（不動 inventory） ---
        if (playerRightHand.childCount > 0)
        {
            Destroy(playerRightHand.GetChild(0).gameObject);
        }

        // --- 生成新物品 ---
        GameObject obj = Instantiate(item.worldPrefab);
        obj.transform.SetParent(playerRightHand, false); // ✅ 避免變形
        obj.transform.localPosition = new Vector3(0f, 0f, 0.03f);
        obj.transform.localRotation = Quaternion.identity;

        Rigidbody r = obj.GetComponent<Rigidbody>();
        if (r) r.isKinematic = true;

        Collider c = obj.GetComponent<Collider>();
        if (c) c.enabled = false;

        // --- ⚡ 設定 ItemHolder.runtimeItem
        ItemHolder holder = obj.GetComponent<ItemHolder>();
        if (holder != null)
        {
            holder.runtimeItem = item; // ✅ 這行很重要，保持跟 inventory 一致
        }

        // --- 通知 PlayerCtrl ---
        onItemHeld?.Invoke(obj);

        // --- UI ---
        selectedIndex = -1;
        RefreshUI();
        inventoryPanel.SetActive(false);
        Time.timeScale = 1f;
        if (!Application.isMobilePlatform)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnSleepClicked()
    {
        ToggleInventory();
        GameManager.Instance.SleepToMorning(wakeupPosition.position, false); // 傳入玩家位置，讓 GameManager 可以在適當位置生成睡覺效果
    }

    // =========================================================
    // 開關背包
    // =========================================================
    public void ToggleInventory()
    {
        bool show = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(show);

        if (show)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f;
            RefreshUI();
        }
        else
        {
            if (!Application.isMobilePlatform)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            Time.timeScale = 1f;
        }
    }

    // =========================================================
    // 更新 UI
    // =========================================================
    public void RefreshUI()
    {
        if (!initialized)
            Init();
        if (slots == null)
        {
            Debug.LogError("RefreshUI 被呼叫，但 InventoryUI 尚未初始化");
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            InventoryItem invItem =
                (i < playerInventory.items.Count ? playerInventory.items[i] : null);
            Item item = invItem != null ? invItem.item : null;

            Image icon = slots[i].transform.Find("Icon").GetComponent<Image>();
            TMP_Text countText = slots[i].transform.Find("Count").GetComponent<TMP_Text>();

            if (item != null)
            {
                icon.sprite = item.icon;
                icon.color = Color.white;
                countText.text = invItem.count > 1 ? invItem.count.ToString() : "";
            }
            else
            {
                icon.sprite = null;
                icon.color = Color.clear;
                countText.text = "";
            }

            if (selectedBGs[i])
                selectedBGs[i].enabled = (i == selectedIndex);
        }

        if (selectedIndex < 0 || selectedIndex >= playerInventory.items.Count)
            descText.text = "";
        else
        {
            Item selectedItem = playerInventory.items[selectedIndex].item;
            // ⭐ 如果是懷表（id=12）
            if (selectedItem.id == "12")
            {
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null)
                {
                    string timeStr = gm.GetCurrentTimeString();
                    descText.text = selectedItem.description + $"，上面顯示著現在時間：{timeStr}";
                }
                else
                {
                    descText.text = "時間未知";
                }
            }
            else
            {
                descText.text = selectedItem.description;
            }
        }


        // 控制 Hold 按鈕與已裝備文字
        if (selectedIndex >= 0 && selectedIndex < playerInventory.items.Count)
        {
            Item selectedItem = playerInventory.items[selectedIndex].item;

            if (selectedItem.holdable)
            {
                holdButton.gameObject.SetActive(true);
                if (equippedText != null)
                    equippedText.gameObject.SetActive(false);
            }
            else
            {
                holdButton.gameObject.SetActive(false);
                if (equippedText != null)
                {
                    equippedText.gameObject.SetActive(true);
                    equippedText.text = "已裝備";
                }
            }
        }
        else
        {
            holdButton.gameObject.SetActive(false);
            if (equippedText != null)
                equippedText.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // 點擊 Slot
    // =========================================================
    private void OnSlotClicked(int index)
    {
        if (index >= playerInventory.items.Count)
            return;

        selectedIndex = index;
        descText.text = playerInventory.items[index].item.description;
        RefreshUI();
    }
}
