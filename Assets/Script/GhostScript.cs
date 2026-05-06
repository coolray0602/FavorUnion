using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GhostScript : MonoBehaviour
{
    [Header("Drop Setting")]
    public Item springItem;   // 指定 id="9" 的那個 ScriptableObject
    private UnityEngine.AI.NavMeshAgent agent;
    private GameObject player;
    private Transform playerTransform;
    private Animator animator;
    private PlayerInventory playerInventory; // 玩家背包引用

    public float gameOverDistance = 2.0f;
    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();

        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerInventory = player.GetComponent<PlayerInventory>();
        }

        agent.isStopped = false;
    }

    private void Update()
    {   
        if(GameManager.Instance.onEnding)
        {
            agent.isStopped = true;
            animator.SetBool("running", false);
            return; // 過關畫面中不處理移動和攻擊
        }
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= gameOverDistance)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            agent.isStopped = true;
            animator.SetBool("running", false);
            animator.SetTrigger("attack");
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            animator.SetBool("running", true);
        }
    }
void OnAnimatorMove()
{
    if (agent == null) return;

    // 🔥 強制用 NavMesh 的位置
    transform.position = agent.nextPosition;
}
    public void attack()
    {
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= gameOverDistance && !player.GetComponent<PlayerCtrl>().fainted)
        {
            GameManager.Instance.SleepToMorning(transform.position, true);
            playerInventory.RemoveAllById("9"); // 移除所有 id="9" 的彈簧
            Debug.Log("Player Caught!");
            player.GetComponent<PlayerCtrl>().fainted= true;
        }
    }

    public void BeBeaten()
    {
        // 先確認玩家背包是否有 id = "4" 的道具
        if (playerInventory != null && playerInventory.items.Exists(i => i.item.id == "4"))
        {
            GameManager.Instance.PlayDamagedSound();
            Debug.Log("Ghost Beaten!");
            agent.isStopped = true;
            animator.SetTrigger("hitten");
            StartCoroutine(DissolveEffect());
        }
        else
        {
            Debug.Log("Ghost immune: player has no item id=\"4\"");
        }
    }

    IEnumerator DissolveEffect()
    {
        float duration = 1.5f;
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            float value = Mathf.Lerp(1f, 0f, t / duration);

            foreach (Renderer r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Dissolve", value);
                r.SetPropertyBlock(mpb);
            }

            yield return null;
        }
        // ⭐ 掉落彈簧
        DropItem();
        Destroy(gameObject);
    }
    private void DropItem()
    {
        if (springItem != null && springItem.worldPrefab != null)
        {
            Instantiate(
                springItem.worldPrefab,
                transform.position+ Vector3.up * 1f, // 稍微往上掉落，避免和地面重疊
                Quaternion.identity
            );

            Debug.Log("Dropped spring item!");
        }
        else
        {
            Debug.LogWarning("Spring item or prefab not assigned!");
        }
    }
}