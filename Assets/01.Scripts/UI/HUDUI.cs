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

    [Header("UI Containers")]
    [SerializeField] private GameObject hudContainer; // [추가] HUD 전체를 담고 있는 부모 오브젝트

    private float updateInterval = 0.1f;
    private float nextUpdateTime = 0f;

    public override void Open()
    {
        base.Open();
        RefreshHUD();
    }

    private void LateUpdate()
    {
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + updateInterval;

        RefreshHUD();
    }

    public override void Refresh() => RefreshHUD();

    private void RefreshHUD()
    {
        // [수정] MatchManager가 존재하더라도 네트워크 소환 전이면 안전하게 종료
        if (MatchManager.Instance == null || MatchManager.Instance.Object == null || MatchManager.Instance.Object.IsValid == false) 
        {
            if (hudContainer != null) hudContainer.SetActive(false);
            return;
        }

        // [핵심] 안전한 Getter를 통해 현재 경기 상태를 체크합니다.
        bool isPlaying = MatchManager.Instance.SafeCurrentState == EMatchState.Playing;
        
        if (hudContainer != null)
        {
            if (hudContainer.activeSelf != isPlaying) hudContainer.SetActive(isPlaying);
        }

        // 경기 중일 때만 데이터 업데이트
        if (isPlaying == true)
        {
            UpdateTimerAndScore();
            UpdatePlayerSlots();
        }
    }

    private void UpdateTimerAndScore()
    {
        if (MatchManager.Instance == null) return;

        // [수정] 남은 시간을 초 단위 정수(예: 120)로 즉시 표시
        float remainingTime = MatchManager.Instance.GetRemainingTime();
        
        if (timeText != null)
        {
            timeText.text = Mathf.FloorToInt(remainingTime).ToString();
            timeText.color = (remainingTime <= 10f) ? Color.red : Color.white;
        }

        if (combinedScoreText != null)
        {
            // 점수 또한 MatchManager가 유효할 때만 가져옴
            int blue = MatchManager.Instance.Object.IsValid ? MatchManager.Instance.BlueScore : 0;
            int red = MatchManager.Instance.Object.IsValid ? MatchManager.Instance.RedScore : 0;
            combinedScoreText.text = $"{blue} : {red}";
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
