using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    //敵が行ってる行動
    private enum EnemyState
    {
        Patrol, //巡回
        Chase , //追跡
        Search, //捜索
    }


    [Header("References")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private EnemyVision enemyVision;


    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float chaseUpdateInterval = 0.3f;


    [Header("Detection")]
    [SerializeField, Min(0f)] private float loseSightDelay = 1f;


    [Header("Search")]
    [SerializeField] private float searchWaitTime = 3f;
    [SerializeField] private float searchLookAngle = 60f;
    [SerializeField] private float searchLookSpeed = 2f;
    [SerializeField] private float searchTurnSpeed = 180f;


    private NavMeshAgent agent;

    private CharacterController playerController;

    private EnemyState currentState = EnemyState.Patrol;
    public string CurrentStateName => currentState.ToString();


    private int waypointIndex = 0;
    private float chaseTimer = 0f;
    private float lostSightTimer = 0f;


    //最後にプレイヤーを発見した位置
    private Vector3 lastKnownPosition;

    //捜索地点に到着したか
    private bool searchArrived = false;

    //捜索地点での残り待機時間
    private float searchTimer = 0f;

    //捜索地点に到着した瞬間の向き
    private Quaternion searchStartRotation;

    //見回しを開始してから経過した時間
    private float searchLookElapsed = 0f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (enemyVision == null)
        {
            enemyVision = GetComponent<EnemyVision>();
        }

        if (playerMovement != null) 
        {
            //playerController = playerMovement.GetComponent<CharacterController>();
        }
    }

    private void Start()
    {
        if (playerMovement == null || false || enemyVision == null || waypoints == null || waypoints.Length == 0) 
        {
            Debug.LogError("EnemyAIの参照が不足しています");
            enabled = false;
            return;
        }

        //CharacterControllerを取得
        playerController = playerMovement.GetComponent<CharacterController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerにCharacterControllerがありません");
            enabled = false;
            return;
        }

        // シーンでは初期無効。NavMeshSurfaceの登録が完了したStartで起動する。
        agent.enabled = true;

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("EnemyがNavMesh上に配置されてません");
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
            //最後に見た位置を更新
            lastKnownPosition = GetPlayerFootPosition();

            //見失った時間をリセット
            lostSightTimer = 0f;


            if (currentState != EnemyState.Chase)
            {
                EnterChase();
            }

            UpdateChase();
            return;
        }

        //プレイヤーを見失った場合捜索へ移行
        if (currentState == EnemyState.Chase)
        {
            //見失ってる時間を加算
            lostSightTimer += Time.deltaTime;
            if(lostSightTimer < loseSightDelay)
            {
                UpdateChase();
                return;
            }


            EnterSearch();
            return;
        }

        //捜索中の処理
        if (currentState == EnemyState.Search)
        {
            UpdateSearch();
            return;
        }

        //それ以外は巡回
        UpdatePatrol();
    }


    //プレイヤーの足元のデータを返す
    private Vector3 GetPlayerFootPosition()
    {
        Vector3 position = playerMovement.transform.position;
        position.y = playerController.bounds.min.y;

        return position;
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

        //最後に見たプレイヤーの位置に向かう
        bool success = SafeNavMeshMove.TryMove(agent, lastKnownPosition);
        
        if (!success)
        {
            //経路を設定できなければ停止
            //次回追跡更新時に再試行
            //agent.velocity = Vector3.zero;
        }
    }


    private void EnterSearch()
    {
        currentState = EnemyState.Search;

        agent.speed = patrolSpeed;
        agent.updateRotation = true;
        agent.isStopped = false;


        searchArrived = false;
        searchTimer = searchWaitTime;

        //最後に見た位置への経路を設定
        bool success = SafeNavMeshMove.TryMove(agent, lastKnownPosition);

        if (!success)
        {
            Debug.LogWarning("捜索地点に到達できません");
            EnterPatrol();
            return;
        }

        Debug.Log("SEARCH START");
    }

    private void UpdateSearch()
    {
        //まだ捜索地点に到着してない場合
        if (!searchArrived)
        {
            //経路計算は待つ
            if (agent.pathPending)
            {
                return;
            }

            //経路が無効なら巡回へ戻る
            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                EnterPatrol();
                return;
            }

            //まだ移動中なら待つ
            if (agent.remainingDistance > agent.stoppingDistance)
            {
                return;
            }


            //捜索地点に到着
            searchArrived = true;
            StopMovement();

            //自動回転を停止
            agent.updateRotation = false;

            //到着した瞬間の向きを記録
            searchStartRotation = transform.rotation;

            //見回し時間を無効化
            searchLookElapsed = 0f;

            Debug.Log("SEARCH WAIT");
        }

        //捜索中の残り時間を更新
        searchLookElapsed += Time.deltaTime;

        //左右に往復する角度を計算
        float lookAngle = Mathf.Sin(searchLookElapsed * searchLookSpeed) * searchLookAngle;

        //到着した瞬間の向きを基準に左右に回転
        Quaternion targetRotation = searchStartRotation * Quaternion.Euler(0f, lookAngle, 0f);

        //目標の向きへ回転
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, searchTurnSpeed * Time.deltaTime);


        //捜索の残り時間を減らす
        searchTimer -= Time.deltaTime;


        //3秒経過後に巡回に戻る
        if (searchTimer <= 0f)
        {
            EnterPatrol();
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
