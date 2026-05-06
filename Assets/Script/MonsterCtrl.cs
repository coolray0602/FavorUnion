using UnityEngine;
using UnityEngine.AI;

public class MonsterCtrl : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private GameObject player;
    private PlayerInventory playerInventory;
    private MonsterAudio monsterAudio;
    private bool isAttacking = false;
    private bool isGettingHit = false;
    private bool isDead = false;
    public int health = 3;

    public enum MonsterState { Idle, Patrol, Scream, Chase, Attack }
    [SerializeField] private MonsterState state = MonsterState.Idle;

    [Header("巡邏")]
    public float moveInterval = 10f;
    public float moveRange = 10f;

    [Header("視野")]
    public float viewDistance = 15f;
    public float dayViewAngle = 300f;
    public float nightViewAngle = 260f;

    [Header("戰鬥")]
    public float attackDistance = 2f;
    public float loseDistance = 20f;

    [Header("速度")]
    public float patrolSpeed = 8f;
    public float chaseSpeed = 12f;

    private float moveTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        monsterAudio = GetComponent<MonsterAudio>();

        player = GameObject.FindGameObjectWithTag("Player");
        playerInventory = player.GetComponent<PlayerInventory>();

    }

    void Start()
    {
        GameManager.Instance.monsterExist = true; // ⭐ 告訴 GameManager 怪物存在了
    }

    void Update()
    {
        if (isDead) return;

        if (state != MonsterState.Attack && state != MonsterState.Scream)
            CheckPlayer();


        // 原本狀態機照跑
        switch (state)
        {
            case MonsterState.Patrol: Patrol(); break;
            case MonsterState.Scream: FacePlayer(); break;
            case MonsterState.Chase: Chase(); break;
            case MonsterState.Attack: Attack(); break;
        }
        UpdateAnimation();
    }

    // ⭐ 防止重複進入同一狀態
    void SetState(MonsterState newState)
    {
        //Debug.Log("嘗試切換到 " + newState + " 狀態");
        if (state == newState)
        {
            //Debug.Log("已經在 " + newState + " 狀態了，跳過");
            return;
        }

        state = newState;

        animator.SetBool("walkleft", false);
        animator.SetBool("walkright", false);

        switch (state)
        {
            case MonsterState.Idle:
                Debug.Log("切換到閒置狀態");
                agent.ResetPath();
                agent.isStopped = true;
                monsterAudio?.StopMove();
                break;
            case MonsterState.Patrol:
                Debug.Log("切換到巡邏狀態");
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                break;

            case MonsterState.Chase:
                Debug.Log("切換到追逐狀態");
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                break;

            case MonsterState.Scream:
                Debug.Log("切換到尖叫狀態");
                agent.ResetPath();
                agent.isStopped = true;

                monsterAudio?.StopMove(); // ⭐ 停腳步聲

                animator.SetTrigger("scream");

                monsterAudio?.PlayRoar();
                break;

            case MonsterState.Attack:
                agent.ResetPath();
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                monsterAudio?.StopMove();
                break;
        }
    }

    void CheckPlayer()
    {
        if (isDead) return;
        //Debug.Log("當前狀態: " + state);
        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (state != MonsterState.Attack && state != MonsterState.Scream)
        {
            if (distance < attackDistance && CanSeePlayer())
            {
                SetState(MonsterState.Attack);
                return;
            }

            if (CanSeePlayer())
            {
                if (state == MonsterState.Patrol)
                    SetState(MonsterState.Scream);
                else
                    SetState(MonsterState.Chase);
                return;
            }

            if (distance > loseDistance)
            {
                SetState(MonsterState.Patrol);
            }
        }
        else
        {
            FacePlayer();
            agent.isStopped = true;
        }

    }
    void OnAnimatorMove()
    {
        if (agent == null) return;

        // 🔥 強制用 NavMesh 的位置
        transform.position = agent.nextPosition;
    }
    void Patrol()
    {
        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;

            Vector3 randomDir = Random.insideUnitSphere * moveRange + transform.position;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, moveRange, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    void Chase()
    {
        //Debug.Log("追逐玩家");
        if (isGettingHit) return; // ⭐ 受擊中不追逐
        agent.isStopped = false;
        agent.SetDestination(player.transform.position);
    }

    void Attack()
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);
        FacePlayer();
        // ⭐ 在攻擊距離內 → 持續攻擊
        if (distance < attackDistance && !isAttacking && !isGettingHit)
        {
            isAttacking = true;
            animator.SetTrigger("attack");
            monsterAudio?.PlayAttack();
        }
    }

    void FacePlayer()
    {
        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0;

        if (dir == Vector3.zero) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 5f);
    }

    bool CanSeePlayer()
    {
        Vector3 dir = player.transform.position - transform.position;

        if (dir.magnitude > viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, dir);
        float viewAngle = GameManager.Instance.isDay ? dayViewAngle : nightViewAngle;

        if (angle > viewAngle * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up, dir.normalized, out RaycastHit hit, viewDistance))
        {
            return hit.collider.CompareTag("Player");
        }

        return false;
    }

    public void OnAttackHit()
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);
        //命中判定：距離玩家在攻擊距離內且在前方扇形範圍180度內

        if (distance < attackDistance + 0.5f && Vector3.Angle(transform.forward, player.transform.position - transform.position) < 90f)
        {
            GameManager.Instance.SleepToMorning(player.transform.position, true);
            playerInventory.RemoveAllById("9");
            //Debug.Log("攻擊命中，距離玩家: " + distance);
        }
    }

    public void BeBeaten()
    {
        if (playerInventory.GetItemCount("11") == 7)    
        {
            health--;
            isAttacking = false;
            monsterAudio?.StopMove(); // ⭐ 防止腳步聲蓋掉
            GameManager.Instance.PlayDamagedSound();
            if (health <= 0 && !isDead)
            {
                Debug.Log("怪物被打死了");
                isDead = true;
                animator.SetTrigger("die");
                monsterAudio?.PlayDeath();
                GameManager.Instance.monsterExist = false; // ⭐ 告訴 GameManager 怪物死了
                GameManager.Instance.ShowEnding();
            }
            else if (!isDead)
            {
                Debug.Log("怪物受擊了，剩餘血量: " + health);
                isGettingHit = true;
                SetState(MonsterState.Chase);
                animator.SetTrigger("getHit");

                monsterAudio?.PlayHit();
            }
        }else
        {
            player.GetComponent<PlayerCtrl>()?.ShowBubble("攻擊似乎對牠沒什麼效果...");
        }

    }
    public void OnScreamEnd()
    {
        Debug.Log("尖叫動畫結束，切回追逐");
        SetState(MonsterState.Chase);
    }
    public void OnAttackEnd()
    {
        Debug.Log("攻擊動畫結束，切回追逐");
        isAttacking = false;
        SetState(MonsterState.Chase);
    }
    void UpdateAnimation()
    {
        float speed = agent.velocity.magnitude;

        bool isMoving = speed > 0.1f && !agent.isStopped;

        animator.SetBool("iswalking", isMoving && state == MonsterState.Patrol);
        animator.SetBool("isrunning", isMoving && state == MonsterState.Chase);
        // ⭐ 音效控制（重點）
        if (isMoving)
        {
            if (state == MonsterState.Patrol)
                monsterAudio?.PlayWalk(1f);
            else if (state == MonsterState.Chase)
                monsterAudio?.PlayWalk(1.5f);
        }
        else
        {
            monsterAudio?.StopMove();
        }
    }
    public void OnGetHitEnd()
    {
        isGettingHit = false;
    }
}