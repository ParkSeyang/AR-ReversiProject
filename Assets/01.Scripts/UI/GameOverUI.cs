using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 경기 종료 후 최종 승리 팀을 표시하고 로비로 이동하는 기능을 관리합니다.
/// </summary>
public class GameOverUI : BaseUI
{
    public override UIType UIType => UIType.GameOver;
    public override bool IsPopup => true;

    [Header("Result Components")]
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button btnBackToLobby;

    protected override void Awake()
    {
        base.Awake();
        btnBackToLobby?.onClick.AddListener(OnClickBackToLobby);
    }

    /// <summary>
    /// 최종 승리 팀 정보를 UI에 세팅합니다.
    /// </summary>
    public void SetResult(int winnerTeam)
    {
        string teamName = (winnerTeam == 0) ? "BLUE TEAM" : "RED TEAM";
        Color teamColor = (winnerTeam == 0) ? Color.cyan : Color.red;

        if (winnerText != null)
        {
            winnerText.text = $"{teamName}\nWINNER!";
            winnerText.color = teamColor;
        }
    }

    private void OnClickBackToLobby()
    {
        if (GameSceneManager.IsInitialized == false || Player.Instance == null) return;

        // 서버(Host) 권한으로 씬 이동 요청
        var runner = Player.Instance.Runner;
        if (runner != null && runner.IsServer == true)
        {
            GameSceneManager.Instance.ReturnToLobby(runner);
        }
    }
}
