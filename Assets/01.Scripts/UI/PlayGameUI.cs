using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// 대기방 UI를 관리하며, 서버에 저장된 닉네임과 준비 상태를 실시간 동기화합니다.
/// </summary>
public class PlayGameUI : BaseUI
{
    // [변경] 독립된 타입을 사용하여 HUDUI와 충돌을 방지합니다.
    public override UIType UIType => UIType.Matchmaking; 
    public override bool IsPopup => false;

    [Header("1. Session Room Panel")]
    [SerializeField] private GameObject sessionRoomPanel;
    [SerializeField] private TMP_InputField roomCodeInputField;
    [SerializeField] private Button btnHost;
    [SerializeField] private Button btnJoin;
    [SerializeField] private Button btnExitToLobby; 
    [SerializeField] private TMP_Text sessionStatusText;

    [Header("2. Game Room Panel")]
    [SerializeField] private GameObject gameRoomPanel;
    [SerializeField] private TMP_Text sessionNumberText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button btnExitRoom;

    [Header("Team Blue Slots")]
    [SerializeField] private TMP_Text bluePlayerText1;
    [SerializeField] private Image blueReadyIndicator1;
    [SerializeField] private TMP_Text bluePlayerText2;
    [SerializeField] private Image blueReadyIndicator2;

    [Header("Team Red Slots")]
    [SerializeField] private TMP_Text redPlayerText1;
    [SerializeField] private Image redReadyIndicator1;
    [SerializeField] private TMP_Text redPlayerText2;
    [SerializeField] private Image redReadyIndicator2;

    [Header("Ready System")]
    [SerializeField] private Button btnReady;
    [SerializeField] private TMP_Text btnReadyText; 

    [Header("3. Countdown & Start Effect")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText; 

    private NetworkRunner runner;
    private bool isReadyLocally = false;
    private bool isSequenceStarted = false;

    private static readonly Color ReadyColor = Color.green;
    private static readonly Color WaitColor = Color.white;

    protected override void Awake()
    {
        base.Awake();
        
        if (btnHost != null) btnHost.onClick.AddListener(OnClickHost);
        if (btnJoin != null) btnJoin.onClick.AddListener(OnClickJoin);
        if (btnExitToLobby != null) btnExitToLobby.onClick.AddListener(OnClickExitToLobby);

        if (btnReady != null) btnReady.onClick.AddListener(OnToggleReady);
        if (btnExitRoom != null) btnExitRoom.onClick.AddListener(OnClickExitRoom);
    }

    private void Start() => InitializeUI();

    private void InitializeUI()
    {
        if (sessionRoomPanel != null) sessionRoomPanel.SetActive(true);
        if (gameRoomPanel != null) gameRoomPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        
        isReadyLocally = false;
        UpdateReadyButtonUI();
    }

    private void Update()
    {
        if (runner == null) runner = FindAnyObjectByType<NetworkRunner>();
        if (runner == null || runner.IsRunning == false) return;

        // 1. 방 접속 시 패널 전환 및 내 정보(이름/캐릭터) 서버에 즉시 보고
        if (sessionRoomPanel != null && sessionRoomPanel.activeSelf == true)
        {
            SwitchToGameRoom();
        }

        // 2. 서버 데이터(LobbyManager)를 기반으로 실시간 UI 갱신
        RefreshPlayerListAndStates();
        CheckStartSequence();
    }

    private void SwitchToGameRoom()
    {
        sessionRoomPanel.SetActive(false);
        if (gameRoomPanel != null) gameRoomPanel.SetActive(true);

        if (sessionNumberText != null) sessionNumberText.text = runner.SessionInfo.Name;

        // [중요] 접속하자마자 내 닉네임과 캐릭터 정보를 서버 LobbyManager에 등록 요청
        if (LobbyManager.Instance != null)
        {
            string myName = PlayerPrefs.GetString("LocalPlayerName", "Guest");
            int myCharIndex = PlayerPrefs.GetInt("SelectedCharIndex", 0);
            LobbyManager.Instance.RPC_SetCharacterAndName(runner.LocalPlayer, myCharIndex, myName);
        }
    }

    private void OnToggleReady()
    {
        if (runner == null) return;

        isReadyLocally = isReadyLocally == false;
        UpdateReadyButtonUI();

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.RPC_SetReady(runner.LocalPlayer, isReadyLocally);
        }
    }

    private void UpdateReadyButtonUI()
    {
        if (btnReadyText != null)
        {
            btnReadyText.text = isReadyLocally ? "CANCEL" : "READY";
            btnReadyText.color = Color.black;
        }
    }

    private void OnClickExitRoom()
    {
        if (NetworkRunnerHandler.Instance != null) NetworkRunnerHandler.Instance.Shutdown();
        InitializeUI();
    }

    private void OnClickExitToLobby()
    {
        // [수정] MainLoby 씬으로 이동하도록 명시적 호출
        if (GameSceneManager.IsInitialized == true) 
        {
            GameSceneManager.Instance.ReturnToLobbyManual();
        }
    }

