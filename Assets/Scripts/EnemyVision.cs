using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [Header("References")]
    //プレイヤーへの参照
    [SerializeField] private PlayerMovement playerMovement;


    [Header("Vision")]
    //発見できる最大距離
    [SerializeField] private float viewDistance = 6f;
    //視野角（左右合わせた角度）
    [SerializeField] private float viewAngle = 90f;
    //視界を遮る障害物のレイヤーマスク
    [SerializeField] private LayerMask obstacleMask;
    //プレイヤーの頭の最上部から少し下を見る
    [SerializeField, Range(0f, 0.4f)] private float targetTopInset = 0.15f; 
    

    //他のスクリプトが読み取る用
    public bool IsDetecting { get; private set; }


    private CharacterController playerController;
    private bool wasDetected = false;


    private void Start()
    {
        IsDetecting = false;

        //Playerの参照を確認
        if (playerMovement == null)
        {
            Debug.LogError("EnemyVisionにPlayerが設定されていません。");
            enabled = false;
            return;
        }

        playerController = playerMovement.GetComponent<CharacterController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerにCharacterControllerがありません。");
            enabled = false;
        }
    }

    void Update()
    {
        if (playerMovement == null || playerController == null || !playerMovement.enabled) 
        {
            IsDetecting = false;
            wasDetected = false;
            return;
        }

        //プレイヤーが視界に入っているかどうかを調べる
        IsDetecting = IsPlayerInSight();

        //視界に入った瞬間だけログを表示
        if (IsDetecting && !wasDetected)
        {
            Debug.Log("PLAYER IN SIGHT!");
        }

        //現在の発見状態を記録
        wasDetected = IsDetecting;
    }


    private bool IsPlayerInSight()
    {
        //敵の位置
        Vector3 enemyPosition = transform.position;

        //プレイヤーの実際の高さ
        Bounds playerBounds = playerController.bounds;


        //プレイヤーの上端より少し下を視線の目標にする
        Vector3 playerTarget = playerBounds.center;
        playerTarget.y = playerBounds.max.y - targetTopInset;


        //敵からプレイヤーへの方向
        Vector3 toPlayer = playerTarget - enemyPosition;
        toPlayer.y = 0f;

        //1.距離の判定
        float distance = toPlayer.magnitude;


        if (distance > viewDistance)
        {
            return false;
        }

        //敵の正面方向
        Vector3 forward = transform.forward;
        forward.y = 0f;

        //2.角度の判定
        float angle = Vector3.Angle(forward, toPlayer);

        
        if (angle > viewAngle / 2f)
        {
            return false;
        }

        //3.壁による遮蔽判定
        Vector3 rayDirection = playerTarget - enemyPosition;
        float rayDistance = rayDirection.magnitude;

        //敵とプレイヤーの間に壁があるか
        bool isBlocked = Physics.Raycast(enemyPosition, rayDirection.normalized, rayDistance, obstacleMask,QueryTriggerInteraction.Ignore);

        //壁に遮られてるなら発見しない
        if(isBlocked)
        {
            return false;
        }

        //全条件を満たした
        return true;
    }
}
