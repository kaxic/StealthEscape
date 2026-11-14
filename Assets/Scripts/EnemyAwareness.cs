using UnityEngine;

[RequireComponent(typeof(EnemyVision))]
public class EnemyAwareness : MonoBehaviour
{
    //警戒度の最大値
    private const float MaxAwareness = 100f;


    [Header("References")]
    [SerializeField] private EnemyVision enemyVision;
    [SerializeField] private PlayerMovement playerMovement;


    [Header("Awareness")]
    [SerializeField, Min(0f)] private float increasePerSecond = 66.67f;
    [SerializeField, Min(0f)] private float crouchIncreasePerSecond = 33.33f;
    [SerializeField, Min(0f)] private float decreasePerSecond = 50f;


    [Header("Runtime Debug")]
    [SerializeField, Range(0f, 100f)] private float awareness = 0f;


    //他のスクリプトから警戒度を読み取れる
    public float CurrentAwareness => awareness;

    //他のスクリプトから警戒度が最大かを読み取れる
    public bool IsAtMaximum => awareness >= MaxAwareness;

    //前のフレームで最大値だったか記録
    private bool wasAtMaximum = false;

    private void Awake()
    {
        if (enemyVision == null)
        {
            enemyVision = GetComponent<EnemyVision>();
        }

        if (enemyVision == null || playerMovement == null)
        {
            Debug.LogError("EnemyAwarenessの参照が不足しています。");
            enabled = false;
            return;
        }


        //最初は警戒度0
        awareness = 0f;
        wasAtMaximum = false;
    }

    private void LateUpdate()
    {
        if (!playerMovement.enabled)
        {
            return;
        }

        //Playerが見えているか
        bool canSeePlayer = enemyVision.isActiveAndEnabled && enemyVision.IsDetecting;

        //見えているなら100を見えていないなら0を目指す
        float targetAwareness = canSeePlayer ? MaxAwareness : 0f;

        //見えているなら増加見えていないなら減少
        float changeSpeed;

        if (canSeePlayer)
        {
            if (playerMovement.IsCrouching)
            {
                changeSpeed = crouchIncreasePerSecond;
            }
            else
            {
                changeSpeed = increasePerSecond;
            }
        }
        else
        {
            changeSpeed = decreasePerSecond;
        }

        //警戒度を時間で変化
        awareness = Mathf.MoveTowards(awareness, targetAwareness, changeSpeed * Time.deltaTime);

        //警戒度が最大になった瞬間にログを出す
        if (IsAtMaximum && !wasAtMaximum)
        {
            Debug.Log("AWARENESS MAX!");
        }

        wasAtMaximum = IsAtMaximum;
    }
}
