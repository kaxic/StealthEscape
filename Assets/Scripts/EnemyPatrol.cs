using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    //巡回地点の配列
    [SerializeField] private Transform[] waypoints;

    //プレイヤーの移動スクリプト
    [SerializeField] private PlayerMovement playerMovement;

    //敵の視界判定
    [SerializeField] private EnemyVision enemyVision;

    //発見中の回転速度（度/秒）
    [SerializeField] private float detectionTurnSpeed = 360f;

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


        if (playerMovement == null || enemyVision == null) 
        {
            Debug.LogWarning("PlayerMovementまたはEnemyVisionが設定されていません");
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

        //発見中は移動を停止してプレイヤーの方を向く
        if(enemyVision.IsDetecting)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            //NavMeshAgentの自動回転を一時停止
            agent.updateRotation = false;

            FacePlayer();
            return;
        }

        //未発見になったら巡回を再開
        agent.updateRotation = true;
        agent.isStopped = false;

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


    private void FacePlayer()
    {
        Vector3 direction = playerMovement.transform.position - transform.position;

        //高さの違いを無視して回転
        direction.y = 0f;


        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        //プレイヤー方向の目標回転
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        //一定速度で回転
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, detectionTurnSpeed * Time.deltaTime);
    }


    private void MoveToCurrentWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];

        if (target == null)
        {
            Debug.LogWarning("巡回地点の参照がありません");
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            enabled = false;
            return;
        }


        //安全な経路設定を試みる
        bool success = SafeNavMeshMove.TryMove(agent, target.position);

        if (!success)
        {
            Debug.LogWarning("到達できない巡回地点：" + target.name);

            //巡回を停止
            enabled = false;
        }
    }
}
