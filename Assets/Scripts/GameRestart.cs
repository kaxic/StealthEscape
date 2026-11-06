using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameRestart : MonoBehaviour
{
    private void Update()
    {
        //キーボードが存在しない場合は処理を行わない
        if (Keyboard.current == null) 
        {
            return;
        }

        //Rキーが押されたらゲームを再起動
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    private void RestartGame()
    {
        //現在のシーンを取得
        Scene currentScene = SceneManager.GetActiveScene();

        //現在のシーンを再読み込み
        SceneManager.LoadScene(currentScene.name);
    }
}
