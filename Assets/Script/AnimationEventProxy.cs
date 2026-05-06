using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpEventProxy : MonoBehaviour
{
    PlayerCtrl player;
    void Start()
    {
        player = transform.parent.GetComponent<PlayerCtrl>();
    }
    public void CallJumpForce()
    {
        if (player != null)
        {
            player.OnJumpForce();
        }
    }

    public void CallLanding()
    {
        if (player != null)
        {
            player.OnLanding();
        }
    }
    public void CallReady()
    {
        if (player != null)
        {
            player.OnReady();
        }
    }
    public void OnHitEnd()
    {
        if (player != null)
        {
            player.OnHitEnd();
        }
    }
    public void OnHitStart()
    {
        if (player != null)
        {
            player.CheckHitEnemy();
        }
        GameManager.Instance.PlayHitSound();
    }
    public void CallPickItemAttach()
    {
        if (player != null)
        {
            player.OnPickItemAttach();
        }
    }

    public void CallPickItemFinish()
    {
        if (player != null)
        {
            player.OnPickItemFinish();
        }
    }
    public void OnBeatenBubble()
    {
        player.ShowBubble("咦，我怎麼在這裡？");
    }

    public void OnBeatenRecover()
    {
        // 恢復移動與攻擊能力
        player.isLanding = false;
        Animator anim = GetComponent<Animator>();
        anim.Play("Standing");
        //Debug.Log("玩家已恢復正常");
    }
}