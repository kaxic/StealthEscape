using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    //敵が行ってる行動
    private enum EnemyState
    {
        Patrol, //巡回
        Chase   //追跡
    }


    [Header("References")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private EnemyVision enemyVision;


    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float chaseUpdateInterval = 0.3f;


    private CharacterController playerController;
    private NavMeshAgent agent;
    private EnemyState currentState = EnemyState.Patrol;


    private int waypointIndex = 0;
    private float chaseTimer = 0f;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (enemyVision == null)
        {
            enemyVision = GetComponent<EnemyVision>();
        }

        if (playerMovement != null) 
        {
            playerController = playerMovement.GetComponent<CharacterController>();
        }
    }

    private void Start()
    {
        if (playerMovement == null || playerController == null || enemyVision == null || waypoints == null || waypoints.Length == 0) 
        {
            Debug.LogWarning("EnemyAIの参照が不足しています");
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("EnemyがNavMesh上に配置されてません");
            enabled = false;
            return;
        }


        agent.speed = patrolSpeed;
        agent.updateRotation = true;
        agent.isStopped = false;


        MoveToWayPoint();
    }


    private void Update()
    {
        if (!agent.isOnNavMesh)
        {
            return;
        }

        //ゲーム終了時は移動を停止
        if (!playerMovement.enabled)
        {
            StopMovement();
            return;
        }


        //プレイヤーを発見した場合
        if (enemyVision.IsDetecting)
        {
            if (currentState != EnemyState.Chase)
            {
                EnterChase();
            }

            UpdateChase();
            return;
        }

        //プレイヤーを見失った場合
        if (currentState == EnemyState.Chase)
        {
            EnterPatrol();
        }

        UpdatePatrol();
    }


    private void EnterChase()
    {
        currentState = EnemyState.Chase;


        agent.speed = chaseSpeed;
        agent.updateRotation = true;
        agent.isStopped = false;


        chaseTimer = 0f;

        Debug.Log("CHASE START");
    }

    private void UpdateChase()
    {
        chaseTimer -= Time.deltaTime;


        if (chaseTimer > 0f)
        {
            return;
        }

        chaseTimer = chaseUpdateInterval;

        //プレイヤーの現在位置を取得
        Vector3 targetPosition = playerMovement.transform.position;
        targetPosition.y = playerController.bounds.min.y;


        bool success = SafeNavMeshMove.TryMove(agent, targetPosition);
        
        if (!success)
        {
            //経路を設定できなければ停止
            //次回追跡更新時に再試行
            agent.velocity = Vector3.zero;
        }
    }


    private void EnterPatrol()
    {
        currentState = EnemyState.Patrol;

        agent.speed = patrolSpeed;
        agent.updateRotation = true;
        agent.isStopped = false;

        Debug.Log("PATROL RESUME");

        MoveToWayPoint();
    }

    private void UpdatePatrol()
    {
        if (agent.pathPending)
        {
            return;
        }

        if(agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            return;
        }

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            return;
        }


        waypointIndex = (waypointIndex + 1) % waypoints.Length;

        MoveToWayPoint();
    }

    private void MoveToWayPoint()
    {
        Transform target = waypoints[waypointIndex];

        if (target == null)
        {
            Debug.LogWarning("Waypointが未設定です");
            StopMovement();
            enabled = false;
            return;
        }


        //安全な経路設定を試みる
        bool success = SafeNavMeshMove.TryMove(agent, target.position);

        if (!success)
        {
            Debug.LogWarning("到達できない巡回地点：" + target.name);

            //巡回を停止
            StopMovement();
            enabled = false;
        }
    }

    private void StopMovement()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }
}