    private void RefreshPlayerListAndStates()
    {
        // [수정] LobbyManager가 존재하더라도 네트워크 소환 전(Object.IsValid == false)이면 중단하여 에러 방지
        if (LobbyManager.Instance == null || LobbyManager.Instance.Object == null || LobbyManager.Instance.Object.IsValid == false) return;

        ClearAllSlots();

        int totalPlayerCount = LobbyManager.Instance.PlayerCount;
        UpdatePlayerCountUI(totalPlayerCount);

        int blueCount = 0;
        int redCount = 0;

        foreach (var kvp in LobbyManager.Instance.PlayerDataDic)
        {
            var data = kvp.Value;
            string playerName = data.Nickname.ToString();

            if (data.TeamID == 0) // Blue Team
            {
                UpdateSlot(blueCount == 0 ? bluePlayerText1 : bluePlayerText2, 
                           blueCount == 0 ? blueReadyIndicator1 : blueReadyIndicator2, 
                           playerName, data.IsReady);
                blueCount++;
            }
            else // Red Team
            {
                UpdateSlot(redCount == 0 ? redPlayerText1 : redPlayerText2, 
                           redCount == 0 ? redReadyIndicator1 : redReadyIndicator2, 
                           playerName, data.IsReady);
                redCount++;
            }
        }
    }

    private void UpdateSlot(TMP_Text nameText, Image indicator, string nickname, bool isReady)
    {
        // [수정] Joining... 상태가 아닐 때만 실제 플레이어로 간주
        bool hasPlayer = string.IsNullOrEmpty(nickname) == false && nickname != "Joining...";

        if (nameText != null)
        {
            nameText.gameObject.SetActive(hasPlayer);
            if (hasPlayer == true) nameText.text = nickname;
        }

        if (indicator != null)
        {
            indicator.gameObject.SetActive(hasPlayer);
            if (hasPlayer == true) indicator.color = isReady ? ReadyColor : WaitColor;
        }
    }

    private void ClearAllSlots()
    {
        SetSlotActive(bluePlayerText1, blueReadyIndicator1, false);
        SetSlotActive(bluePlayerText2, blueReadyIndicator2, false);
        SetSlotActive(redPlayerText1, redReadyIndicator1, false);
        SetSlotActive(redPlayerText2, redReadyIndicator2, false);
    }

    private void SetSlotActive(TMP_Text nameText, Image indicator, bool isActive)
    {
        if (nameText != null) nameText.gameObject.SetActive(isActive);
        if (indicator != null) indicator.gameObject.SetActive(isActive);
    }

    private void UpdatePlayerCountUI(int count)
    {
        if (playerCountText == null) return;
        playerCountText.text = $"{count} / 4";
        playerCountText.color = (count >= 4) ? Color.red : new Color(0.5f, 1f, 0.5f, 1f);
    }

    #region Session Room Logic
    private void OnClickHost()
    {
        if (NetworkRunnerHandler.Instance == null) return;

        string roomName = string.IsNullOrEmpty(roomCodeInputField.text) 
                          ? $"ROOM_{Random.Range(1000, 9999)}" 
                          : roomCodeInputField.text;

        Debug.Log($"[UI] Hosting Session: {roomName}");
        NetworkRunnerHandler.Instance.StartGame(GameMode.Host, roomName);
    }

    private void OnClickJoin()
    {
        if (NetworkRunnerHandler.Instance == null) return;

        string roomName = roomCodeInputField.text;
        if (string.IsNullOrEmpty(roomName) == true)
        {
            Debug.LogWarning("[UI] Please enter a Room Code to join.");
            return;
        }

        Debug.Log($"[UI] Joining Session: {roomName}");
        NetworkRunnerHandler.Instance.StartGame(GameMode.Client, roomName);
    }
    #endregion

    private void CheckStartSequence()
    {
        if (LobbyManager.Instance == null || isSequenceStarted == true) return;
        
        // [수정] 안전한 Getter 사용
        if (LobbyManager.Instance.SafeIsMatchStarting == true)
        {
            isSequenceStarted = true;
            StartCoroutine(FullCountdownSequence());
        }
    }

    private IEnumerator FullCountdownSequence()
    {
        if (gameRoomPanel != null) gameRoomPanel.SetActive(false);
        // 카운트다운 시작 시 HUD와 게임오버 등 다른 UI를 끄고 카운트다운만 노출
        if (countdownPanel != null) countdownPanel.SetActive(true);

        yield return StartCoroutine(AnimateTextFlyIn("3", Color.white));
        yield return StartCoroutine(AnimateTextFlyIn("2", Color.white));
        yield return StartCoroutine(AnimateTextFlyIn("1", Color.white));
        yield return StartCoroutine(AnimateTextFlyIn("LET'S BATTLE!", Color.yellow));

        if (countdownPanel != null) countdownPanel.SetActive(false);
    }

    private IEnumerator AnimateTextFlyIn(string content, Color textColor)
    {
        if (countdownText == null) yield break;

        countdownText.text = content;
        countdownText.color = textColor;

        float duration = 0.5f;
        float timer = 0f;

        // 초기 상태: 크게 시작 및 투명
        countdownText.transform.localScale = Vector3.one * 5.0f;
        Color startColor = textColor;
        startColor.a = 0f;
        countdownText.color = startColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            // 1. 크기 보간 (5.0 -> 1.0)
            countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 5.0f, Vector3.one, progress);

            // 2. 투명도 보간 (0.0 -> 1.0)
            Color currentColor = textColor;
            currentColor.a = Mathf.Lerp(0f, 1.0f, progress);
            countdownText.color = currentColor;

            yield return null;
        }

        // 잠시 유지 (가독성)
        yield return new WaitForSeconds(0.3f);
    }
}
