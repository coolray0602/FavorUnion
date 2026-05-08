using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;
using System.Runtime.CompilerServices;

public class PlayerCtrl : MonoBehaviour
{
    public TextMeshProUGUI debugtxt;
    public LayerMask hitMask;
    public GameObject punchEffectPrefab;
    public AudioClip carEnterClip;
    public AudioSource sfxSource;   // 播音效用（例如上車聲）
    [DllImport("__Internal")]
    private static extern string GetUserAgent();

    [Header("Inventory")]
    public PlayerInventory playerInventory;
    public InventoryUI inventoryUI;   // 在 Inspector 拖你的 InventoryUI
    [Header("Info UI")]
    public GameObject infoPanel;
    public GameObject[] boards;

    [Header("Camera & View")]
    public GameObject eye;
    private float pitch = 0f;
    public GameObject selectionMarker; // 指向你的 SelectionMarker
    [Header("Movement")]
    private Rigidbody rb;
    public Animator animator;
    public float moveSpeed = 10f;
    public float rotateSpeed = 80f;
    public Transform groundCheck;
    public float groundCheckDistance = 0.1f;
    private bool isGround = false;
    private bool isJumpPreparing = false;
    private bool isJumping = false;
    public bool isLanding = true;
    private bool isPicking = false;
    private bool isHitting = false;
    private GameObject heldObject = null;
    [Header("Mobile")]
    public Joystick joystick;
    private Vector2 lastTouchPos;
    private bool isTouching = false;
    private bool isMobile = false;
    [Header("Pick up")]
    public Transform rightHand;                // 拖角色右手骨頭
    private GameObject targetItem = null;   // 正在撿的物品

    [Header("Car")]
    private bool isInCar = false;
    private GameObject currentCar;
    private Camera playerCam;
    private Camera carCam;
    public float carMoveSpeed = 10f;
    public float carRotateSpeed = 50f;
    private Transform[] wheelTransforms;
    public float wheelCheckDistance = 0.5f;
    private bool canDance = false;
    [Header("UI")]
    public TextMeshProUGUI hitButtonText;



    [Header("Player Bubble")]
    public string[] playerReplyMessages;
    public GameObject playerSpeechBubblePrefab;
    private GameObject playerSpeechBubbleInstance;
    SleepArea currentSleepArea;

    public bool fainted = false;    //如果已經昏倒了，鬼怪就不再次攻擊了

    float checkTimer = 0f;
    GameObject cachedFrontNPC;
    Collider[] npcResults = new Collider[20];   // ⭐ 不要太大
    List<GameObject> candidates = new List<GameObject>();
    GameObject lastSelectedNPC = null;
    // ===== NonAlloc 共用 =====
    Collider[] itemResults = new Collider[10];
    Collider[] carResults = new Collider[10];
    Collider[] enemyResults = new Collider[10];

    // ===== 快取 =====
    GameObject cachedNearbyItem;
    GameObject cachedNearbyCar;

    void Start()
    {

        isLanding = true;
        // 在真正裝置上正常判斷
        isMobile = Application.isMobilePlatform;

        //isMobile = true; // 🔥 強制開啟手機模式，方便測試

        Debug.Log("isMobile: " + isMobile);
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        playerCam = Camera.main;
        CameraManager.SetCamera(playerCam);

        inventoryUI.onItemHeld = (obj) =>
        {
            heldObject = obj;    // ✔ 玩家現在手中真的有物品
        };
        animator.SetTrigger("wakeup");
    }

    void Update()
    {
        checkTimer -= Time.deltaTime;

        if (checkTimer <= 0f)
        {
            cachedFrontNPC = GetFrontNPC(1f, 45f);
            cachedNearbyItem = FindNearbyItem();
            cachedNearbyCar = FindNearbyCar();
            checkTimer = 0.1f; // ⭐ 每0.1秒檢查一次
        }
        if (GameManager.Instance.onEnding)
        {
            // 過關畫面中不處理玩家控制並顯示滑鼠游標
            ShowCursor();
            return;
        }
        isGround = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance);

        HandleKeyEvent();

        UpdateHitButtonText();

