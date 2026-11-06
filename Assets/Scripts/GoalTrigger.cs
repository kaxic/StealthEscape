using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private GameObject clearPanel;

    private bool isCleared = false;

    private void Start()
    {
        //ゲーム開始時はクリアパネルを非表示
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //すでにクリア済みの場合は処理を行わない
        if (isCleared)
        {
            return;
        }

        //接触したオブジェクトがプレイヤーかどうかを判定
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if(player == null)
        {
            return;
        }

        //クリア処理
        isCleared = true;

        Debug.Log("GAME CLEAR!");

        //プレイヤーの移動を停止
        player.enabled = false;

        //クリアパネルを表示
        if (clearPanel != null)
        {
            clearPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ClearPanelが設定されていません");
        }
    }
}
