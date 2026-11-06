using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -9f);


    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        //プレイヤーから一定距離離れた位置に移動
        transform.position = target.position + offset;

        //プレイヤーの方向を向く
        transform.LookAt(target);
    }
}
