using UnityEngine;
using System.Collections;

public class LitterAutoDestroy : MonoBehaviour
{
    public float lifeTime = 10f;
    public float protectDistance = 10f;

    private Transform player;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        StartCoroutine(DestroyRoutine());
    }

    IEnumerator DestroyRoutine()
    {
        yield return new WaitForSeconds(lifeTime);

        // 如果玩家存在，檢查距離
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= protectDistance)
            {
                // 玩家太近 → 延後再檢查
                StartCoroutine(DestroyRoutine());
                yield break;
            }
        }

        // 玩家不在保護範圍內 → 刪除
        Destroy(gameObject);
    }
}