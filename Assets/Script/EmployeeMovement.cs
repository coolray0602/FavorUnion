using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EmployeeMovement : MonoBehaviour
{
    public AudioClip talkingClip;
    public AudioClip hurtClip;
    public AudioSource audioSource;
    [Header("泡泡")]
    public GameObject speechBubblePrefab;
    public GameObject speechBubbleInstance;

    [Header("移動設定")]
    public float arriveDistance = 3.0f;
    public float bowDistance = 2.0f;
    public float normalSpeed = 1.0f;
    public float textingSpeed = 0.7f;

    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 targetPosition;
    private Transform player;
    private bool isBeaten = false;
    private bool hasBowed = false;
    private bool isFrozenForGive = false;
    private Vector3 savedDestination;
    private bool isTexting = false;

    [Header("自言自語")]
    public string[] assignedMessages;

    public Vector3 TargetPosition => targetPosition;

    [Header("手機物件")]
    public GameObject phoneObject;

    [Header("丟垃圾設定")]
    public GameObject[] litterPrefabs;
    public Transform rightHandTransform;
    public float litterForce = 3f;
    public float litterUpForce = 1f;

    public Transform exitPoint; // NPC 逃跑的出口位置

    public bool isEscaping = false;

    // ==================== 效能優化：定時器變數 ====================
    private float nextTalkTime = 0f;
    private float nextTextingTime = 0f;
    private float nextLitterTime = 0f;
    private float nextBowCheckTime = 0f;
    
    // 隨機間隔範圍
    private float talkMinInterval = 8f;
    private float talkMaxInterval = 15f;
    private float textingMinInterval = 2f;
    private float textingMaxInterval = 4f;
    private float litterMinInterval = 15f;
    private float litterMaxInterval = 25f;
    private float bowCheckInterval = 0.5f;
    
    // 機率控制
    private float talkChance = 0.3f;      // 30% 機率說話
    private float textingChance = 0.15f;  // 15% 機率看手機
    private float litterChance = 0.08f;   // 8% 機率丟垃圾

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = normalSpeed;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
        
        // 初始化定時器
        InitializeTimers();
        
        // 啟動唯一的協程（用於需要等待的動作）
        StartCoroutine(LitterThrowRoutine());
    }
    
    private void InitializeTimers()
    {
        nextTalkTime = Time.time + Random.Range(talkMinInterval, talkMaxInterval);
        nextTextingTime = Time.time + Random.Range(textingMinInterval, textingMaxInterval);
        nextLitterTime = Time.time + Random.Range(litterMinInterval, litterMaxInterval);
        nextBowCheckTime = Time.time + bowCheckInterval;
    }

    public void SetDestination(Vector3 target)
    {
        targetPosition = target;
        agent.SetDestination(target);
    }

    private void Update()
    {
        // ⭐ 打倒狀態優先處理 - 完全停止所有行為
        if (isBeaten)
        {
            // 確保完全停止移動
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
            
            // 確保動畫停留在倒地狀態
            animator.SetBool("isWalking", false);
            animator.SetBool("texting", false);
            
            // 不執行任何其他 Update 邏輯
            return;
        }
        
        // 基本移動檢查（只在非打倒狀態執行）
        if (!isEscaping && !isFrozenForGive)
        {
            float speed = agent.velocity.magnitude;
            animator.SetBool("isWalking", speed > 0.1f);
            
            // 到達目的地銷毀
            if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        // 逃脫行為（優先級最高）
        if (isEscaping)
        {
            UpdateEscape();
            return;
        }
        
        // 凍結狀態處理
        if (isFrozenForGive)
        {
            UpdateFrozenState();
            return;
        }
        
        // 怪物存在時的逃脫檢查（降低頻率）
        if (!isEscaping && GameManager.Instance != null && GameManager.Instance.monsterExist && exitPoint != null)
        {
            BeginExit();
            return;
        }
        
        // 使用統一的定時器更新所有行為
        UpdateTimedBehaviors();
    }
    
    // 統一定時器更新
    private void UpdateTimedBehaviors()
    {
        float currentTime = Time.time;
        
        // 自言自語
        if (currentTime >= nextTalkTime && !isResponding && speechBubbleInstance == null)
        {
            nextTalkTime = currentTime + Random.Range(talkMinInterval, talkMaxInterval);
            
            if (Random.value <= talkChance)
            {
                string msg = GetRandomMessage();
                if (!string.IsNullOrEmpty(msg))
                {
                    ShowBubble(msg);
                    PlayTalkingSound(0.5f);
                }
            }
        }
        
        // 看手機行為
        if (currentTime >= nextTextingTime && !isFrozenForGive)
        {
            nextTextingTime = currentTime + Random.Range(textingMinInterval, textingMaxInterval);
            
            bool newTextingState = (Random.value <= textingChance);
            if (newTextingState != isTexting)
            {
                isTexting = newTextingState;
                animator.SetBool("texting", isTexting);
                if (phoneObject != null)
                    phoneObject.SetActive(isTexting);
                agent.speed = isTexting ? textingSpeed : normalSpeed;
            }
        }
        
        // 鞠躬檢查（降低頻率）
        if (currentTime >= nextBowCheckTime && player != null && !hasBowed)
        {
            nextBowCheckTime = currentTime + bowCheckInterval;
            CheckBowRangeOptimized();
        }
    }
    
    // 優化版的鞠躬檢查
    private void CheckBowRangeOptimized()
    {
        if (player == null || hasBowed) return;
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= bowDistance)
        {
            agent.isStopped = true;
            
            Vector3 dir = player.position - transform.position;
            dir.y = 0;
            transform.rotation = Quaternion.LookRotation(dir);
            
            animator.SetTrigger("bow");
            hasBowed = true;
            
            if (player != null)
            {
                PlayerCtrl pc = player.GetComponent<PlayerCtrl>();
                if (pc != null)
                    pc.ShowPlayerReplyBubble();
            }
            
            ShowBubble("老闆好！");
            PlayTalkingSound(0.5f);
        }
    }
    
    private void UpdateEscape()
    {
        if (agent == null || exitPoint == null) return;
        
        // 到達出口就銷毀
        if (Vector3.Distance(transform.position, exitPoint.position) <= arriveDistance)
        {
            Destroy(gameObject);
        }
    }
    
    private void UpdateFrozenState()
    {
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        
        if (TryGetComponent<Rigidbody>(out var rb))
            rb.velocity = Vector3.zero;
        
        // 永遠面向玩家
        if (player != null)
        {
            Vector3 lookPos = player.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
            
            // 玩家距離超過 2 米 → 自動解除凍結
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > 2f)
            {
                UnfreezeFromGive();
            }
        }
    }
    
    void OnAnimatorMove()
    {
        // ⭐ 打倒狀態不更新位置
        if (isBeaten || isEscaping)
            return;
            
        if (agent == null) return;
        transform.position = agent.nextPosition;
    }
    
    public void BeginExit()
    {
        if (exitPoint == null || agent == null || isEscaping) return;
        
        // 停止所有行為
        StopAllCoroutines();
        isEscaping = true;
        isFrozenForGive = false;
        isBeaten = false;
        
        ShowBubble("救命啊！有怪物！");
        
        // 重置狀態
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }
        
        animator.SetTrigger("escape");
        agent.speed = normalSpeed * 1.2f;
        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(exitPoint.position);
        targetPosition = exitPoint.position;
    }

    public void FreezeForGive(Transform playerTransform)
    {
        if (isFrozenForGive || isEscaping) return;
        
        isFrozenForGive = true;
        savedDestination = agent.destination;
        
        agent.isStopped = true;
        agent.ResetPath();
        animator.SetBool("isWalking", false);
        
        Vector3 lookPos = playerTransform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    public void UnfreezeFromGive()
    {
        if (!isFrozenForGive) return;
        
        isFrozenForGive = false;
        
        if (!isEscaping)
        {
            agent.SetDestination(savedDestination);
            agent.isStopped = false;
            animator.SetBool("isWalking", true);
        }
    }

    public void BeBeaten()
    {
        if (isBeaten) return;
        isBeaten = true;
        
        // ⭐ 完全停止 NavMeshAgent
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.speed = 0f; // 額外設置速度為0
        }
        
        // ⭐ 停止所有協程
        StopAllCoroutines();
        
        // ⭐ 停止音效
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
        
        // ⭐ 隱藏氣泡
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }
        
        // ⭐ 停止手機動畫
        if (phoneObject != null)
            phoneObject.SetActive(false);
        
        // ⭐ 確保動畫狀態正確
        animator.SetBool("isWalking", false);
        animator.SetBool("texting", false);
        animator.SetTrigger("beaten");
        animator.SetBool("isDead", true);
        
        GameManager.Instance?.PlayDamagedSound();
        ShowBubble("哎呀！");
        
        Invoke(nameof(PlayHurtSound), 0.5f);
        FindObjectOfType<GuardAI>()?.StartChasing();
    }
    
    public void PlayHurtSound()
    {
        if (hurtClip != null)
        {
            AudioSource.PlayClipAtPoint(hurtClip, transform.position);
        }
    }
    
    public void PlayTalkingSound(float length = 1f)
    {
        if (audioSource == null || talkingClip == null) return;
        
        float maxStartTime = Mathf.Max(0, talkingClip.length - length);
        float randomStart = Random.Range(0f, maxStartTime);
        
        audioSource.clip = talkingClip;
        audioSource.time = randomStart;
        audioSource.Play();
        
        StartCoroutine(StopAfterTime(length));
    }
    
    IEnumerator StopAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        if (audioSource.isPlaying)
            audioSource.Stop();
    }
    
    private bool isResponding = false;
    private Coroutine currentBubbleCoroutine;
    private float lastBubbleTime = 0f;
    private const float BUBBLE_COOLDOWN = 0.5f;

    public void response()
    {
        if (player == null || isEscaping) return;
        player.GetComponent<PlayerCtrl>()?.ShowBubble("工作辛苦了！");
        StartCoroutine(NPCresponse());
    }

    public IEnumerator NPCresponse()
    {
        isResponding = true;
        
        if (currentBubbleCoroutine != null)
        {
            StopCoroutine(currentBubbleCoroutine);
            currentBubbleCoroutine = null;
        }
        
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }
        
        yield return new WaitForSeconds(1f);
        
        if (isBeaten)
        {
            ShowBubble("我不行了…");
        }
        else
        {
            ShowBubble("謝謝關心！");
        }
        PlayTalkingSound(0.8f);
        
        yield return new WaitForSeconds(2.5f);
        isResponding = false;
        
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }
        
        if (currentBubbleCoroutine != null)
        {
            StopCoroutine(currentBubbleCoroutine);
            currentBubbleCoroutine = null;
        }
    }

    public void ShowBubble(string msg)
    {
        if (Time.time - lastBubbleTime < BUBBLE_COOLDOWN || isEscaping)
            return;
        
        lastBubbleTime = Time.time;
        
        if (currentBubbleCoroutine != null)
        {
            StopCoroutine(currentBubbleCoroutine);
            currentBubbleCoroutine = null;
        }
        
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }
        
        speechBubbleInstance = Instantiate(speechBubblePrefab, transform);
        speechBubbleInstance.transform.localPosition = new Vector3(0f, 1.2f, 0);
        
        var bubble = speechBubbleInstance.GetComponent<SpeechBubble>();
        bubble?.SetText(msg);
        
        currentBubbleCoroutine = StartCoroutine(HideBubbleAfterDelay(speechBubbleInstance, 2f));
    }

    private IEnumerator HideBubbleAfterDelay(GameObject bubble, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (speechBubbleInstance == bubble)
        {
            Destroy(bubble);
            speechBubbleInstance = null;
        }
        
        currentBubbleCoroutine = null;
    }

    private string GetRandomMessage()
    {
        if (assignedMessages == null || assignedMessages.Length == 0)
            return null;
        
        // 凍結狀態有特殊台詞
        if (isFrozenForGive)
        {
            string[] options = { "?", "怎麼了？", "有事嗎" };
            return options[Random.Range(0, options.Length)];
        }
        
        return assignedMessages[Random.Range(0, assignedMessages.Length)];
    }
    
    // 單獨處理丟垃圾的協程（因為需要 Animation Event）
    private IEnumerator LitterThrowRoutine()
    {
        while (!isEscaping && !isBeaten)
        {
            float waitTime = Random.Range(litterMinInterval, litterMaxInterval);
            yield return new WaitForSeconds(waitTime);
            
            if (isEscaping || isBeaten || isTexting || isFrozenForGive)
                continue;
            
            if (Random.value <= litterChance && agent != null && agent.isOnNavMesh && !agent.isStopped)
            {
                animator.SetTrigger("littering");
                agent.isStopped = true;
            }
        }
    }

    public void LitterThrow()
    {
        if (litterPrefabs.Length == 0 || rightHandTransform == null) return;
        
        Vector3 spawnPos = rightHandTransform.position + Vector3.up * 0.2f;
        Quaternion spawnRot = Random.rotation;
        
        GameObject litter = Instantiate(
            litterPrefabs[Random.Range(0, litterPrefabs.Length)],
            spawnPos,
            spawnRot
        );
        
        Rigidbody rb = litter.GetComponent<Rigidbody>();
        if (rb == null)
            rb = litter.AddComponent<Rigidbody>();
        
        rb.mass = 0.2f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        Vector3 throwDir = transform.right + Vector3.up * litterUpForce;
        rb.AddForce(throwDir.normalized * litterForce, ForceMode.Impulse);
        
        LitterAutoDestroy auto = litter.AddComponent<LitterAutoDestroy>();
        auto.lifeTime = 10f;
        
        // 恢復移動
        if (agent != null && !isEscaping && !isBeaten)
            agent.isStopped = false;
    }
    
    // 重置狀態（用於對象池）
    public void ResetState()
    {
        isBeaten = false;
        hasBowed = false;
        isFrozenForGive = false;
        isTexting = false;
        isEscaping = false;
        isResponding = false;
        
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }
        
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = normalSpeed;
            agent.velocity = Vector3.zero;
        }
        
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("texting", false);
            animator.SetBool("isDead", false);
            animator.Rebind(); // 重置動畫狀態
        }
        
        if (phoneObject != null)
            phoneObject.SetActive(false);
        
        InitializeTimers();
    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
        
        if (speechBubbleInstance != null)
            Destroy(speechBubbleInstance);
    }
}