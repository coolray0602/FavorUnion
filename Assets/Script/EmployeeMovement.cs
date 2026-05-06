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

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.speed = normalSpeed;
    }

    private void Start()
    {
        //animator.enabled = false;
        audioSource = GetComponent<AudioSource>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        StartCoroutine(TalkingRoutine());
        StartCoroutine(LitterCheckRoutine());
    }

    public void SetDestination(Vector3 target)
    {
        targetPosition = target;
        agent.SetDestination(target);
        StartCoroutine(TextingRoutine());
    }

    private void Update()
    {
        float speed = agent.velocity.magnitude;

        animator.SetBool("isWalking", speed > 0.1f);

        if (Vector3.Distance(transform.position, targetPosition) <= arriveDistance)
            Destroy(gameObject);

        if (isBeaten) return;

        if (!isEscaping && GameManager.Instance != null && GameManager.Instance.monsterExist && exitPoint != null)
        {
            StopAllCoroutines(); // 停止其他行為
            isEscaping = true;
            ShowBubble("救命啊！有怪物！");
            BeginExit();
            return;
        }

        if (isEscaping)
        {
            return;
        }

        if (isFrozenForGive)
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

            return; // 直接跳過下面邏輯
        }

        if (agent == null) return;

        CheckBowRange();
        //Debug.Log("離目標距離: " + Vector3.Distance(transform.position, targetPosition));
    }
    void OnAnimatorMove()
    {
        if (agent == null) return;

        // 🔥 強制用 NavMesh 的位置
        transform.position = agent.nextPosition;
    }
    public void BeginExit()
    {
        if (exitPoint == null || agent == null) return;

        animator.SetTrigger("escape");
        agent.speed = normalSpeed * 1.2f; // 加速逃跑
        isFrozenForGive = false; // 確保不再被凍結
        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(exitPoint.position);
        targetPosition = exitPoint.position;
    }

    // ========== 讓 NPC 暫停並等待玩家給予 ==========
    public void FreezeForGive(Transform player)
    {
        if (isFrozenForGive) return;

        isFrozenForGive = true;

        // 記住原本要去哪裡
        savedDestination = agent.destination;

        // 停止 NavMesh
        agent.isStopped = true;
        agent.ResetPath();
        // 停止動畫
        animator.SetBool("isWalking", false);

        // NPC 面向玩家（可加可不加）
        Vector3 lookPos = player.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);
    }

    // ========== 玩家不再給予，NPC 恢復 ==========
    public void UnfreezeFromGive()
    {
        if (!isFrozenForGive) return;

        isFrozenForGive = false;

        agent.SetDestination(savedDestination);
        agent.isStopped = false;
        animator.SetBool("isWalking", true);
    }

    // ======================================================
    // 被打與鞠躬氣泡
    // ======================================================
    public void BeBeaten()
    {
        if (isBeaten) return;
        isBeaten = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

        }
        GameManager.Instance.PlayDamagedSound();

        animator.SetTrigger("beaten");
        animator.SetBool("isDead", true);
        ShowBubble("哎呀！");
        //0.5秒後播放受傷音效
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
    private void CheckBowRange()
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
    public void PlayTalkingSound(float length = 1f)
    {
        if (audioSource == null || talkingClip == null) return;

        // 確保不超出音檔長度
        float maxStartTime = talkingClip.length - length;
        if (maxStartTime < 0) maxStartTime = 0;

        // 隨機起點
        float randomStart = Random.Range(0f, maxStartTime);

        audioSource.clip = talkingClip;
        audioSource.time = randomStart;
        audioSource.Play();

        StopAllCoroutines();
        StartCoroutine(StopAfterTime(length));
    }
    IEnumerator StopAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    public void response()
    {
        if (player == null) return;
        player.GetComponent<PlayerCtrl>()?.ShowBubble("工作辛苦了！");
        StartCoroutine(NPCresponse());
    }
    public IEnumerator NPCresponse()
    {
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
    }
    public void ShowBubble(string msg)
    {
        // 先銷毀舊氣泡（但不影響正在運行的協程）
        if (speechBubbleInstance != null)
        {
            Destroy(speechBubbleInstance);
            speechBubbleInstance = null;
        }

        // 生成新氣泡
        speechBubbleInstance = Instantiate(speechBubblePrefab, transform);
        speechBubbleInstance.transform.localPosition = new Vector3(0f, 1.2f, 0);

        var bubble = speechBubbleInstance.GetComponent<SpeechBubble>();
        if (bubble != null)
            bubble.SetText(msg);

        // 啟動協程，延時後檢查是否還是最新氣泡
        StartCoroutine(HideBubbleAfterDelay(speechBubbleInstance, 2f));
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

    // ======================================================
    // 自言自語
    // ======================================================
    private IEnumerator TalkingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            if (speechBubbleInstance != null || isBeaten)
                continue;

            if (Random.value > 1f / 3f)
                continue;

            string msg = GetRandomMessage();
            if (string.IsNullOrEmpty(msg))
                continue;

            if (isFrozenForGive)
            {
                string[] options = { "?", "怎麼了？", "有事嗎" };
                int index = Random.Range(0, options.Length); // 隨機取得 0,1,2
                ShowBubble(options[index]);
                //bubble.SetText(options[index]);
            }
            else
            {
                ShowBubble(msg);
            }

        }
    }
    private string GetRandomMessage()
    {
        if (assignedMessages == null || assignedMessages.Length == 0)
            return null;

        return assignedMessages[Random.Range(0, assignedMessages.Length)];
    }

    // ======================================================
    // Texting 動作
    // ======================================================
    private IEnumerator TextingRoutine()
    {
        while (true)
        {

            yield return new WaitForSeconds(3f);
            if (agent != null && agent.isOnNavMesh)
            {
                if (agent.isStopped) continue;
            }

            if (!isFrozenForGive)
            {
                isTexting = (Random.value > 0.7f);
                animator.SetBool("texting", isTexting);

                if (phoneObject != null)
                    phoneObject.SetActive(isTexting);

                agent.speed = isTexting ? textingSpeed : normalSpeed;
            }
        }
    }

    // ======================================================
    // 丟垃圾協程：決定何時丟垃圾
    // ======================================================
    private IEnumerator LitterCheckRoutine()
    {
        while (true)
        {

            yield return new WaitForSeconds(10f);
            if (agent != null && agent.isOnNavMesh)
            {
                if (agent.isStopped || isBeaten || isTexting)
                    continue;
            }

            if (!isFrozenForGive)
            {
                if (Random.value <= 0.1f)
                {
                    animator.SetTrigger("littering");
                    agent.isStopped = true;  // 停止走路
                }

            }
        }
    }

    // ======================================================
    // Animation Event：真正生成垃圾
    // ======================================================
    public void LitterThrow()
    {
        if (litterPrefabs.Length == 0 || rightHandTransform == null) return;

        // 生成位置稍微抬高
        Vector3 spawnPos = rightHandTransform.position + Vector3.up * 0.2f;

        // 隨機旋轉
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

        // 丟出方向（前右上）
        Vector3 throwDir = transform.right + Vector3.up * litterUpForce;
        rb.AddForce(throwDir.normalized * litterForce, ForceMode.Impulse);

        LitterAutoDestroy auto = litter.AddComponent<LitterAutoDestroy>();
        auto.lifeTime = 10f;
        //Destroy(litter, 10f);
    }
}
