using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float  moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        //1.キーボードの入力を取得
        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;

        if(keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;
        }

        //2.斜め移動の速度を制限
        input = Vector2.ClampMagnitude(input, 1f);

        //3.入力を3D空間の方向に変換
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        //4.地面に接している時の落下速度を調整
        if(controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        //5.重力によって落下速度を変化
        verticalVelocity += gravity * Time.deltaTime;

        //6.移動速度と重力を合成
        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalVelocity;

        //7.実際にプレイヤーを移動
        controller.Move(velocity * Time.deltaTime);
    }
}
