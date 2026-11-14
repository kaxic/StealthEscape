using System;
using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField, Min(0.05f)] private float stepInterval = 0.5f;
    [SerializeField, Min(0f)] private float minimumMoveSpeed = 0.2f;


    [Header("Hearing Range")]
    [SerializeField, Min(0f)] private float walkHearingRadius = 6f;
    [SerializeField, Min(0f)] private float crouchHearingRadius = 2f; 


    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;


    //足音発生を他のスクリプトに通知するイベント
    public event Action<Vector3, float> FootstepOccurred;


    private CharacterController controller;
    private PlayerMovement playerMovement;


    //次の足音までの残り時間
    private float stepTimer = 0f;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();

        if (controller == null || playerMovement == null)
        {
            Debug.LogError("必要なコンポーネントがありません");
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if(!playerMovement.enabled)
        {
            stepTimer = 0f;
            return;
        }

        //移動速度を取得
        Vector3 horizontalVelocity = controller.velocity;

        //上下移動は除外
        horizontalVelocity.y = 0f;

        //地面にいてなおかつ一定以上の速度で移動しているか
        bool isMoving = controller.isGrounded && horizontalVelocity.sqrMagnitude > minimumMoveSpeed * minimumMoveSpeed;

        if (!isMoving)
        {
            stepTimer = 0f;
            return;
        }


        //次の足音までの時間を減らす
        stepTimer -= Time.deltaTime;

        if(stepTimer > 0f)
        {
            return;
        }

        //足音を発生させてタイマーリセット
        stepTimer = stepInterval;
        GenerateFootstep();
    }

    //足音イベントを発生
    private void GenerateFootstep()
    {
        //足元の位置を取得
        Vector3 footPosition = controller.bounds.center;
        footPosition.y = controller.bounds.min.y;

        //半径を決定
        float hearingRadius = playerMovement.IsCrouching ? crouchHearingRadius : walkHearingRadius;

        //デバッグ用
        string moveType = playerMovement.IsCrouching ? "CROUCH" : "WALK";

        //ログで足音の発生を確認（デバッグ用）
        if (showDebugLog)
        {
            Debug.Log("FOOTSTEP [" + moveType + "] " + "Range = " + hearingRadius + " Position = " + footPosition);
        }


        //他のスクリプトへ足音発生を通知
        FootstepOccurred?.Invoke(footPosition, hearingRadius);
    }
}
