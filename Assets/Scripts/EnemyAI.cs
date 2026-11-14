using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    //敵が行ってる行動
    private enum EnemyState
    {
        Patrol,      //通常巡回
        Chase ,      //追跡
        Search,      //捜索
        Investigate, //足音の調査
    }


    [Header("References")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private EnemyVision enemyVision;
    [SerializeField] private EnemyHearing enemyHearing;
    [SerializeField] private EnemyAwareness enemyAwareness;


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


    [Header("Investigate")]
    [SerializeField,Min(0f)] private float investigateWaitTime = 2f;
    [SerializeField, Min(0.1f)] private float investigateRetargetCooldown = 0.75f;
    [SerializeField, Min(0f)] private float investigateRetargetMinDistance = 1.5f;


    private NavMeshAgent agent;

    private CharacterController playerController;

    private EnemyState currentState = EnemyState.Patrol;
    public string CurrentStateName => currentState.ToString();

    public bool IsChasing => currentState == EnemyState.Chase;


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


    //最後に聞いた足音の位置
    private Vector3 lastHeardPosition;

    //調査地点に到着したか
    private bool investigateArrived = false;

    //調査地点での残り待機時間
    private float investigateTimer = 0f;

    //調査地点に到着した時の向き
    private Quaternion investigateStartRotation;

    //見回し開始からの経過時間
    private float investigateLookElapsed = 0f;

    //次に調査先を更新できる時刻
    private float nextInvestigateRetargetTime = 0f;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (enemyVision == null)
        {
            enemyVision = GetComponent<EnemyVision>();
        }

        if (enemyAwareness == null)
        {
            enemyAwareness = GetComponent<EnemyAwareness>();
        }
    }

    private void OnEnable()
    {
        //聴覚コンポーネントを取得
        if (enemyHearing == null)
        {
            enemyHearing = GetComponent<EnemyHearing>();
        }

        //足音検知イベントを購読
        if (enemyHearing != null)
        {
            enemyHearing.FootstepHeard += OnFootstepHeard;
        }
    }

    private void OnDisable()
    {
        //イベントの購読を解除
        if (enemyHearing != null)
        {
            enemyHearing.FootstepHeard -= OnFootstepHeard;
        }
    }

    private void Start()
    {
        if (playerMovement == null || false || enemyVision == null || enemyAwareness == null || waypoints == null || waypoints.Length == 0) 
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


        //プレイヤーが視界内にいるか
        bool canSeePlayer = enemyVision.isActiveAndEnabled && enemyVision.IsDetecting;

        //警戒度が最大か
        bool fullyDetected = enemyAwareness.isActiveAndEnabled && enemyAwareness.IsAtMaximum;

        if (canSeePlayer && (currentState == EnemyState.Chase || fullyDetected))
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

        //足音調査中の処理
        if (currentState == EnemyState.Investigate)
        {
            UpdateInvestigate();
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

        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
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


    private void EnterInvestigate(Vector3 soundPosition)
    {
        //足音の調査状態にする
        currentState = EnemyState.Investigate;

        //足音の位置を記録
        lastHeardPosition = soundPosition;

        //通常巡回と同じ速度
        agent.speed = patrolSpeed;
        agent.updateRotation = true;
        agent.isStopped = false;

        //状態を初期化
        investigateArrived = false;
        investigateTimer = investigateWaitTime;

        //安全な経路を設定
        bool success = SafeNavMeshMove.TryMove(agent, lastHeardPosition);

        //最初の調査開始から一定時間は調査先を変更しない
        nextInvestigateRetargetTime = Time.time + investigateRetargetCooldown;

        if (!success)
        {
            Debug.LogWarning("足音の位置へ到達できません。");

            // 移動できない場合は巡回へ戻す
            EnterPatrol();
            return;
        }


        Debug.Log("INVESTIGATE START");
    }

    private void UpdateInvestigate()
    {
        //移動中の場合
        if (!investigateArrived)
        {
            if (agent.pathPending)
            {
                return;
            }

            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                EnterPatrol();
                return;
            }

            if (agent.remainingDistance > agent.stoppingDistance)
            {
                return;
            }


            //目的地に到着
            investigateArrived = true;

            //移動を停止
            StopMovement();

            //自動回転を停止
            agent.updateRotation = false;

            //到着した瞬間の向きを保存
            investigateStartRotation = transform.rotation;

            //見回しの経過時間をリセット
            investigateLookElapsed = 0f;

            Debug.Log("INVESTIGATE WAIT");
        }

        //その場で左右を見渡す
        investigateLookElapsed += Time.deltaTime;

        //左右に往復する角度を計算
        float lookAngle = Mathf.Sin(investigateLookElapsed * searchLookSpeed) * searchLookAngle;

        //到着した瞬間の向きを基準に左右に回転
        Quaternion targetRotation = investigateStartRotation * Quaternion.Euler(0f, lookAngle, 0f);

        //目標の向きへ回転
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, searchTurnSpeed * Time.deltaTime);


        //残り時間を減らす
        investigateTimer -= Time.deltaTime;


        //終了後に巡回に戻る
        if (investigateTimer <= 0f)
        {
            Debug.Log("INVESTIGATE END");
            EnterPatrol();
        }
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

    private void OnFootstepHeard(Vector3 soundPosition)
    {
        if (playerMovement == null || !playerMovement.enabled)
        {
            return;
        }

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            return;
        }


        //視覚でプレイヤーを発見している時は視覚を優先
        if (enemyVision != null && enemyVision.IsDetecting)
        {
            return;
        }

        //通常巡回中のみ足音を拾う
        if (currentState == EnemyState.Patrol)
        {
            //足音がした場所を調査
            EnterInvestigate(soundPosition);
            return;
        }

        //調査中なら足音の位置の更新を試みる
        if (currentState == EnemyState.Investigate)
        {
            TryUpdateInvestigateTarget(soundPosition);
            return;
        }
    }

    private void TryUpdateInvestigateTarget(Vector3 soundPosition)
    {
        //更新間隔を確認
        if (Time.time < nextInvestigateRetargetTime)
        {
            return;
        }


        //前回の調査先からの距離を調べる
        Vector3 difference = soundPosition - lastHeardPosition;
        difference.y = 0f;

        float minDistanceSqr = investigateRetargetMinDistance * investigateRetargetMinDistance;

        //足音が近ければ更新しない
        if(difference.sqrMagnitude < minDistanceSqr)
        {
            return;
        }


        //次に更新できる時刻
        nextInvestigateRetargetTime = Time.time + investigateRetargetCooldown;

        //移動を再開
        agent.isStopped = false;
        agent.updateRotation = true;


        //新しい経路を設定
        bool success = SafeNavMeshMove.TryMove(agent, soundPosition);

        if (!success)
        {
            Debug.LogWarning("新しい足音の位置に到達できません。");

            // 安全のため、古い経路を使わず巡回へ戻る
            EnterPatrol();
            return;
        }


        //調査先を更新
        lastHeardPosition = soundPosition;

        investigateArrived = false;
        investigateTimer = investigateWaitTime;
        investigateLookElapsed = 0f;

        Debug.Log("INVESTIGATE TARGET UPDATED: " + soundPosition);
    }
}
