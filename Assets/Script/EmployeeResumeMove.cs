using UnityEngine;
using UnityEngine.AI;

public class EmployeeResumeMove : StateMachineBehaviour
{
    // 進入這個 Animator State 時會被呼叫一次
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var agent = animator.GetComponent<NavMeshAgent>();
        var movement = animator.GetComponent<EmployeeMovement>();

        if (agent != null && movement != null)
        {
            agent.isStopped = false;
            agent.SetDestination(movement.TargetPosition);

            // 移除氣泡（如果存在）
            if (movement.speechBubbleInstance != null)
            {
                Destroy(movement.speechBubbleInstance);
                movement.speechBubbleInstance = null;
            }
        }
    }
}
