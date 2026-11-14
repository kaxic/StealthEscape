using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyAwarenessHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAwareness enemyAwareness;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private PlayerMovement playerMovement;


    [Header("UI")]
    [SerializeField] private GameObject awarenessGauge;
    [SerializeField] private Image awarenessFill;
    [SerializeField] private TextMeshProUGUI warningText;


    private void Awake()
    {
        if(enemyAwareness==null||enemyAI==null||playerMovement==null||
            awarenessGauge == null || awarenessFill == null || warningText == null)
        {
            Debug.LogError("EnemyAwarenessHUDの参照が不足しています。");
            enabled = false;
            return;
        }

        //初期化
        awarenessFill.fillAmount = 0f;
        SetUIVisible(false);
    }

    private void LateUpdate()
    {
        if (!playerMovement.enabled)
        {
            SetUIVisible(false);
            return;
        }


        //追跡中か警戒度MAXなら発見
        bool isDetected = enemyAI.IsChasing || enemyAwareness.IsAtMaximum;

        //追跡中は警戒度MAX
        float displayedAwareness = isDetected ? 100f : enemyAwareness.CurrentAwareness;

        //警戒度をゲージに反映
        awarenessFill.fillAmount = Mathf.Clamp01(displayedAwareness / 100f);

        //警戒度0ならUIを隠す
        bool showWarning = displayedAwareness > 0.01f;

        SetUIVisible(showWarning);

        if (!showWarning)
        {
            return;
        }

        //発見状態に応じて文字変更
        string message = isDetected ? "! DETECTED !" : "SUSPICIOUS";

        if (warningText.text != message)
        {
            warningText.text = message;
        }


        //警戒中は黄色、発見したら赤
        Color warningColor = isDetected ? Color.red : Color.yellow;

        warningText.color = warningColor;
        awarenessFill.color = warningColor;
    }


    //ゲージと警戒文字をまとめて切り替える
    private void SetUIVisible(bool visible)
    {
        if(awarenessGauge.activeSelf!=visible)
        {
            awarenessGauge.SetActive(visible);
        }

        if (warningText.gameObject.activeSelf != visible)
        {
            warningText.gameObject.SetActive(visible);
        }
    }
}
