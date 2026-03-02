using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 경기 중 상단 정보를 관리하며, 타이머를 초 단위(예: 120)로 표시합니다.
/// </summary>
public class HUDUI : BaseUI
{
    public override UIType UIType => UIType.HUD;
    public override bool IsPopup => false;

    [Header("1. Time & Score Display")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI combinedScoreText;

    [Header("2. Blue Team List (Slots)")]
    [SerializeField] private TextMeshProUGUI blueTeamListText1;
    [SerializeField] private TextMeshProUGUI blueTeamListText2;
    
    [Header("3. Red Team List (Slots)")]
    [SerializeField] private TextMeshProUGUI redTeamListText1;
    [SerializeField] private TextMeshProUGUI redTeamListText2;

    private float updateInterval = 0.1f;
    private float nextUpdateTime = 0f;

    private void LateUpdate()
    {
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + updateInterval;

        RefreshHUD();
    }

    public override void Refresh() => RefreshHUD();

    private void RefreshHUD()
    {
        if (MatchManager.Instance == null) return;

        UpdateTimerAndScore();
        UpdatePlayerSlots();
    }

    private void UpdateTimerAndScore()
    {
        // [수정] 남은 시간을 초 단위 정수(예: 120)로 즉시 표시
        float remainingTime = MatchManager.Instance.GetRemainingTime();
        
        if (timeText != null)
        {
            timeText.text = Mathf.FloorToInt(remainingTime).ToString();
            
            // 10초 미만일 때 강조 색상 적용
            timeText.color = (remainingTime <= 10f) ? Color.red : Color.white;
        }

        if (combinedScoreText != null)
        {
            combinedScoreText.text = $"{MatchManager.Instance.BlueScore} : {MatchManager.Instance.RedScore}";
        }
    }

    private void UpdatePlayerSlots()
    {
        var allPlayers = Object.FindObjectsByType<Player>(FindObjectsSortMode.None).ToList();
        var bluePlayers = allPlayers.Where(p => p.TeamID == 0).ToList();
        var redPlayers = allPlayers.Where(p => p.TeamID == 1).ToList();

        SetPlayerSlotInfo(blueTeamListText1, bluePlayers.Count > 0 ? bluePlayers[0] : null);
        SetPlayerSlotInfo(blueTeamListText2, bluePlayers.Count > 1 ? bluePlayers[1] : null);

        SetPlayerSlotInfo(redTeamListText1, redPlayers.Count > 0 ? redPlayers[0] : null);
        SetPlayerSlotInfo(redTeamListText2, redPlayers.Count > 1 ? redPlayers[1] : null);
    }

    private void SetPlayerSlotInfo(TextMeshProUGUI slotText, Player player)
    {
        if (slotText == null) return;

        if (player == null)
        {
            slotText.text = string.Empty;
            return;
        }

        string statusTag = (player.HP <= 0) ? "<color=red>[OUT]</color>" : "<color=green>[ALIVE]</color>";
        slotText.text = $"{player.PlayerName} {statusTag}";
    }
}
