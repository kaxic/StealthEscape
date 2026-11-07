using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    //プレイヤーへの参照
    [SerializeField] private PlayerMovement playerMovement;

    //発見できる最大距離
    [SerializeField] private float viewDistance = 6f;

    //視野角（左右合わせた角度）
    [SerializeField] private float viewAngle = 90f;

    //視界を遮る障害物のレイヤーマスク
    [SerializeField] private LayerMask obstacleMask;

    //発見時に表示する警告UI
    [SerializeField] private GameObject detectionWarning;

    //
    public bool IsDetecting { get; private set; }

    //前のフレームで発見していたか
    private bool wasDetected = false;

    private void Start()
    {
        IsDetecting = false;
        SetWarningVisible(false);
    }

    void Update()
    {
        if (playerMovement == null || !playerMovement.enabled) 
        {
            IsDetecting = false;
            wasDetected = false;
            SetWarningVisible(false);
            return;
        }

        //プレイヤーが視界に入っているかどうかを調べる
        IsDetecting = IsPlayerInSight();

        //視界に入った瞬間だけログを表示
        if (IsDetecting && !wasDetected)
        {
            Debug.Log("PLAYER DETECTED!");
        }

        //発見中だけ警告UIを表示
        SetWarningVisible(IsDetecting);

        //現在の発見状態を記録
        wasDetected = IsDetecting;
    }


    private void SetWarningVisible(bool visible)
    {
        if (detectionWarning == null)
        {
            return;
        }

        if(detectionWarning.activeSelf != visible)
        {
            detectionWarning.SetActive(visible);
        }
    }


    private bool IsPlayerInSight()
    {
        //敵とプレイヤーの位置
        Vector3 enemyPosition = transform.position;
        Vector3 playerPosition = playerMovement.transform.position;

        //敵からプレイヤーへの方向
        Vector3 toPlayer = playerPosition - enemyPosition;

        //高さの違いを無視
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
        Vector3 rayDirection = playerPosition - enemyPosition;

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
