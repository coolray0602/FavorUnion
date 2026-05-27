using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI 元件")]
    public CanvasGroup menuPanel;   // Menu（淡出用）
    public GameObject controlUI;    // 整組操作UI
    public RectTransform endingText;       // TMP 文字 RectTransform
    public TMP_Text endingTextComponent;    // TMP 文字元件
    public GameObject panel;               // 黑色半透明 Panel
    public Button newStartButton;           // 重新開始按鈕
    public Button continueButton;          // 繼續遊戲按鈕
    public TMP_Text fpsText;              // FPS 顯示
    
    [Header("效能設定")]
    public bool showFPS = true;           // 是否顯示 FPS
    public float fpsUpdateInterval = 0.5f; // FPS 更新間隔（秒）
    
    [Header("Camera")]
    public Transform cameraTransform;
    public Transform cameraTarget; // 玩家背後的位置
    public float moveDuration = 3f;

    [Header("Player")]
    public MonoBehaviour playerController; // 你的控制腳本

    private Vector3 camStartPos;
    private Quaternion camStartRot;


    public bool onEnding = false;              // 是否正在過關畫面中
    [Header("設定")]
    public float scrollSpeed = 50f;        // 上滑速度

    private bool isEnding = false;
    private float targetY;
    public static GameManager Instance;
    private int currentDay = 1;
    private float playTime = 0f;
    [Header("BGM")]
    public AudioSource bgmSource;

    public AudioClip[] dayMusics;
    public AudioClip[] nightMusics;
    public AudioClip endingMusic;
    public AudioClip manTalkingClip;
    public AudioClip womanTalkingClip;
    public AudioClip playerTalkingClip;
    public AudioClip hitClip;
    public AudioClip damagedClip;

    // =======================
    // 時間設定
    // =======================
    [Header("時間設定（秒）")]
    public float dayDuration = 240f;
    public float nightDuration = 120f;

    private float timer = 0f;
    public bool isDay = true;
    public bool monsterExist = false;
    
    // =======================
    // 太陽與月亮
    // =======================
    [Header("太陽與天空")]
    public Light sunLight;
    public Color dayColor = Color.white;
    public Color dawnColor = new Color(1f, 0.85f, 0.6f);
    public Color duskColor = new Color(1f, 0.7f, 0.5f);
    public Color nightColor = Color.black;

    [Header("月亮（Quad）")]
    public GameObject moonQuad;
    public float moonDistance = 500f;

    // =======================
    // 員工系統
    // =======================
    [Header("員工設定")]
    public GameObject[] employeePrefabs;
    public Transform spawnPoint;
    public Transform[] destinations;
    public Transform[] restaurants;
    public Transform exitPoint; // 怪物出現時 NPC 逃往的出口

    [Header("生成設定")]
    public float spawnInterval = 5f;
    [Header("動物生成設定")]
    public GameObject animalPrefab;
    public Transform animalSpawnPoint;
    [Header("員工自言自語台詞")]
    public string[] morningMessages = { "上班囉", "上班使我快樂", "努力工作" };
    public string[] noonMessages = { "吃飯囉", "肚子餓了", "午餐時間" };
    public string[] noonPostMessages = { "上班囉", "努力工作", "加油" };
    public string[] eveningMessages = { "吃飯囉", "肚子餓了" };
    public string[] nightMessages = { "下班囉", "又是充實的一天", "回家休息" };
    
    // =======================
    // Ghost 設定
    // =======================
    [Header("Ghost Settings")]
    public GameObject ghostPrefab;

    // 用來記錄目前場上的 ghosts
    private List<GameObject> activeGhosts = new List<GameObject>();
    
    // =======================
    // Game Over UI
    // =======================
    [Header("Game Over 介面")]
    public GameObject gameOverUI;
    public Button restartButton;

    [Header("Cars")]
    private CarController[] cars;
    [Header("Sleep / Reset Settings")]
    public Transform playerSpawnPoint;
    [Header("睡覺介面")]
    public GameObject sleepUI;
    public GameObject faintedPicture;
    public GameObject sleepPicture;

    private Dictionary<CarController, Vector3> carStartPositions = new();

    [Header("Key Settings")]
    public GameObject keyPrefab;
    public Transform[] keySpawnPoints; // 5 個位置

    private List<string> allPlateNumbers = new List<string>();
    public bool isGameOver = false;

    // =======================
    // Timed Spawn 設定
    // =======================
    [System.Serializable]
    public class TimedSpawnSetting
    {
        [Header("生成設定")]
        public string spawnName;
        public GameObject prefab;
        public Transform spawnPoint;
        public float spawnTime = 0f;
        public float spawnTimeOnXinYuan = 0f;
        [HideInInspector] public bool spawned = false;

        [Header("巡邏目的地（直接指定）")]
        public Transform[] patrolPoints;
        public Transform xinyuanPoint;
        [Header("夜晚出口（可空）")]
        public Transform exitPoint;
    }
    [Header("定時生成設定")]
    public TimedSpawnSetting[] timedSpawns;

    private GameObject player;
    private PlayerCtrl playerCtrl;
    
    // =======================
    // 效能優化相關變數
    // =======================
    private float fpsTimer = 0f;
    private int frameCount = 0;
    private float currentFPS = 0f;
    private float lastTimeUpdateTime = 0f;
    private const float TIME_UPDATE_INTERVAL = 0.1f; // 時間更新間隔
    
    // 對象池（可選）
    private Dictionary<GameObject, Queue<GameObject>> employeePools = new Dictionary<GameObject, Queue<GameObject>>();
    private int maxEmployeeCount = 50; // 最大員工數量限制
    
    // =======================
    // 初始化
    // =======================
    private void Awake()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        //測試心圓會, 讓第一次進入遊戲的玩家直接達到心圓會舉辦當天的狀態，之後會重置回未準備狀態
        //PlayerPrefs.SetInt("xinyuanReady", 2);

        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        GameObject[] carObjects = GameObject.FindGameObjectsWithTag("Car");
        cars = new CarController[carObjects.Length];
        for (int i = 0; i < carObjects.Length; i++)
        {
            cars[i] = carObjects[i].GetComponent<CarController>();
            if (cars[i] == null)
                Debug.LogWarning($"Car object '{carObjects[i].name}' 沒有 CarController 元件！");
        }
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerCtrl = player.GetComponent<PlayerCtrl>();
            
        // 效能設定
        Application.targetFrameRate = 30; // 手機上限制幀率
        Time.fixedDeltaTime = 0.0333f; // 30 FPS 的物理更新間隔
    }

    private void Start()
    {
        // 初始關閉控制
        playerController.enabled = false;
        controlUI.SetActive(false);
        menuPanel.gameObject.SetActive(true);
        camStartPos = cameraTransform.position;
        camStartRot = cameraTransform.rotation;

        AssignPlateNumbersToCars();
        SpawnKeys();
        //生成動物
        if (animalPrefab != null && animalSpawnPoint != null)
        {
            Instantiate(animalPrefab, animalSpawnPoint.position, animalSpawnPoint.rotation);
        }

        foreach (var car in cars)
            if (car != null)
                carStartPositions[car] = car.transform.position;

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        if (newStartButton != null)
            newStartButton.onClick.AddListener(RestartGame);
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (moonQuad != null)
            moonQuad.SetActive(false);

        // 初始化 FPS 顯示
        if (fpsText != null)
            fpsText.gameObject.SetActive(showFPS);

        StartCoroutine(TimeRoutine());
        StartCoroutine(SpawnCycleRoutine());
        if (isDay)
            PlayRandomBGM(dayMusics);
        else
            PlayRandomBGM(nightMusics);
    }
    
    private void Update()
    {
        // FPS 計算
        if (showFPS && fpsText != null)
        {
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;
            
            if (fpsTimer >= fpsUpdateInterval)
            {
                currentFPS = frameCount / fpsTimer;
                UpdateFPSDisplay();
                frameCount = 0;
                fpsTimer = 0f;
            }
        }
        
        if (!isEnding)
        {
            playTime += Time.deltaTime;
            return;
        }

        // 上滑文字
        endingText.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        // 判斷是否到達目標位置
        if (endingText.anchoredPosition.y >= targetY)
        {
            isEnding = false;

            // 將文字精確對齊
            endingText.anchoredPosition = new Vector2(0, targetY);

            // 顯示按鈕
            newStartButton.gameObject.SetActive(true);
            continueButton.gameObject.SetActive(true);
        }
        if (hasStarted) return;

        if (Input.anyKeyDown)
        {
            hasStarted = true;
            StartCoroutine(StartGameSequence());
        }
    }
    
    // 更新 FPS 顯示
    private void UpdateFPSDisplay()
    {
        if (fpsText == null) return;
        
        string colorCode = GetFPSColorCode(currentFPS);
        fpsText.text = $"<color={colorCode}>FPS: {Mathf.RoundToInt(currentFPS)}</color>";
    }
    
    // 根據 FPS 返回顏色
    private string GetFPSColorCode(float fps)
    {
        if (fps >= 25) return "#00FF00";  // 綠色 - 流暢
        if (fps >= 15) return "#FFFF00";  // 黃色 - 可接受
        return "#FF0000";                  // 紅色 - 卡頓
    }
    
    public void SetShowFPS(bool show)
    {
        showFPS = show;
        if (fpsText != null)
            fpsText.gameObject.SetActive(show);
    }
    
    public string GetPlayTimeString()
    {
        int totalSeconds = Mathf.FloorToInt(playTime);

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
    
    // =======================
    // 睡覺
    // =======================
    public void SleepToMorning(Vector3 pos, bool fainted)
    {
        if (isGameOver) return;
        StartCoroutine(SleepRoutine(pos, fainted));
    }

    IEnumerator SleepRoutine(Vector3 pos, bool fainted)
    {
        monsterExist = false; // 睡覺時怪物不存在
        if (sleepUI != null)
            sleepUI.SetActive(true);
        if (faintedPicture != null)
            faintedPicture.SetActive(fainted);
        if (sleepPicture != null)
            sleepPicture.SetActive(!fainted);

        playerCtrl.resetStatus();
        playerCtrl.isLanding = true;
        playerCtrl.animator.SetTrigger("wakeup");

        // ⭐ 記錄睡覺前的時間狀態
        bool sleptDuringDay = isDay;

        // ⭐ 重置世界（清除 NPC、鬼魂、動物等，但不影響時間狀態）
        ResetWorldForNewDay();

        // ⭐ 重置計時器
        timer = 0f;

        yield return new WaitForSeconds(3f);

        if (sleepUI != null)
            sleepUI.SetActive(false);

        // ⭐ 根據睡覺時間決定醒來後的時間和音樂
        if (sleptDuringDay)
        {
            // 白天睡覺 → 睡到晚上
            isDay = false;
            PlayRandomBGM(nightMusics);   // 🌙 切夜晚音樂
            SpawnGhosts();                 // 生成鬼魂
            playerCtrl.ShowBubble("怎麼天黑了...");
        }
        else
        {
            // 夜晚睡覺 → 睡到隔天早上
            isDay = true;
            currentDay++;                  // 新的一天
            PlayRandomBGM(dayMusics);      // ☀️ 切白天音樂
            playerCtrl.ShowBubble("新的一天開始了！");
        }

        if (player != null)
            player.transform.position = pos;

        if (fainted)
        {
            playerCtrl.ShowBubble("我怎麼會昏倒了...?");
        }

        // 重置車輛狀態
        foreach (var pair in carStartPositions)
        {
            if (pair.Key == null) continue;

            pair.Key.transform.position = pair.Value;
            pair.Key.ResetState();
        }
    }
    
    // 呼叫此函數啟動過關畫面
    public void ShowEnding()
    {
        isEnding = true;
        onEnding = true;
        if (endingText == null || panel == null || newStartButton == null || continueButton == null)
            return;

        ClearGhosts();
        // 先啟用 Panel 和 TMP，並隱藏按鈕
        panel.SetActive(true);
        string timeStr = GameManager.Instance.GetPlayTimeString();
        endingTextComponent.text =
        $@"你成功打敗了可怕的惡龍，

拯救了公司全體員工！

曾經的擔心與恐懼，

如今已煙消雲散。

員工們重獲自由，

臉上重新綻放笑容。

總計耗時：

{timeStr}

遊戲中度過了 {currentDay} 個晝夜

辛苦你了！！

製作人員名單

遊戲設計：	Ray Liu

美術繪製：	Ray Liu

程式開發：	Ray Liu

音效製作：	Ray Liu

UI/UX 設計：	Ray Liu

測試與調整：	Ray Liu

感謝你一路以來的陪伴與支持！

希望這段冒險帶給你歡笑、挑戰與成就感。

願勇者的精神常伴你左右！

— Ray Liu 敬上 —";

        endingText.gameObject.SetActive(true);
        newStartButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);

        // 設置文字初始位置在 Panel 底部
        endingText.anchoredPosition = new Vector2(0, -panel.GetComponent<RectTransform>().rect.height);

        // 計算目標位置：文字下緣對齊 Panel 下緣
        float panelHeight = panel.GetComponent<RectTransform>().rect.height;
        float textHeight = endingText.rect.height;
        targetY = textHeight - panelHeight; // 文字上滑到下緣對齊 Panel 下緣


        PlayRandomBGM(new AudioClip[] { endingMusic }); // 播放過關音樂

    }

    // =======================
    // 日夜循環（優化版）
    // =======================
    IEnumerator TimeRoutine()
    {
        while (true)
        {
            if (GameManager.Instance.onEnding)
            {
                yield return null;
                continue;
            }
            
            timer += Time.deltaTime;
            
            // 降低更新頻率
            if (Time.time - lastTimeUpdateTime >= TIME_UPDATE_INTERVAL)
            {
                lastTimeUpdateTime = Time.time;
                UpdateTimeBasedLogic();
            }
            
            yield return null;
        }
    }
    
    private void UpdateTimeBasedLogic()
    {
        if (isDay)
        {
            float morningEnd = dayDuration / 6f;
            float eveningStart = dayDuration * 5f / 6f;

            if (timer < morningEnd)
                sunLight.color = Color.Lerp(dawnColor, dayColor, timer / morningEnd);
            else if (timer > eveningStart)
                sunLight.color = Color.Lerp(dayColor, duskColor, (timer - eveningStart) / (dayDuration / 6f));
            else
                sunLight.color = dayColor;

            float sunAngle = Mathf.Lerp(0f, 180f, timer / dayDuration);
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 0, 0);

            if (moonQuad != null)
                moonQuad.SetActive(false);

            if (timer >= dayDuration)
            {
                timer = 0f;
                isDay = false;
                PlayRandomBGM(nightMusics);
                SpawnGhosts();
            }
        }
        else
        {
            sunLight.transform.rotation = Quaternion.Euler(-90f, 0, 0);
            sunLight.color = nightColor;

            float moonAngle = Mathf.Lerp(0f, 180f, timer / nightDuration);

            if (moonQuad != null)
            {
                moonQuad.SetActive(true);
                Vector3 moonPos = new Vector3(
                    0,
                    Mathf.Sin(Mathf.Deg2Rad * moonAngle) * moonDistance,
                    Mathf.Cos(Mathf.Deg2Rad * moonAngle) * moonDistance
                );
                moonQuad.transform.position = moonPos;
                if (Camera.main != null)
                    moonQuad.transform.LookAt(Camera.main.transform);
            }

            if (timer >= nightDuration)
            {
                timer = 0f;
                isDay = true;
                PlayRandomBGM(dayMusics);
                currentDay++;
                ClearGhosts();
            }
        }
        
        // Timed Spawn 檢查
        foreach (var setting in timedSpawns)
        {
            if (setting.spawned) continue;
            
            bool isXinYuanTime = PlayerPrefs.GetInt("xinyuanReady", 0) == 2;
            bool isCorrectTime = (!isXinYuanTime && timer >= setting.spawnTime) || (isXinYuanTime && timer >= setting.spawnTimeOnXinYuan);

            if (isCorrectTime)
            {
                SpawnTimedObject(setting);
            }
        }
    }
    
    private void SpawnTimedObject(TimedSpawnSetting setting)
    {
        GameObject obj = Instantiate(
            setting.prefab,
            setting.spawnPoint.position,
            setting.spawnPoint.rotation
        );

        obj.name = setting.spawnName;

        NPCMovement movement = obj.GetComponent<NPCMovement>();
        if (movement != null)
        {
            bool isXinYuanTime = PlayerPrefs.GetInt("xinyuanReady", 0) == 2;
            if (isXinYuanTime)
                movement.InitXinyuan(setting.xinyuanPoint);
            else
                movement.InitPatrol(setting.patrolPoints);

            movement.exitPoint = setting.exitPoint;
        }
        else
        {
            Debug.LogWarning($"{obj.name} 沒有 NPCMovement 元件，無法設定巡邏點");
        }
        setting.spawned = true;
    }

    // =======================
    // 員工生成（優化版）
    // =======================
    IEnumerator SpawnCycleRoutine()
    {
        bool morningDone = false;
        bool noonDone = false;
        bool noonPostDone = false;
        bool eveningDone = false;
        bool nightDone = false;
        int lastSpawnDay = currentDay;
        
        while (true)
        {
            if (currentDay != lastSpawnDay)
            {
                lastSpawnDay = currentDay;

                morningDone = false;
                noonDone = false;
                noonPostDone = false;
                eveningDone = false;
                nightDone = false;
                if (PlayerPrefs.GetInt("xinyuanReady", 0) >= 3)
                {
                    PlayerPrefs.SetInt("xinyuanReady", 0);
                }
            }
            if (isGameOver)
            {
                yield return null;
                continue;
            }

            if (isDay && !morningDone && timer >= 0f)
            {
                PlayerPrefs.SetInt("notNoon", 1);
                PlayerPrefs.SetInt("lunchServed", 0);
                morningDone = true;
                StartCoroutine(SpawnPhase(new Transform[] { spawnPoint }, destinations, morningMessages, 8)); // 減少數量
                Debug.Log("早上開始了");
            }

            if (isDay && !noonDone && timer >= dayDuration / 2f)
            {
                PlayerPrefs.SetInt("notNoon", 0);
                noonDone = true;
                StartCoroutine(SpawnPhase(destinations, restaurants, noonMessages, 8));
                Debug.Log("中午開始了");
            }

            if (isDay && !noonPostDone && timer >= dayDuration / 2f + 30f)
            {
                PlayerPrefs.SetInt("notNoon", 1);
                noonPostDone = true;
                StartCoroutine(SpawnPhase(restaurants, destinations, noonPostMessages, 8));
                Debug.Log("中午結束了，下午開始了");
            }

            if (isDay && !eveningDone && timer >= dayDuration * 5f / 6f)
            {
                PlayerPrefs.SetInt("notNoon", 1);
                eveningDone = true;
                StartCoroutine(SpawnPhase(destinations, restaurants, eveningMessages, 8));
                StartCoroutine(SpawnPhase(new Transform[] { spawnPoint }, destinations, morningMessages, 8));
                Debug.Log("傍晚開始了，員工們開始下班了，但也有新的員工上班了");
            }

            if (!isDay && !nightDone && timer >= nightDuration / 3f)
            {
                PlayerPrefs.SetInt("notNoon", 1);
                nightDone = true;
                StartCoroutine(SpawnPhase(destinations, new Transform[] { spawnPoint }, nightMessages, 8));
                Debug.Log("夜晚開始了");
            }

            yield return null;
        }
    }

    IEnumerator SpawnPhase(Transform[] origins, Transform[] targets, string[] messages, int targetCount)
    {
        int count = 0;
        int currentEmployeeCount = GameObject.FindGameObjectsWithTag("NPC").Length;
        
        while (count < targetCount && !isGameOver && currentEmployeeCount < maxEmployeeCount)
        {
            float waitTime = Random.Range(1f, spawnInterval);
            yield return new WaitForSeconds(waitTime);

            Transform origin = origins[Random.Range(0, origins.Length)];
            Transform dest = targets[Random.Range(0, targets.Length)];
            SpawnEmployee(origin.position, dest.position, messages);
            
            count++;
            currentEmployeeCount++;
        }
    }

    void SpawnEmployee(Vector3 startPos, Vector3 targetPos, string[] assignedMessages)
    {
        GameObject prefab = employeePrefabs[Random.Range(0, employeePrefabs.Length)];
        GameObject employee = Instantiate(prefab, startPos, Quaternion.identity);

        EmployeeMovement move = employee.GetComponent<EmployeeMovement>();
        if (move != null)
        {
            move.SetDestination(targetPos);
            move.assignedMessages = assignedMessages;
            move.exitPoint = exitPoint;
        }

        EmployeeInfo info = employee.GetComponent<EmployeeInfo>();
        Animator anim = employee.GetComponent<Animator>();
        if (info != null && anim != null)
            anim.SetBool("isMale", info.isMale);
            
        // 自動銷毀避免累積
        StartCoroutine(AutoDestroyEmployee(employee, 120f));
    }
    
    IEnumerator AutoDestroyEmployee(GameObject employee, float maxLifeTime)
    {
        yield return new WaitForSeconds(maxLifeTime);
        if (employee != null)
        {
            Destroy(employee);
        }
    }

    // =======================
    // 車牌與鑰匙
    // =======================
    void AssignPlateNumbersToCars()
    {
        allPlateNumbers.Clear();

        foreach (CarController car in cars)
        {
            string plate;
            do
            {
                plate = GenerateRandomPlate();
            }
            while (allPlateNumbers.Contains(plate));

            allPlateNumbers.Add(plate);
            car.SetPlateNumber(plate);
        }
    }

    void SpawnKeys()
    {
        if (keySpawnPoints.Length < 8)
        {
            Debug.LogError("鑰匙生成點不足 8 個！");
            return;
        }

        List<string> shuffledPlates = new List<string>(allPlateNumbers);
        Shuffle(shuffledPlates);

        List<Transform> shuffledPoints = new List<Transform>(keySpawnPoints);
        Shuffle(shuffledPoints);

        for (int i = 0; i < 8; i++)
        {
            GameObject keyObj = Instantiate(keyPrefab, shuffledPoints[i].position, shuffledPoints[i].rotation);

            ItemHolder holder = keyObj.GetComponent<ItemHolder>();
            holder.InitRuntimeItem(shuffledPlates[i]);
        }
    }

    string GenerateRandomPlate() => Random.Range(0, 1000000).ToString("D6");

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            (list[i], list[rand]) = (list[rand], list[i]);
        }
    }
    
    void SpawnGhosts()
    {
        if (ghostPrefab == null || destinations == null || destinations.Length == 0 || playerController.enabled == false)
            return;

        foreach (Transform point in destinations)
        {
            if (point == null) continue;

            GameObject ghost = Instantiate(
                ghostPrefab,
                point.position,
                point.rotation
            );

            activeGhosts.Add(ghost);
        }
    }
    
    public void ClearGhosts()
    {
        foreach (GameObject ghost in activeGhosts)
        {
            if (ghost != null)
                Destroy(ghost);
        }

        activeGhosts.Clear();
    }
    
    void ClearMonsters()
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject monster in monsters)
        {
            Destroy(monster);
        }
        monsterExist = false;
    }

    // =======================
    // Game Over
    // =======================
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void ContinueGame()
    {
        onEnding = false;
        Time.timeScale = 1f;
        if (panel != null)
            panel.SetActive(false);
        if (endingText != null)
            endingText.gameObject.SetActive(false);
        if (isDay)
            PlayRandomBGM(dayMusics);
        else
            PlayRandomBGM(nightMusics);
    }
    
    // =======================
    // 清空世界（睡醒 / 昏倒用）
    // =======================
    void ResetWorldForNewDay()
    {
        Debug.Log("Resetting world for new day...");

        if (!isDay)
        {
            if (PlayerPrefs.GetInt("xinyuanReady", 0) == 1)
            {
                PlayerPrefs.SetInt("xinyuanReady", 2);
                PlayerPrefs.SetInt("xinyuanGroup", 0);
            }
            else if (PlayerPrefs.GetInt("xinyuanReady", 0) >= 2)
            {
                PlayerPrefs.SetInt("xinyuanReady", 0);
            }
            PlayerPrefs.SetInt("rayDanced", 0);

            foreach (GameObject npc in GameObject.FindGameObjectsWithTag("NPC"))
            {
                if (npc.GetComponent<NPCMovement>() != null && npc.GetComponent<NPCMovement>().stayPut)
                    continue;
                Destroy(npc);
            }
        }
        
        ClearGhosts();
        ClearMonsters();
        playerCtrl.fainted = false;

        foreach (var setting in timedSpawns)
        {
            setting.spawned = false;
        }

        GameObject[] animals = GameObject.FindGameObjectsWithTag("Animal");
        foreach (GameObject animal in animals)
        {
            Destroy(animal);
        }
        if (animalPrefab != null && animalSpawnPoint != null)
        {
            Instantiate(animalPrefab, animalSpawnPoint.position, animalSpawnPoint.rotation);
        }
    }
    
    // =======================
    // 日夜階段判斷（保留給員工使用）
    // =======================
    public bool IsMorningEnd() { return isDay && timer >= dayDuration / 6f; }
    public bool IsNoon() { return isDay && timer >= dayDuration / 2f; }
    public bool IsNoonPost() { return isDay && timer >= dayDuration / 2f && timer < dayDuration / 2f + 30f; }
    public bool IsEvening() { return isDay && timer >= dayDuration * 5f / 6f; }
    public bool IsNightEarly() { return !isDay && timer <= nightDuration / 3f; }

    IEnumerator FadeBGM(AudioClip[] clips)
    {
        float t = 0;
        float startVolume = bgmSource.volume;

        while (t < 1f)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0, t);
            yield return null;
        }

        int index = Random.Range(0, clips.Length);
        bgmSource.clip = clips[index];
        bgmSource.Play();

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0, startVolume, t);
            yield return null;
        }
    }

    void PlayRandomBGM(AudioClip[] clips)
    {
        if (bgmSource == null || clips.Length == 0) return;
        StartCoroutine(FadeBGM(clips));
    }
    
    public void PlayHitSound()
    {
        if (hitClip != null && player != null)
        {
            AudioSource.PlayClipAtPoint(hitClip, player.transform.position);
        }
    }
    
    public void PlayDamagedSound()
    {
        if (damagedClip != null && player != null)
        {
            AudioSource.PlayClipAtPoint(damagedClip, player.transform.position);
        }
    }
    
    public string GetCurrentTimeString()
    {
        if (isDay)
        {
            float hours = Mathf.Floor(timer / dayDuration * 12f) + 6;
            int displayHours = (int)(hours % 24);
            return $"{displayHours}點";
        }
        else
        {
            float hours = Mathf.Floor(timer / nightDuration * 12f) + 18;
            int displayHours = (int)(hours % 24);
            if (displayHours == 0) displayHours = 24;
            return $"{displayHours}點";
        }
    }

    public void HideCursor()
    {
        if (playerCtrl != null && playerCtrl.IsMobile())
            return;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    bool hasStarted = false;
    public void SetFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }
    
    public void OnClickStartGame()
    {
        if (hasStarted) return;
        hasStarted = true;
        HideCursor();
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = time / moveDuration;
            t = 1 - Mathf.Pow(1 - t, 3);

            cameraTransform.position = Vector3.Lerp(camStartPos, cameraTarget.position, t);
            cameraTransform.rotation = Quaternion.Lerp(camStartRot, cameraTarget.rotation, t);
            menuPanel.alpha = 1 - t;

            yield return null;
        }

        cameraTransform.position = cameraTarget.position;
        cameraTransform.rotation = cameraTarget.rotation;
        menuPanel.alpha = 0;
        menuPanel.gameObject.SetActive(false);

        playerController.enabled = true;
        controlUI.SetActive(true);
    }
}