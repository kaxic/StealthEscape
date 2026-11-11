using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;       //通常の移動速度
    [SerializeField] private float crouchSpeed = 2.5f;   //しゃがみ中の移動速度
    [SerializeField] private float gravity = -9.81f;


    [Header("Crouch")]
    [SerializeField] private Transform playerVisual;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float heightChangeSpeed = 4f;


    //立ち上がる時に確認する障害物のレイヤー
    [SerializeField] private LayerMask standingObstacleMask;


    //しゃがみ中かどうか
    public bool IsCrouching {  get; private set; }


    private CharacterController controller;
    private float verticalVelocity;

    //通常時の当たり判定保存用
    private float standingHeight;
    private Vector3 standingCenter;

    //通常時の見た目保存用
    private Vector3 standingVisualScale;
    private Vector3 standingVisualPosition;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        //Player本体を縮めないよう確認
        if (playerVisual == null || playerVisual == transform)
        {
            Debug.LogError("PlayerVisualにはPlayerの子オブジェクトを設定してください");
            enabled = false;
            return;
        }

        //開始時の高さと中心を保存
        standingHeight = controller.height;
        standingCenter = controller.center;

        //開始時の見た目を保存
        standingVisualScale = playerVisual.localScale;
        standingVisualPosition = playerVisual.localPosition;
    }

    private void Update()
    {
        //1.キーボードの入力を取得
        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;

        //左Ctrlキーをチェック
        bool crouchKeyPressed = false;

        if(keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;

            crouchKeyPressed = keyboard.leftCtrlKey.isPressed;
        }

        IsCrouching = crouchKeyPressed || !CanStandUp();

        //2.斜め移動の速度を制限
        input = Vector2.ClampMagnitude(input, 1f);

        //3.入力を3D空間の方向に変換
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        //4.しゃがみ状態の高さを更新
        UpdateCrouchHeight();

        //5.地面に接している時の落下速度を調整
        if(controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        //6.重力によって落下速度を変化
        verticalVelocity += gravity * Time.deltaTime;

        //7.しゃがみ状態によって移動速度を変化
        float currentSpeed = IsCrouching ? crouchSpeed : moveSpeed;

        //8.移動速度と重力を合成
        Vector3 velocity = direction * currentSpeed;
        velocity.y = verticalVelocity;

        //9.実際にプレイヤーを移動
        controller.Move(velocity * Time.deltaTime);
    }


    //立ち上がるための空間があるか確認
    private bool CanStandUp()
    {
        //しゃがんでなければ確認不要
        if (controller.height >= standingHeight - 0.01f)
        {
            return true;
        }


        //CharacterControllerの中心をワールド座標に変換
        Vector3 worldCenter = transform.TransformPoint(controller.center);

        //足元の高さを計算
        float feetY = worldCenter.y - controller.height * 0.5f;

        float radius = controller.radius;

        Vector3 currentHead = new Vector3(worldCenter.x, feetY + controller.height - radius, worldCenter.z);
        Vector3 standingHead = new Vector3(worldCenter.x, feetY + standingHeight - radius, worldCenter.z);


        //頭上に障害物があるか調べる
        bool isBlocked = Physics.CheckCapsule(currentHead, standingHead, radius, standingObstacleMask, QueryTriggerInteraction.Ignore);


        return !isBlocked;
    }

    //見た目と当たり判定の高さを変更
    private void UpdateCrouchHeight()
    {
        float targetHeight = IsCrouching ? crouchHeight : standingHeight;

        //一定速度で目標の高さへ近づける
        float newHeight = Mathf.MoveTowards(controller.height, targetHeight, heightChangeSpeed * Time.deltaTime);

        //高さが変わらなければ処理を省略
        if(Mathf.Approximately(controller.height,newHeight))
        {
            return;
        }


        //当たり判定の高さを変更
        controller.height = newHeight;


        //中心を下げる
        Vector3 newCenter = standingCenter;

        newCenter.y += (newHeight - standingHeight) * 0.5f;

        controller.center = newCenter;


        //見た目の高さを変更
        float heightRatio = newHeight / standingHeight;

        playerVisual.localScale = new Vector3(standingVisualScale.x, standingVisualScale.y * heightRatio, standingVisualScale.z);


        //見た目の足元を固定
        playerVisual.localPosition = standingVisualPosition + Vector3.up * ((newHeight - standingHeight) * 0.5f);
    }
}
