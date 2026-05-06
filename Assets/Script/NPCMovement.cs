using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;


//xinyuanReady 0代表未有心圓會通知，1是已通知隔天心圓會，2是心圓會當天，3是心圓會跳舞中，4是跳完舞出發
public class NPCMovement : MonoBehaviour
{
    public AudioClip talkingClip;
    public AudioClip hurtClip;
    private NavMeshAgent agent;
    private Animator animator;
    private GameObject player;
    [Header("位置不變設定")]
    public bool stayPut = false;   // 是否不被刪除

    [Header("物品回應設定")]
    public ItemResponseData[] itemResponses;
    [Header("距離設定")]
    private float stopDistance = 1f;
    private float chaseDistance = 2f;
    [Header("夜晚離場設定")]
    [HideInInspector] public float exitDestroyDistance = 5f;

    private bool isLeaving = false;

    [Header("出口目的地")]

    [HideInInspector] public Transform exitPoint;

    [Range(0f, 1f)]
    public float stayChance = 0.3f;
    public float rotateSpeed = 5f;

    private Vector3 currentTarget;
    private Transform xinyuanTransform;
    private bool xinyuanArrived = false;
    [Header("Speech Bubble")]
    public GameObject speechBubblePrefab;
    private GameObject speechBubbleInstance;
    [Header("對話內容")]
    public NPCResponseData[] dialogues;

    private bool isTalking = false;
    [Header("自言自語")]
    public string[] assignedMessages;
    [Header("是否會自己行動設定")]
    public bool moveable = false;
    public bool xinyuanMember = false;
    public bool xinyuanLeader = false;
    public string[] candidateSentences = new string[]
    {
        "早安！", "需要幫忙嗎？","我來了！"
    };
    [Header("巡邏設定")]
    public Transform[] patrolPoints;
    public float decisionInterval = 10f;
    private bool hasShownBubble = false;

