using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    //巡回地点の配列
    [SerializeField] private Transform[] waypoints;

    //プレイヤーの移動スクリプト
    [SerializeField] private PlayerMovement playerMovement;

    private NavMeshAgent agent;

    //現在のウェイポイントのインデックス
    private int currentWaypointIndex = 0;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }


    private void Start()
    {
        //巡回地点が設定されているか確認
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("巡回地点が設定されていません");
            enabled = false;
            return;
        }

        //
        if(playerMovement == null)
        {
            Debug.LogWarning("PlayerMovementが設定されていません");
            enabled = false;
            return;
        }

        //EnemyがNavMesh上に配置されているか確認
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("EnemyがNavMesh上に配置されてません");
            enabled = false;
            return;
        }

        //最初の巡回地点へ移動開始
        MoveToCurrentWaypoint();
    }


    private void Update()
    {
        //NavMesh上にいなければ処理を行わない
        if (!agent.isOnNavMesh) 
        {
            return;
        }

        //ゲーム終了時は敵も停止
        if(!playerMovement.enabled)
        {
            //プレイヤーが移動できない状態なら巡回を停止
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            return;
        }

        //経路を計算している時は待機
        if (agent.pathPending)
        {
            return;
        }

        //正常な経路がない場合は待機
        if (agent.pathStatus != NavMeshPathStatus.PathComplete) 
        {
            return;
        }

        //目的地までの距離を調べる
        if(agent.remainingDistance <= agent.stoppingDistance)
        {
            //次の巡回地点へ切り替える
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            
            MoveToCurrentWaypoint();
        }
    }


    private void MoveToCurrentWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];

        if (target == null)
        {
            Debug.LogWarning("巡回地点の参照がありません");
            enabled = false;
            return;
        }

        agent.SetDestination(target.position);
    }
}
