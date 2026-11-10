using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class EnemyStateDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAI enemyAI;

    private TextMeshProUGUI stateText;


    private void Awake()
    {
        stateText = GetComponent<TextMeshProUGUI>();
    }

    private void LateUpdate()
    {
        if(enemyAI == null)
        {
            return;
        }

        //現在の状態を取得
        string stateName = enemyAI.CurrentStateName;

        //表示用文字列
        string displayText = "STATE : " + stateName.ToUpperInvariant();

        //状態が変わった時だけ文字を更新
        if(stateText.text != displayText )
        {
            stateText.text = displayText;
        }
    }
}
