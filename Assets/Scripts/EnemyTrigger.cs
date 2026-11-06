using UnityEngine;

public class EnemyTrigger : MonoBehaviour
{
    //
    [SerializeField] private GameObject gameOverPanel;

    //ゲームオーバーになったかどうかを判定
    private bool isGameOver = false;

    private void Start()
    {
        //ゲーム開始時はゲームオーバーパネルを非表示
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //すでにゲームオーバーになっている場合は処理を行わない
        if (isGameOver)
        {
            return;
        }

        //接触したオブジェクトがプレイヤーかどうかを判定
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player == null || !player.enabled)
        {
            return;
        }

        //ゲームオーバーにする
        isGameOver = true;

        Debug.Log("GAME OVER!");

        //プレイヤーの移動を停止
        player.enabled = false;

        //ゲームオーバー画面を表示
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("GameOverPanelが設定されていません");
        }
    }
}
