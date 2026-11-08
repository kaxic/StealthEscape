using UnityEngine;
using UnityEngine.AI;

public static class SafeNavMeshMove
{
    //目的地が到達可能な場合だけ移動する
    public static bool TryMove(NavMeshAgent agent, Vector3 targetPosition)
    {
        //Agentの状態を確認
        if (agent == null || !agent.isOnNavMesh) 
        {
            return false;
        }

        //目的地付近にNavMeshがあるか確認
        bool found = NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 0.75f, agent.areaMask);

        if (!found)
        {
            StopAgent(agent);
            return false;
        }

        //経路を計算
        NavMeshPath path = new NavMeshPath();

        bool hasPath = agent.CalculatePath(hit.position, path);


        //完全な経路だけを採用
        if (!hasPath || path.status != NavMeshPathStatus.PathComplete) 
        {
            StopAgent(agent);
            return false;
        }

        //確認済みの経路を設定
        if (!agent.SetPath(path))
        {
            StopAgent(agent);
            return false;
        }

        return true;
    }


    private static void StopAgent(NavMeshAgent agent)
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        //古い経路を破棄
        agent.ResetPath();

        //残っている移動速度を消す
        agent.velocity = Vector3.zero;
    }
}
