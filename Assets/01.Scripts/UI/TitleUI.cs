using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// 메인 타이틀(로비) UI입니다.
/// 특정 방 이름과 닉네임을 입력하여 호스트 또는 클라이언트로 접속합니다.
/// </summary>
public class TitleUI : BaseUI
{
    public override UIType UIType => UIType.Title;
    public override bool IsPopup => false;

    [Header("UI Components")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField playerNameInput;
    
    [SerializeField] private Button btnHost;
    [SerializeField] private Button btnJoin;

    protected override void Start()
    {
        base.Start();

        // 1. 초기값 세팅 (PlayerPrefs에서 불러오기)
        roomNameInput.text = "DodgeballRoom";
        playerNameInput.text = PlayerPrefs.GetString("LocalPlayerName", "Player_" + Random.Range(1000, 9999));

        // 2. 버튼 이벤트 연결
        btnHost.onClick.AddListener(() => OnClickStartGame(GameMode.Host));
        btnJoin.onClick.AddListener(() => OnClickStartGame(GameMode.Client));
    }

    private async void OnClickStartGame(GameMode mode)
    {
        if (string.IsNullOrEmpty(roomNameInput.text) == true || string.IsNullOrEmpty(playerNameInput.text) == true)
        {
            UIManager.Instance.ShowWarning("방 이름과 닉네임을 모두 입력해주세요!");
            return;
        }

        // 1. 닉네임 저장
        PlayerPrefs.SetString("LocalPlayerName", playerNameInput.text);
        PlayerPrefs.Save();

        // 2. 버튼 비활성화 및 UI 모드 전환
        btnHost.interactable = false;
        btnJoin.interactable = false;

        // 씬 전환 없이 즉시 게임 UI로 교체
        UIManager.Instance.SetAllInGameUIActive(true);

        // 3. 네트워크 세션 접속
        await NetworkRunnerHandler.Instance.StartGame(mode, roomNameInput.text);
    }

    public override void Refresh()
    {
        base.Refresh();
        
        // 씬 전환 등으로 돌아왔을 때 버튼 복구
        if (btnHost != null) btnHost.interactable = true;
        if (btnJoin != null) btnJoin.interactable = true;
    }
}