        if (isJumping && rb.velocity.y < 0f && isGround)
        {
            animator.SetTrigger("grounded");
            isJumping = false;
            isJumpPreparing = false;
        }
    }

    void LateUpdate()
    {
        if (!isInCar)
        {
            HandleRotation();
        }
        else
        {
            HandleCarControl();
        }

    }
    float lastEulerAngle = 0f;
    void HandleRotation()
    {
        // 🔥 ③ 旋轉：滑鼠 Look
        if (isMobile == false) // 沒有觸控時才用滑鼠旋轉
        {
            float rx = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float ry = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;

            if (rx != 0f)
            {
                /*
                if (Mathf.Abs(lastEulerAngle - transform.eulerAngles.y) > 0.01f)
                {
                    Debug.Log($"******旋轉時Euler Y changed,from {lastEulerAngle} to {transform.eulerAngles.y}");
                }
                else
                {
                    Debug.Log($"correct: {transform.eulerAngles.y}");
                }*/

                transform.eulerAngles += new Vector3(0, rx, 0);
                lastEulerAngle = transform.eulerAngles.y;
            }
            /*
            else
            {
                if (Mathf.Abs(lastEulerAngle - transform.eulerAngles.y) > 0.01f)
                {
                    Debug.Log($"******沒有在旋轉 Euler Y changed,from {lastEulerAngle} to {transform.eulerAngles.y}");
                    // 打印调用堆栈（最重要！）
                    UnityEngine.Debug.LogWarning($"调用堆栈:\n{StackTraceUtility.ExtractStackTrace()}");
                    transform.eulerAngles = new Vector3(transform.eulerAngles.x, lastEulerAngle, transform.eulerAngles.z);
                }
            }*/

            pitch -= ry;
            pitch = Mathf.Clamp(pitch, -40f, 40f);
            eye.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }
        else
        {
            HandleTouchLook();
        }
    }
    void FixedUpdate()
    {
        if (!isInCar)
            HandleMovementPhysics();
    }

    void HandleKeyEvent()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
            ShowCursor();
        if (!GameManager.Instance.isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(2))
            {
                if (inventoryUI != null)
                    inventoryUI.ToggleInventory();
                else
                    Debug.LogWarning("inventoryUI 未拖到 PlayerCtrl");
            }

            if (inventoryUI != null && inventoryUI.inventoryPanel.activeSelf)
                return; // 背包開著時不處理其他按鍵
            //info面板開著時任意鍵關閉面板，且不處理其他按鍵
            if (infoPanel != null && infoPanel.activeSelf)
            {
                if (Input.anyKeyDown)
                {
                    infoPanel.SetActive(false);
                    return;
                }
            }

            if (!isInCar && Input.GetKeyDown(KeyCode.Space))
                JumpButton();
            if (!isMobile)
            {
                if (Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1))
                    HandleActionButton();
                if (!isInCar && (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(0)))
                {
                    TalkButton();
                }
            }
        }
    }

    public void resetStatus()
    {
        //完成拾取物品
        if (isPicking)
        {
            OnPickItemFinish();
        }

        isJumpPreparing = false;
        isJumping = false;
        isLanding = false;
        isPicking = false;
        isHitting = false;

    }
    void HandleMovementPhysics()
    {
        if (isPicking || isHitting || isJumpPreparing || isLanding)
        {
            animator.SetBool("isWalking", false);

            // 停止水平移動（但保留重力）
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        float inputH = 0f;
        float inputV = 0f;

        // ------------------------------
        // 🔥 ① 取得輸入（不乘 deltaTime！）
        // ------------------------------
        if (!isMobile)
        {
            inputH = Input.GetAxis("Horizontal");
            float kbV = Input.GetAxis("Vertical");
            inputV = kbV >= 0 ? kbV : kbV * 0.5f;
        }
        else if (joystick != null)
        {
            //debugtxt.text="joystickH= "+joystick.Horizontal+" , joystickV= "+joystick.Vertical;
            inputH = joystick.Horizontal;
            float joyV = joystick.Vertical;
            inputV = joyV >= 0 ? joyV : joyV * 0.5f;
        }

        // ------------------------------
        // 🔥 ② 計算目標速度（關鍵）
        // ------------------------------
        Vector3 moveDir = (transform.right * inputH + transform.forward * inputV).normalized;

        float targetSpeed = moveSpeed;
        Vector3 targetVelocity = moveDir * targetSpeed;

        // ------------------------------
        // 🔥 ③ 套用速度（保留Y）
        // ------------------------------
        Vector3 velocity = rb.velocity;
        velocity.x = targetVelocity.x;
        velocity.z = targetVelocity.z;

        // ⭐ 限速（避免穿牆）
        float maxSpeed = moveSpeed;
        velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
        velocity.z = Mathf.Clamp(velocity.z, -maxSpeed, maxSpeed);

        rb.velocity = velocity;

        // ------------------------------
        // 🔥 ④ 動畫
        // ------------------------------
        bool isMoving = moveDir.magnitude > 0.1f;
        if (animator.GetBool("isWalking") != isMoving)
            animator.SetBool("isWalking", isMoving);

        float blendValue = inputV < -0.1f ? 1f : 0f;
        animator.SetFloat("moveZ", blendValue);
    }


    void HandleTouchLook()
    {

        Debug.Log("Handling touch look with " + Input.touchCount + " touches.");
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.position.x > Screen.width / 2)
            {
                if (t.phase == TouchPhase.Began)
                {
                    lastTouchPos = t.position;
                    isTouching = true;
                }
                else if (t.phase == TouchPhase.Moved && isTouching)
                {
                    Vector2 delta = t.deltaPosition * 0.2f;
                    transform.Rotate(0, -delta.x, 0);

                    pitch += delta.y * 0.2f;
                    pitch = Mathf.Clamp(pitch, -40f, 40f);
                    eye.transform.localEulerAngles = new Vector3(pitch, 0, 0);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    isTouching = false;
            }
        }
    }

    public void ShowBubble(string message)
    {
        if (playerSpeechBubbleInstance != null)
            Destroy(playerSpeechBubbleInstance);

        playerSpeechBubbleInstance = Instantiate(playerSpeechBubblePrefab, transform);
        playerSpeechBubbleInstance.transform.localPosition = new Vector3(0.4f, 1f, 0);

        var bubble = playerSpeechBubbleInstance.GetComponent<SpeechBubble>();
        if (bubble != null)
            bubble.SetText(message);
        PlayPlayerTalkingSound();
        Destroy(playerSpeechBubbleInstance, 2f);
    }

    void PlayPlayerTalkingSound()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerTalkingClip != null)
        {
            AudioSource.PlayClipAtPoint(GameManager.Instance.playerTalkingClip, transform.position);
        }
    }

    public void ShowPlayerReplyBubble()
    {
        if (playerReplyMessages == null || playerReplyMessages.Length == 0)
            return;

        string reply = playerReplyMessages[Random.Range(0, playerReplyMessages.Length)];

        ShowBubble(reply);
    }

    // ===== 車輛控制 =====
    void AutoGetWheels()
    {
        if (currentCar == null) return;

        List<Transform> wheels = new List<Transform>();
        Transform[] allChildren = currentCar.GetComponentsInChildren<Transform>();
        foreach (Transform t in allChildren)
            if (t.CompareTag("Wheel"))
                wheels.Add(t);

        wheelTransforms = wheels.ToArray();
    }

    void TryEnterCar(GameObject car = null)
    {
        if (car == null)
            car = cachedNearbyCar;

        if (car == null) return;
        // --- 取得車牌號 ---
        CarController carCtrl = car.GetComponent<CarController>();
        string carPlate = carCtrl != null ? carCtrl.plateNumber : "";
        Debug.Log("Car plate: " + carPlate);

        if (string.IsNullOrEmpty(carPlate) && !car.CompareTag("myCar"))
        {
            Debug.LogWarning("車沒有設定 plateNumber!");
            return;
        }

        // --- 檢查是否有鑰匙 ---
        bool hasKey = false;
        foreach (var invItem in playerInventory.items)
        {
            if (invItem.item.id == "Key_" + carPlate)
            {
                hasKey = true;
                break;
            }
        }
        //hasKey = true; // 🔥 測試用，先強制有鑰匙
        if (!hasKey && !car.CompareTag("myCar"))
        {
            ShowBubble("我沒有鑰匙");
            return;
        }

        // --- 入車流程 ---
        if (car.CompareTag("myCar") && !PlayerPrefs.HasKey("hasRefillGas"))
        {
            ShowBubble("昨天忘記加油，車子沒有油了…");
            return;
        }
        currentCar = car;
        carCam = currentCar.GetComponentInChildren<Camera>();

        if (playerCam != null)
            playerCam.gameObject.SetActive(false);

        if (carCam != null)
        {
            carCam.gameObject.SetActive(true);
            carCam.enabled = true;
            CameraManager.SetCamera(carCam);
        }
        else
            Debug.LogWarning("Car camera not found!");

        SetPlayerVisible(false);
        transform.SetParent(currentCar.transform);
        transform.localPosition = Vector3.zero; // 調整到車內位置
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false; // 禁用玩家碰撞器
        isInCar = true;
        rb.isKinematic = true;
        AutoGetWheels();
        PlayCarEnterSound();
    }
    void ExitCar()
    {
        if (currentCar == null) return;

        if (carCam != null)
            carCam.enabled = false;

        if (playerCam != null)
        {
            playerCam.gameObject.SetActive(true);
            CameraManager.SetCamera(playerCam);
        }

        SetPlayerVisible(true);

        transform.SetParent(null);

        Transform exitPoint = currentCar.transform.Find("ExitPoint");

        Vector3 exitPos = exitPoint != null
            ? exitPoint.position
            : currentCar.transform.position + currentCar.transform.right * 2f;

        Vector3 forward = currentCar.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        // 🔥 1. 先解除 kinematic（很重要）
        rb.isKinematic = false;

        // 🔥 2. 先移出車子 collider 範圍（關鍵）
        transform.position = exitPos + currentCar.transform.up * 0.5f;

        // 🔥 3. 再清 physics
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = rot;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Physics.SyncTransforms(); // ⭐ 超重要

        isInCar = false;
        currentCar = null;

        StopCarEnterSound();
    }

    void HandleCarControl()
    {
        if (currentCar == null) return;

        Rigidbody carRb = currentCar.GetComponent<Rigidbody>();
        if (carRb == null) return;

        // ----- 車子傾斜超過 80 度不能開 -----
        float tiltAngle = Vector3.Angle(currentCar.transform.up, Vector3.up);
        if (tiltAngle >= 80f) return;

        // ==========================================================
        // 🔥 整合輸入：鍵盤 + 虛擬搖桿
        // ==========================================================
        float h = 0f;
        float v = 0f;

        // --- 鍵盤輸入 ---
        float kbH = Input.GetAxis("Horizontal");
        float kbV = Input.GetAxis("Vertical");

        h += kbH;
        v += kbV;

        // --- 搖桿輸入 ---
        if (joystick != null)
        {
            float joyH = joystick.Horizontal;
            float joyV = joystick.Vertical;

            h += joyH;
            v += joyV;
        }

        // ==========================================================
        // 🔥 移動方向：車子前方是 X+（transform.right）
        // ==========================================================

        // ----- 移動 -----
        if (Mathf.Abs(v) > 0.1f)
        {
            Vector3 velocity = currentCar.transform.right * (v * carMoveSpeed);
            carRb.velocity = new Vector3(velocity.x, carRb.velocity.y, velocity.z);
        }
        else
        {
            carRb.velocity = new Vector3(0f, carRb.velocity.y, 0f);
        }

        // ----- 轉向：有在前後移動才能轉 -----
        if (Mathf.Abs(v) > 0.1f)
        {
            float rotationDirection = v > 0 ? 1f : -1f;

            Quaternion rot = Quaternion.Euler(
                0f,
                h * carRotateSpeed * rotationDirection * Time.deltaTime,
                0f
            );

            carRb.MoveRotation(carRb.rotation * rot);
        }
        // ===== 防翻 =====
        float tilt = Vector3.Dot(currentCar.transform.up, Vector3.up);
        if (tilt < 0.8f)
        {
            Vector3 torque = Vector3.Cross(currentCar.transform.up, Vector3.up);
            carRb.AddTorque(torque * 10f);
        }
    }
    void SetPlayerVisible(bool visible)
    {
        foreach (var rend in GetComponentsInChildren<Renderer>())
            rend.enabled = visible;
    }

    // ===== 攻擊/撿物品/上車/下車 =====
    GameObject FindNearbyCar()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, 1f, carResults);

        for (int i = 0; i < count; i++)
        {
            Collider hit = carResults[i];

            if (!hit.CompareTag("Car") && !hit.CompareTag("myCar"))
                continue;

            Vector3 toCar = (hit.transform.position - transform.position).normalized;

            // 👉 用 Dot 取代 Angle（更快）
            if (Vector3.Dot(transform.forward, toCar) > 0.7f)
            {
                return hit.gameObject;
            }
        }

        return null;
    }
    public GameObject GetFrontInfo(float radius, float maxAngle = 45f)
    {
        if (Camera.main == null)
        {
            return null;
        }
        Vector3 origin = Camera.main.transform.position;
        Vector3 forward = Camera.main.transform.forward;

        Collider[] cols = Physics.OverlapSphere(origin, radius);

        GameObject bestInfo = null;
        float bestDist = Mathf.Infinity;

        foreach (Collider col in cols)
        {
            if (!col.CompareTag("info"))
                continue;

            Vector3 closestPoint = col.ClosestPoint(origin);
            Vector3 toTarget = closestPoint - origin;

            if (toTarget.sqrMagnitude < 0.001f)
                continue;

            Vector3 dir = toTarget.normalized;
            float angle = Vector3.Angle(forward, dir);

            if (angle <= maxAngle)
            {
                float dist = toTarget.magnitude;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestInfo = col.gameObject;
                }
            }
        }

        return bestInfo;
    }
    public GameObject GetFrontNPC(float radius, float maxAngle = 45f)
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        // ✅ 不產生GC
        int count = Physics.OverlapSphereNonAlloc(origin, radius, npcResults);

        candidates.Clear();

        for (int i = 0; i < count; i++)
        {
            Collider col = npcResults[i];

            if (!col.CompareTag("NPC") && !col.CompareTag("Obj") && !col.CompareTag("Animal"))
                continue;

            Vector3 dir = (col.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dir);

            if (angle <= maxAngle)
            {
                candidates.Add(col.gameObject);
            }
        }

        if (candidates.Count == 0)
        {
            UpdateSelection(null);
            return null;
        }

        GameObject best = null;
        float bestDist = Mathf.Infinity;

        foreach (var npc in candidates)
        {
            float dist = (npc.transform.position - origin).sqrMagnitude; // ⭐ 比 Distance 快
            if (dist < bestDist)
            {
                bestDist = dist;
                best = npc;
            }
        }

        UpdateSelection(best);

        return best;
    }
    void UpdateSelection(GameObject newNPC)
    {
        // ✅ 沒變就不要重做
        if (lastSelectedNPC == newNPC)
            return;

        // --- 清除舊的 ---
        if (lastSelectedNPC != null)
        {
            var emp = lastSelectedNPC.GetComponent<EmployeeMovement>();
            emp?.UnfreezeFromGive();
        }

        lastSelectedNPC = newNPC;

        // --- 沒選到 ---
        if (newNPC == null)
        {
            selectionMarker.SetActive(false);
            return;
        }

        // --- 設定 marker ---
        Transform head = newNPC.transform.Find("HeadMarker");
        if (head != null)
        {
            selectionMarker.transform.position = head.position;
            selectionMarker.SetActive(true);
        }

        // --- Freeze ---
        var empNew = newNPC.GetComponent<EmployeeMovement>();
        if (empNew != null && !empNew.isEscaping)
        {
            empNew.FreezeForGive(transform);
        }
    }

    public void GiveItemToNPC(GameObject npc)
    {
        if (heldObject == null || npc == null) return;
        playerInventory.RemoveItem(heldObject.GetComponent<ItemHolder>()?.item, 1);
        animator.SetTrigger("give");

        Animator npcAnim = npc.GetComponent<Animator>();
        if (npcAnim != null)
        {
            npcAnim.SetTrigger("receive");

        }
        npc.GetComponent<EmployeeMovement>()?.ShowBubble("給我這個幹嘛？");
        npc.GetComponent<NPCMovement>()?.receiveItem(heldObject);
        npc.GetComponent<AnimalMovement>()?.receiveItem(heldObject);
        Destroy(heldObject);
        heldObject = null;
    }

    public void HandleActionButton()
    {
        if (GameManager.Instance.onEnding)
        {
            return; // 過關畫面中不處理行動按鈕
        }
        PlayerAction action = GetCurrentAction();

        switch (action)
        {
            case PlayerAction.Give:
                GiveItemToNPC(cachedFrontNPC);
                break;

            case PlayerAction.Drop:
                DropHeldItem();
                break;

            case PlayerAction.ExitCar:
                ExitCar();
                break;

            case PlayerAction.EnterCar:
                TryEnterCar(cachedNearbyCar);
                break;

            case PlayerAction.PickUp:
                targetItem = cachedNearbyItem;
                if (targetItem != null)
                {
                    isPicking = true;
                    animator.SetTrigger("pickup");
                }
                break;
            case PlayerAction.Hit:
                isHitting = true;

                animator.SetTrigger("hit");
                break;
            case PlayerAction.Sleep:
                GameManager.Instance.SleepToMorning(transform.position, false);
                break;
            case PlayerAction.dance:
                animator.SetBool("isDancing", true);
                PlayerPrefs.SetInt("rayDanced", 1);
                break;
            case PlayerAction.RefillGas:
                PlayerPrefs.SetInt("hasRefillGas", 1);
                ShowBubble("加滿油了！");
                playerInventory.RemoveItem(heldObject.GetComponent<ItemHolder>()?.item, 1);
                Destroy(heldObject);
                heldObject = null;
                break;
            case PlayerAction.read:
                GameObject frontInfo = GetFrontInfo(3f, 45f);
                if (frontInfo != null)
                {
                    InfoObject info = frontInfo.GetComponent<InfoObject>();
                    if (info != null)
                    {
                        infoPanel.SetActive(true);
                        for (int i = 0; i < boards.Length; i++)
                        {
                            boards[i].SetActive(i == info.infoID);
                        }
                        //infoPanel.GetComponent<InfoPanel>().ShowInfo(info.infoID);
                    }
                }
                break;
        }
    }
    public void OnDanceEnd()
    {
        animator.SetBool("isDancing", false);
    }
    public void OnPickItemAttach()
    {
        if (targetItem == null) return;

        Rigidbody r = targetItem.GetComponent<Rigidbody>();
        if (r) r.isKinematic = true;

        Collider c = targetItem.GetComponent<Collider>();
        if (c) c.enabled = false;

        targetItem.transform.SetParent(rightHand);
        targetItem.transform.localPosition = Vector3.zero;
        targetItem.transform.localRotation = Quaternion.identity;
    }

    public void OnPickItemFinish()
    {

        if (targetItem == null)
        {
            Debug.LogError("OnPickItemFinish：targetItem 為 null");
            return;
        }

        ItemHolder holder = targetItem.GetComponent<ItemHolder>();
        holder.EnsureRuntimeItem();
        if (holder == null || holder.runtimeItem == null)
        {
            Debug.LogError("OnPickItemFinish：targetItem 沒有 ItemHolder 或 runtimeItem 沒設定");
            isPicking = false;
            return;
        }

        playerInventory.AddItem(holder.runtimeItem, 1, allowStack: false);

        // 更新 UI
        if (inventoryUI != null)
            inventoryUI.RefreshUI();

        // 銷毀場景物件
        Destroy(targetItem);
        targetItem = null;
        heldObject = null;
        isPicking = false;
        //完成物品撿取
        Debug.Log("Picked up: " + holder.runtimeItem.name);
    }
    public string GetHeldItemID()
    {
        if (heldObject == null) return null;

        ItemHolder holder = heldObject.GetComponent<ItemHolder>();
        if (holder != null && holder.runtimeItem != null)
            return holder.runtimeItem.id;

        return null;
    }

    void UpdateHitButtonText()
    {
        if (hitButtonText == null) return;

        PlayerAction action = GetCurrentAction();

        switch (action)
        {
            case PlayerAction.Give:
                hitButtonText.text = "給予";
                break;
            case PlayerAction.ExitCar:
                hitButtonText.text = "下車";
                break;
            case PlayerAction.EnterCar:
                hitButtonText.text = "上車";
                break;
            case PlayerAction.Drop:
                hitButtonText.text = "放下";
                break;
            case PlayerAction.PickUp:
                hitButtonText.text = "取得";
                break;
            case PlayerAction.Hit:
                hitButtonText.text = "揮拳";
                break;
            case PlayerAction.Sleep:
                hitButtonText.text = "睡覺";
                break;
            case PlayerAction.dance:
                hitButtonText.text = "跳舞";
                break;
            case PlayerAction.RefillGas:
                hitButtonText.text = "加油";
                break;
            case PlayerAction.read:
                hitButtonText.text = "閱讀";
                break;
            default:
                hitButtonText.text = "";
                break;
        }
    }

    bool CanPickItem()
    {
        if (isPicking || isHitting || isJumping || isJumpPreparing || isLanding)
            return false;

        if (!animator.GetBool("isWalking") && animator.GetFloat("moveZ") != 0f)
            return false;

        return cachedNearbyItem != null;
    }

    GameObject FindNearbyItem()
    {
        Vector3 origin = transform.position + transform.forward * 0.5f;

        int count = Physics.OverlapSphereNonAlloc(origin, 0.4f, itemResults);

        for (int i = 0; i < count; i++)
        {
            if (itemResults[i].CompareTag("item"))
            {
                return itemResults[i].gameObject;
            }
        }

        return null;
    }
    public void TalkButton()
    {
        if (GameManager.Instance.onEnding)
        {
            return; // 過關畫面中不處理對話
        }
        GameObject npc = cachedFrontNPC; // 直接使用快取的 NPC，避免重複計算
        if (npc != null)
        {
            npc.GetComponent<EmployeeMovement>()?.response();
            npc.GetComponent<NPCMovement>()?.response();
        }
        else
        {
            ShowBubble("好無聊呀！");
        }
    }
    public void JumpButton()
    {
        Debug.Log("transform position when jump: " + transform.position);
        if (GameManager.Instance.onEnding)
        {
            return; // 過關畫面中不處理跳躍
        }
        //PlayerPrefs.SetInt("rayDanced", 1);
        if (isHitting) return;
        if (isGround && !isJumpPreparing && !isJumping)
        {
            //GameManager.Instance.ShowEnding();
            isJumpPreparing = true;
            animator.SetTrigger("jump");
        }
    }

    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CheckHitEnemy()
    {
        Vector3 center = transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;

        int count = Physics.OverlapSphereNonAlloc(center, 0.4f, enemyResults);

        for (int i = 0; i < count; i++)
        {
            Collider col = enemyResults[i];

            col.GetComponent<EmployeeMovement>()?.BeBeaten();
            col.GetComponent<NPCMovement>()?.BeBeaten();
            col.GetComponent<GhostScript>()?.BeBeaten();
            col.GetComponent<MonsterCtrl>()?.BeBeaten();
        }

        if (playerInventory.GetItemCount("11") == 7)
        {
            SpawnPunchEffect();
        }
    }

    public void OnJumpForce()
    {
        float baseJumpSpeed = 5f;
        float extraPerSpring = 5f;
        float maxJumpSpeed = 40f;

        int springCount = playerInventory.GetItemCount("9");

        float finalJumpSpeed = baseJumpSpeed + (springCount * extraPerSpring);

        // ⭐ 限制上限
        finalJumpSpeed = Mathf.Clamp(finalJumpSpeed, 0f, maxJumpSpeed);

        rb.AddForce(Vector3.up * finalJumpSpeed, ForceMode.Impulse);

        Debug.Log($"Jump initiated! Spring count: {springCount}, Final Jump Speed: {finalJumpSpeed}");

        isJumpPreparing = false;
        isJumping = true;
    }


    public void DropHeldItem()
    {
        if (heldObject == null) return;

        ItemHolder holder = heldObject.GetComponent<ItemHolder>();
        if (holder == null)
        {
            Debug.LogError("DropHeldItem：heldObject 沒有 ItemHolder");
            return;
        }

        if (holder.runtimeItem == null)
        {
            holder.EnsureRuntimeItem();
            //Debug.LogError("DropHeldItem：runtimeItem 為 null");
            //return;
        }

        // ✅ 安全傳給 Inventory
        playerInventory.RemoveItem(holder.runtimeItem, 1);
        if (inventoryUI != null)
            inventoryUI.RefreshUI();

        // 原本丟掉物品流程
        GameObject obj = heldObject;
        heldObject = null;

        obj.transform.SetParent(null);

        Rigidbody r = obj.GetComponent<Rigidbody>();
        Collider c = obj.GetComponent<Collider>();

        float offsetY = (c != null) ? c.bounds.extents.y : 0.5f;

        Vector3 dropPos = transform.position + transform.forward * 0.5f;
        dropPos.y += offsetY + 0.5f;
        obj.transform.position = dropPos;
        if (r)
        {
            r.isKinematic = false;
            r.velocity = Vector3.zero;

            Vector3 throwDir = transform.forward * 0.1f + Vector3.up;
            r.AddForce(throwDir * 0.2f, ForceMode.Impulse);
        }

        if (c)
            c.enabled = true;
    }
    PlayerAction GetCurrentAction()
    {
        bool hasItem = heldObject != null;
        bool canAct = !isJumping && !isHitting && !isPicking;
        //Debug.Log("canAct: " + canAct+", isJumping: "+isJumping+", isHitting: "+isHitting+", isPicking: "+isPicking);
        GameObject frontNPC = cachedFrontNPC;
        GameObject nearbyCar = cachedNearbyCar;
        GameObject frontInfo = GetFrontInfo(3f, 45f);

        // 1️⃣ 睡覺（高優先）
        if (currentSleepArea != null && canAct && !isInCar)// && GameManager.Instance.CanSleepNow()) //暫時不限制晚上才能睡
            return PlayerAction.Sleep;

        // 1.5️⃣ 跳舞
        if (canDance && canAct && !isInCar && PlayerPrefs.GetInt("xinyuanReady", 0) == 4) // 在心圓活動區正在跳舞環節時才可跳舞
            return PlayerAction.dance;

        // 2️⃣ 給予 NPC
        if (hasItem && canAct && !isInCar && frontNPC != null)
            return PlayerAction.Give;

        // 3️⃣ 下車
        if (isInCar)
            return PlayerAction.ExitCar;

        // 4️⃣ 撿東西
        if (!hasItem && CanPickItem())
            return PlayerAction.PickUp;

        // 4.5️⃣ 加油 (如果玩家手上拿的是油桶，且附近有myCar)
        if (hasItem && heldObject.GetComponent<ItemHolder>()?.item.id == "7" && nearbyCar != null && nearbyCar.CompareTag("myCar"))
            return PlayerAction.RefillGas;


        // 5️⃣ 上車
        if (!hasItem && nearbyCar != null)
            return PlayerAction.EnterCar;

        // 6️⃣ 放下
        if (hasItem)
            return PlayerAction.Drop;
        if (frontInfo != null && canAct && !isInCar)
            return PlayerAction.read;
        // 7️⃣ 揮拳
        if (isGround && canAct)
            return PlayerAction.Hit;



        return PlayerAction.None;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Player entered trigger: " + other.name);
        SleepArea area = other.GetComponent<SleepArea>();
        if (area != null)
            currentSleepArea = area;
        DanceArea danceArea = other.GetComponent<DanceArea>();
        if (danceArea != null)
            canDance = true;

    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("Player exited trigger: " + other.name);
        SleepArea area = other.GetComponent<SleepArea>();
        if (area != null && currentSleepArea == area)
            currentSleepArea = null;
        DanceArea danceArea = other.GetComponent<DanceArea>();
        if (danceArea != null)
            canDance = false;
    }
    void PlayCarEnterSound()
    {
        if (sfxSource == null || carEnterClip == null) return;

        sfxSource.clip = carEnterClip;
        sfxSource.loop = false; // 確保不循環
        sfxSource.Play();
    }
    void StopCarEnterSound()
    {
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }

    public void SpawnPunchEffect()
    {
        Vector3 origin = transform.position + transform.up * 0.5f; // 從玩家胸口位置發出射線
        Vector3 dir = transform.forward;

        float distance = 2f;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, hitMask))
        {
            Instantiate(punchEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else
        {
            Vector3 spawnPos = origin + dir * distance;
            Instantiate(punchEffectPrefab, spawnPos, Quaternion.LookRotation(dir));
        }
    }

    public void OnLanding() => isLanding = true;
    public void OnReady() => isLanding = false;
    public void OnHitEnd() => isHitting = false;
    public bool IsMobile()
    {
        return isMobile;
    }
}

public enum PlayerAction
{
    None,
    Give,
    Drop,
    EnterCar,
    ExitCar,
    PickUp,
    Hit,
    Sleep,        // ⭐ 新增
    dance,
    read,
    RefillGas   //用玩家手上的油給車子加油
}
