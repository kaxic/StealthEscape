using System;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerFootsteps playerFootsteps;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    //敵が足音を聞いた時他のスクリプトに通知
    public event Action<Vector3> FootstepHeard;


    private void OnEnable()
    {
        //Playerの足音イベントを購読
        if(playerFootsteps != null)
        {
            playerFootsteps.FootstepOccurred += OnFootstepOccurred;
        }
    }

    private void Start()
    {
        if (playerFootsteps == null)
        {
            Debug.LogWarning("EnemyHearingにPlayerFootstepsが設定されていません。");
        }
    }

    private void OnDisable()
    {
        if (playerFootsteps != null)
        {
            playerFootsteps.FootstepOccurred -= OnFootstepOccurred;
        }
    }


    private void OnFootstepOccurred(Vector3 soundPosition, float hearingRadius)
    {
        if (hearingRadius <= 0f)
        {
            return;
        }


        //足音からEnemyまでの方向を計算
        Vector3 toSound = soundPosition - transform.position;
        toSound.y = 0f;

        //距離の2乗と半径の2乗を比較
        float sqrDistance = toSound.sqrMagnitude;
        float sqrRadius = hearingRadius * hearingRadius;

        //範囲外なら何もしない
        if (sqrDistance > sqrRadius)
        {
            return;
        }


        //足音を検知
        if (showDebugLog)
        {
            float distance = toSound.magnitude;

            Debug.Log("FOOTSTEP HEARD! " + 
                "Distance = " + distance.ToString("F2") + " Range = " + hearingRadius + " Position = " + soundPosition);
        }

        FootstepHeard?.Invoke(soundPosition);
    }
}
