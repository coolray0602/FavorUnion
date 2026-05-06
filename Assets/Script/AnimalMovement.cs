using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private GameObject player;
    private PlayerCtrl playerCtrl;

    [Header("移動設定")]
    public float moveInterval = 10f; // 每多少秒生成新的目的地
    public float moveRange = 10f;    // 目的地生成範圍
    [Header("黑煙特效")]
    public GameObject smokeEffect;

    [Header("變形成的怪物")]
    public GameObject monsterPrefab;
    private bool freezing = false;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        playerCtrl = player.GetComponent<PlayerCtrl>();
    }

    private void Start()
    {
        StartCoroutine(MoveRoutine());
    }

    void Update()
    {
        float speed = agent.velocity.magnitude;

        if (speed > 0.1f)
        {
            // 動物移動中
            animator.SetBool("isWalking", true);
            animator.speed = 3f;

            // 動物平滑旋轉
            Vector3 lookDir = agent.velocity.normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
            }
        }
        else
        {
            // 動物停止
            animator.SetBool("isWalking", false);
            animator.speed = 1f;

            // 保持 agent 運行，不設 isStopped = true
            // 避免微移動的另一種方式是鎖住 y 軸或禁用 Root Motion
        }
    }
void OnAnimatorMove()
{
    if (agent == null) return;

    // 🔥 強制用 NavMesh 的位置
    transform.position = agent.nextPosition;
}
    IEnumerator MoveRoutine()
    {
        while (!freezing)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            bool playerHeldBurger = playerCtrl.GetHeldItemID() == "10";

            if (distanceToPlayer < 10f && playerHeldBurger)
            {
                if (distanceToPlayer > 1f)
                {
                    agent.SetDestination(player.transform.position);
                }
                else
                {
                    agent.ResetPath();

                    Vector3 lookDir = (player.transform.position - transform.position).normalized;
                    lookDir.y = 0f;

                    if (lookDir != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            lookRotation,
                            5f * Time.deltaTime
                        );
                    }
                }

                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                Vector3 randomPos = transform.position + new Vector3(
                    Random.Range(-moveRange, moveRange),
                    0,
                    Random.Range(-moveRange, moveRange)
                );

                NavMeshHit hit;

                if (NavMesh.SamplePosition(randomPos, out hit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }

                yield return new WaitForSeconds(moveInterval);
            }
        }
    }
    public void receiveItem(GameObject item)
    {
        string itemID = item.GetComponent<ItemHolder>()?.item.id;

        if (itemID == "10") // 漢堡 ID
        {
            // 觸發黑煙特效
            if (smokeEffect != null)
            {
                //先生成特效實體
                smokeEffect = Instantiate(smokeEffect, transform.position, Quaternion.identity);
                smokeEffect.SetActive(true);
                StartCoroutine(DisableSmokeAfterDelay(2f));
            }
            //playerCtrl.ShowBubble("給小貓漢堡了！");
            freezing = true; // 冻结动物

        }
        else
        {
            playerCtrl.ShowBubble("它似乎沒什麼興趣...");
        }
        return;
    }
    IEnumerator DisableSmokeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (smokeEffect != null)
        {
            smokeEffect.SetActive(false);
        }
        // 銷毀動物
        Destroy(gameObject);
        // 生成怪物
        GameObject monster = Instantiate(monsterPrefab, transform.position, Quaternion.identity);
    }

}