    // ----------------------
    // 被打狀態控制
    // ----------------------
    private bool isBeaten = false;     // 正在被打倒
    public bool canBeBeaten = true;   // 是否可以被打

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(TalkingRoutine());
    }
    // ⭐ 給 GameManager 呼叫
    public void InitPatrol(Transform[] points)
    {
        patrolPoints = points;

        if (!moveable) return;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("InitPatrol：巡邏點為空！ obj=" + gameObject.name);
            return;
        }

        currentTarget = patrolPoints[Random.Range(0, patrolPoints.Length)].position;
        if (agent != null)
            agent.SetDestination(currentTarget);

        StartCoroutine(ChooseDestinationRoutine());
    }

    public void InitXinyuan(Transform point)
    {
        xinyuanTransform = point;
        currentTarget = point.position;
        if (agent != null)
            agent.SetDestination(currentTarget);
    }

    private void Update()
    {
        if (!isLeaving && GameManager.Instance != null && GameManager.Instance.monsterExist && exitPoint != null)    //如果怪物存在，NPC就逃往出口
        {
            animator.SetTrigger("escape");
            ShowBubble("救命啊！有怪物！");
            agent.speed = agent.speed * 1.2f; // 加速逃跑
            BeginExit();
            return;
        }
        // ===== 夜晚 → 前往出口並離場 =====
        if (!isLeaving && IsNight() && exitPoint != null)
        {
            ShowBubble("該回家了！");
            BeginExit();
            return;
        }

        if (isLeaving)
        {
            HandleExitMovement();
            return;
        }
        if (player == null) return;
        if (isBeaten) return; // 被打倒期間停止巡邏/追玩家
        bool isMoving = false;
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (!moveable && stayPut)   //不會移動但不會被刪除的NPC，保持面向玩家，目前只有廚師
        {
            if (distanceToPlayer <= stopDistance)
                StopAgentAndLookAtPlayer();
        }
        if (moveable && PlayerPrefs.GetInt("xinyuanReady", 0) <= 1) //非心圓會時的日常行動
        {
            Vector3 targetPosition;

            // 玩家太近 → 停下
            if (distanceToPlayer <= stopDistance)
            {
                StopAgentAndLookAtPlayer();
            }
            // 追玩家
            else if (distanceToPlayer <= chaseDistance)
            {
                targetPosition = player.transform.position;
                if (agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPosition);

                    if (!agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
                        isMoving = true;
                }
                if (!hasShownBubble && speechBubblePrefab != null && candidateSentences.Length > 0)
                {
                    ShowSpeechBubbleRandom();
                    hasShownBubble = true;
                }
            }
            // 巡邏
            else
            {
                targetPosition = currentTarget;
                if (agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPosition);

                    if (!agent.pathPending && agent.remainingDistance > agent.stoppingDistance)
                        isMoving = true;
                }
                hasShownBubble = false;
            }

            animator.SetBool("isWalking", isMoving);
        }
        else if (moveable && PlayerPrefs.GetInt("xinyuanReady", 0) == 2 && xinyuanArrived == false && (xinyuanMember || xinyuanLeader))      //心圓會集合中的可移動npc的行動
        {
            // 玩家太近 → 停下
            if (distanceToPlayer <= stopDistance)
            {
                StopAgentAndLookAtPlayer();
                isMoving = false;
            }
            else
            {
                isMoving = true;
                if (agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(xinyuanTransform.position);
                }
            }
            // 計算和目標點的實際距離
            float distanceToTarget = Vector3.Distance(transform.position, xinyuanTransform.position);

            // 真正接近目標點才算到達
            if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && distanceToTarget < 0.2f && !xinyuanArrived)
            {
                agent.isStopped = true;
                // 取得目標點的 -Z 世界方向
                Vector3 lookDir = xinyuanTransform.TransformDirection(Vector3.back);
                lookDir.y = 0f; // 保持水平

                transform.rotation = Quaternion.LookRotation(lookDir);

                isMoving = false;
                xinyuanArrived = true;

                PlayerPrefs.SetInt("xinyuanGroup",
                    PlayerPrefs.GetInt("xinyuanGroup", 0) + 1);
            }

            animator.SetBool("isWalking", isMoving);
        }
        if (xinyuanArrived && PlayerPrefs.GetInt("xinyuanReady", 0) == 2 && PlayerPrefs.GetInt("xinyuanGroup", 0) == 6 && xinyuanLeader)
        {
            PlayerPrefs.SetInt("xinyuanReady", 3);
            ShowBubble("人終於到齊啦！");
            StartCoroutine(ShowBubbleAfterDelay(2f, "出發前先唱個心圓歌吧"));
            StartCoroutine(ShowBubbleAfterDelay(4f, "記得帶動作哦"));
            Invoke(nameof(ReadyToDance), 6f);
            StartCoroutine(ShowBubbleAfterDelay(16f, "跳得很好，以後不許再跳了"));
            StartCoroutine(ShowBubbleAfterDelay(18f, "出發訪視關懷戶吧"));
            Invoke(nameof(FinishDance), 20f);
        }
        if (xinyuanMember && PlayerPrefs.GetInt("xinyuanReady", 0) == 4 && animator.GetInteger("dance") == 0)  //心圓會成員開始跳舞
        {
            animator.SetInteger("dance", Random.Range(1, 4));
            ShowBubble("來到心圓真歡喜");
            StartCoroutine(ShowBubbleAfterDelay(2f, "感恩尊重來學習"));
            StartCoroutine(ShowBubbleAfterDelay(4f, "做人謙虛縮小自己"));
            StartCoroutine(ShowBubbleAfterDelay(6f, "實在福報"));
            StartCoroutine(ShowBubbleAfterDelay(8f, "沒得比"));
        }
        if ((xinyuanMember || xinyuanLeader) && PlayerPrefs.GetInt("xinyuanReady", 0) == 5)    //心圓會成員跳完舞準備出發
        {
            animator.SetInteger("dance", 0);
            if (distanceToPlayer <= stopDistance)
            {
                StopAgentAndLookAtPlayer();
                isMoving = false;
            }
            else
            {
                isMoving = true;
                if (agent != null)
                {
                    agent.isStopped = false;
                    agent.SetDestination(exitPoint.position);
                }
                animator.SetBool("isWalking", true);
            }
            animator.SetBool("isWalking", isMoving);

            //到達出口後清除人物
            if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                float distanceToExit = Vector3.Distance(transform.position, exitPoint.position);
                if (distanceToExit <= exitDestroyDistance)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
void OnAnimatorMove()
{
    if (agent == null) return;

    // 🔥 強制用 NavMesh 的位置
    transform.position = agent.nextPosition;
}
    void ReadyToDance() //心圓會組長命令跳舞
    {
        PlayerPrefs.SetInt("xinyuanReady", 4);   //4是準備好開始跳舞
    }
    void FinishDance()  //心圓會組長命令結束跳舞
    {
        PlayerPrefs.SetInt("xinyuanReady", 5);   //5是跳完舞了
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPoint.position);
        }
        animator.SetBool("isWalking", true);
        //如果玩家正在跳舞，使其停止跳舞回到正常狀態
        PlayerCtrl pc = player.GetComponent<PlayerCtrl>();
        if (pc != null && pc.animator.GetBool("isDancing"))
        {
            pc.OnDanceEnd();
        }
    }
    public void OnDanceEnd()
    {
    }
    private string GetRandomMessage()
    {
        if (assignedMessages == null || assignedMessages.Length == 0)
            return null;

        return assignedMessages[Random.Range(0, assignedMessages.Length)];
    }
    private IEnumerator TalkingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.value * 3f + 3f);

            if (speechBubbleInstance != null || isBeaten)
                continue;

            if (Random.value > 1f / 3f)
                continue;

            string msg = GetRandomMessage();
            if (string.IsNullOrEmpty(msg))
                continue;

            else
            {
                ShowBubble(msg);
            }

        }
    }
    private void StopAgentAndLookAtPlayer()
    {
        if (agent != null && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        Vector3 lookDir = (player.transform.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotateSpeed * Time.deltaTime);
            transform.rotation = lookRotation;
        }
    }

    private IEnumerator ChooseDestinationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(decisionInterval);

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer > chaseDistance)
            {
                if (Random.value > stayChance)
                {
                    Transform nextTarget;
                    do
                    {
                        nextTarget = patrolPoints[Random.Range(0, patrolPoints.Length)];
                    } while (nextTarget.position == currentTarget);

                    currentTarget = nextTarget.position;
                }
            }
        }
    }
    public void setXinyuanReady()
    {
        PlayerPrefs.SetInt("xinyuanReady", 1);

        Debug.Log("已設定準備好心圓會了！ pref= " + PlayerPrefs.GetInt("xinyuanReady", 0));

    }
    public void setDancedFalse()
    {
        PlayerPrefs.SetInt("rayDanced", 0);
    }
    public void setBurnedGoldPaperFalse()
    {
        Debug.Log("Reset burnedGoldPaper flag.");
        PlayerPrefs.SetInt("burnedGoldPaper", 0);
    }
    public void setLunchSearved()
    {
        PlayerPrefs.SetInt("lunchServed", 1);
    }
    bool CheckCondition(NPCResponseData data)
    {
        if (string.IsNullOrEmpty(data.requiredFlag))
            return false; // 沒設定 flag 名稱就不符合
        return PlayerPrefs.GetInt(data.requiredFlag, 0) == 1;
    }
    public void response()
    {
        if (player == null) return;
        if (isTalking) return;
        isTalking = true;
        int currentIndex = 0;
        foreach (var i in dialogues)
        {
            if (CheckCondition(i))
            {
                Debug.Log("符合條件：" + i.requiredFlag);
                currentIndex = System.Array.IndexOf(dialogues, i);
                break;
            }
        }

        var data = dialogues[currentIndex];
        player.GetComponent<PlayerCtrl>()?.ShowBubble(data.playerText);
        StartCoroutine(NPCresponse(data));
    }
    public IEnumerator NPCresponse(NPCResponseData data)
    {
        yield return new WaitForSeconds(1f);

        ShowBubble(data.responseText);
        PlayTalkingSound();
        // 給玩家物品
        if (data.rewardItem != null)
        {
            GivePlayerItem(data.rewardItem);
        }
        data.onConditionSuccess?.Invoke();
        isTalking = false;
    }


    public void receiveItem(GameObject itemObj)
    {
        Debug.Log("NPC received an item.");
        ItemHolder itemHolder = itemObj.GetComponent<ItemHolder>();
        if (itemHolder == null || itemHolder.item == null)
            return;

        Item givenItem = itemHolder.item;

        foreach (var response in itemResponses)
        {
            if (response.requiredItem == givenItem)
            {
                // NPC 說話
                ShowBubble(response.responseText);
                PlayTalkingSound();

                // 給玩家獎勵物品
                if (response.rewardItem != null)
                {
                    GivePlayerItem(response.rewardItem);
                }
                response.onConditionSuccess?.Invoke();
                Debug.Log($"NPC accepted {givenItem.itemName}");
                return;
            }
        }
        //如果是身上有barrelBurn腳本的NPC，會觸發冒煙效果
        barrelBurn burnScript = GetComponent<barrelBurn>();
        if (burnScript != null)
        {
            burnScript.Smoke();
        }
        Debug.Log($"我不需要 {givenItem.itemName}");
        // 沒有對應反應
        ShowBubble($"我不需要 {givenItem.itemName}");
    }
    private void GivePlayerItem(Item reward)
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddItem(reward);
        }
    }
    IEnumerator ShowBubbleAfterDelay(float delay, string message)
    {
        yield return new WaitForSeconds(delay);
        ShowBubble(message);
    }
    public void ShowBubble(string msg)
    {
        if (speechBubblePrefab == null)
        {
            return;
        }
        // 先銷毀舊氣泡（但不影響正在運行的協程）
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }

        // 生成新氣泡
        speechBubbleInstance = Instantiate(speechBubblePrefab, transform);
        speechBubbleInstance.transform.localPosition = new Vector3(0f, 1.25f, 0);

        var bubble = speechBubbleInstance.GetComponent<SpeechBubble>();
        if (bubble != null)
            bubble.SetText(msg);

        // 啟動協程，延時後檢查是否還是最新氣泡
        StartCoroutine(HideBubbleAfterDelay(speechBubbleInstance, 3f));
    }

    private IEnumerator HideBubbleAfterDelay(GameObject bubble, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 只銷毀當前最新氣泡，避免覆蓋被提前刪除
        if (speechBubbleInstance == bubble)
        {
            Destroy(bubble);
            speechBubbleInstance = null;
        }
    }
    // ----------------------
    // TMP 氣泡
    // ----------------------
    private void ShowSpeechBubbleRandom()
    {
        int idx = Random.Range(0, candidateSentences.Length);

        ShowBubble(candidateSentences[idx]);
    }

    // ----------------------
    // 被打相關
    // ----------------------
    public void BeBeaten()
    {
        if (!canBeBeaten) return;

        canBeBeaten = false;
        isBeaten = true;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        GameManager.Instance.PlayDamagedSound();
        //0.5秒後播放受傷音效
        Invoke(nameof(PlayHurtSound), 0.5f);
        ShowBubble("好痛！");
        animator.SetTrigger("beaten");
    }
    private void PlayHurtSound()
    {
        if (hurtClip != null)
        {
            AudioSource.PlayClipAtPoint(hurtClip, transform.position);
        }
    }
    // 動畫事件：站起來中間呼叫
    public void OnBeatenBubble()
    {
        ShowBubble("別鬧了！");
    }

    // 動畫事件：起身完成呼叫
    public void OnBeatenRecover()
    {
        Debug.Log("NPC 從被打狀態恢復，可以繼續行動了");
        isBeaten = false;
        canBeBeaten = true;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget);
        }
    }
    bool IsNight()
    {
        return GameManager.Instance != null && GameManager.Instance.IsEvening();
    }
    void BeginExit()
    {
        isLeaving = true;

        StopAllCoroutines(); // 停止巡邏 / 自言自語
        hasShownBubble = false;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
            currentTarget = exitPoint.position;
            agent.SetDestination(exitPoint.position);
        }
        animator.SetBool("isWalking", true);
    }
    void HandleExitMovement()
    {
        if (agent != null && agent.pathPending) {
            return; // 等待路徑計算完成
        }

        float dist = Vector3.Distance(transform.position, exitPoint.position);

        if (dist <= exitDestroyDistance)
        {   
            Destroy(gameObject);
        }
    }
    void PlayTalkingSound()
    {
        if (talkingClip != null)
        {
            AudioSource.PlayClipAtPoint(talkingClip, transform.position);
        }
    }
}
