using UnityEngine;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;

    private bool isChasing = false;

    public float gameOverDistance = 1.0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        // 一開始先停止（如果你希望一開始就不要跑）
        agent.isStopped = true;
    }

    public void StartChasing()
    {
        // 只執行一次
        if (isChasing) return;
        isChasing = true;

        // 🔥 1. 播放跑步動畫
        if (animator != null)
            animator.SetTrigger("run");

        // 2. 允許 Agent 移動
        agent.isStopped = false;
    }

    private void Update()
    {
        if (!isChasing || player == null) return;

        // 持續追玩家
        agent.SetDestination(player.position);

        // 距離判定 → Game Over
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= gameOverDistance)
        {
           GameManager.Instance.GameOver();
        }
    }

    private void GameOver()
    {
        Debug.Log("GAME OVER");
        agent.isStopped = true;

        // 之後可以切換結算場景、顯示 UI 等
    }
}
