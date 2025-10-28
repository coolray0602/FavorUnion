using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpEventProxy : MonoBehaviour
{
    public void CallJumpForce()
    {
        PlayerCtrl player = transform.parent.GetComponent<PlayerCtrl>();
        if (player != null)
        {
            player.OnJumpForce();
        }
        else
        {
            Debug.LogError("no PlayerCtrl!");
        }
    }
    
    public void CallLanding() {
        PlayerCtrl player = transform.parent.GetComponent<PlayerCtrl>();
        if (player != null)
        {
            player.OnLanding();
        }
        else
        {
            Debug.LogError("no PlayerCtrl!");
        }
    }
    public void CallReady()
    {
        PlayerCtrl player = transform.parent.GetComponent<PlayerCtrl>();
        if (player != null)
        {
            player.OnReady();
        }
        else
        {
            Debug.LogError("no PlayerCtrl!");
        }
    }
}