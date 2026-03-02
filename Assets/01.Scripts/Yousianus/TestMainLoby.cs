using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;

namespace Youstianus
{
    public class TestMainLoby : MonoBehaviour
    {
        [Header("User Information")]
        [SerializeField] private TMP_Text nickNameText;

        [Header("Character Selection")]
        [SerializeField] private GameObject yourSelectPrefab;   
        [SerializeField] private Transform[] charSelectPositions; 
        [SerializeField] private Button[] charSelectButtons;    
        [SerializeField] private Image[] charImages;            

        [Header("Room Management")]
        [SerializeField] private GameObject roomCreatePanel;      
        [SerializeField] private Button openRoomPanelButton;      
        [SerializeField] private TMP_Text openRoomPanelButtonText; 
        [SerializeField] private Button closeRoomPanelButton;     
        [SerializeField] private TMP_InputField roomNameInputField; 
        [SerializeField] private Button createButton;             
        [SerializeField] private Button joinButton;               
        [SerializeField] private TMP_Text informationText;        

        [Header("Ready System")]
        [SerializeField] private GameObject loadingImage;      
        [SerializeField] private float rotationSpeed = 200f;   
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Waiting UI")]
        [SerializeField] private RectTransform waitingPeoplePanel; 
        [SerializeField] private TMP_Text waitingPeopleText;       
        [SerializeField] private float panelMoveDuration = 0.5f;   
        [SerializeField] private float panelOffY = -500f;          

        [Header("Matchmaking Success UI")]
        [SerializeField] private GameObject matchmakingSuccessPanel; 
        [SerializeField] private RectTransform successTextTransform; 
        [SerializeField] private float textFlyInDuration = 0.8f;     
        [SerializeField] private float textFlyInOffset = 1000f;     

        [Header("Fade UI")]
        [SerializeField] private Image fadeImage;                   
        [SerializeField] private float fadeOutDuration = 1.0f;       

        [Header("Game Result UI")]
        [SerializeField] private GameObject ingameResultPanel;    // "IngameResult" 패널
        [SerializeField] private TMP_Text winnerTeamText;         // 승리팀 표기용
        [SerializeField] private TMP_Text winnerName1Text;        // 승리자1
        [SerializeField] private TMP_Text winnerName2Text;        // 승리자2
        [SerializeField] private Button resultCloseButton;        // 결과창 닫기 버튼

        private GameObject instantiatedIndicator; 
        private bool isReady = false;
        private int currentSelectedIndex = -1;
        private Coroutine colorFadeCoroutine;
        private Coroutine panelMoveCoroutine;
        private Coroutine matchSuccessSequence;

        private Vector2 panelTargetPos;
        private readonly Color dimmedColor = new Color(120f / 255f, 120f / 255f, 120f / 255f, 1f);

        private void Awake()
        {
            if (waitingPeoplePanel != null)
            {
                panelTargetPos = waitingPeoplePanel.anchoredPosition;
                waitingPeoplePanel.anchoredPosition = new Vector2(panelTargetPos.x, panelOffY);
                waitingPeoplePanel.gameObject.SetActive(false);
            }

            if (matchmakingSuccessPanel != null) matchmakingSuccessPanel.SetActive(false);
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
                fadeImage.color = new Color(0, 0, 0, 0);
                fadeImage.raycastTarget = false;
            }

            // 결과창 초기화
            if (ingameResultPanel != null) ingameResultPanel.SetActive(false);
            if (resultCloseButton != null) resultCloseButton.onClick.AddListener(() => ingameResultPanel.SetActive(false));
            
            InitializeUI();
            LoadNickName();
        }

        private void Start()
        {
            // 게임 결과가 있다면 표시
            CheckAndShowResult();
        }

        private void CheckAndShowResult()
        {
            if (NetworkRunnerHandler.LastResult != null)
            {
                var result = NetworkRunnerHandler.LastResult;
                if (ingameResultPanel != null)
                {
                    ingameResultPanel.SetActive(true);
                    
                    if (winnerTeamText != null)
                        winnerTeamText.text = result.WinnerTeam == Youstianus.ETeam.Blue ? "BlueTeam Win!" : "RedTeam Win!";

                    // 승리자 이름 채우기 (2명)
                    if (winnerName1Text != null) 
                        winnerName1Text.text = result.WinnerNames.Count > 0 ? result.WinnerNames[0] : "";
                    if (winnerName2Text != null) 
                        winnerName2Text.text = result.WinnerNames.Count > 1 ? result.WinnerNames[1] : "";
                }

                // 소모한 데이터는 초기화
                NetworkRunnerHandler.LastResult = null;
            }
        }

        private void OnEnable()
        {
            if (NetworkRunnerHandler.Instance != null)
            {
                NetworkRunnerHandler.Instance.OnShutdownEvent += ResetLobbyUI;
                NetworkRunnerHandler.Instance.OnPlayerCountChanged += CheckMatchSuccess;
            }
        }

        private void OnDisable()
        {
            if (NetworkRunnerHandler.Instance != null)
            {
                NetworkRunnerHandler.Instance.OnShutdownEvent -= ResetLobbyUI;
                NetworkRunnerHandler.Instance.OnPlayerCountChanged -= CheckMatchSuccess;
            }
        }
private void CheckMatchSuccess(int count)
{
    UpdatePlayerCount(count);

    // [복구] 실제 2명이 모였을 때만 성공 연출 시작
    if (count >= 2 && isReady && matchSuccessSequence == null)
    {
        matchSuccessSequence = StartCoroutine(MatchSuccessSequenceRoutine());
    }
}


        private void UpdatePlayerCount(int count)
        {
            if (waitingPeopleText != null)
                waitingPeopleText.text = $"대기중.. ({count} / 2)";
        }

        private IEnumerator MatchSuccessSequenceRoutine()
        {
            if (fadeImage != null) fadeImage.raycastTarget = true;

            if (matchmakingSuccessPanel != null)
            {
                matchmakingSuccessPanel.SetActive(true);
                if (successTextTransform != null)
                {
                    Vector2 targetPos = Vector2.zero;
                    Vector2 startPos = new Vector2(textFlyInOffset, 0);
                    successTextTransform.anchoredPosition = startPos;

                    float timer = 0f;
                    while (timer < textFlyInDuration)
                    {
                        timer += Time.deltaTime;
                        float t = Mathf.SmoothStep(0, 1, timer / textFlyInDuration);
                        successTextTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                        yield return null;
                    }
                }
            }

            float remainingWait = 3.0f - textFlyInDuration - fadeOutDuration;
            if (remainingWait > 0) yield return new WaitForSeconds(remainingWait);

            if (fadeImage != null)
            {
                float timer = 0f;
                while (timer < fadeOutDuration)
                {
                    timer += Time.deltaTime;
                    float alpha = Mathf.Clamp01(timer / fadeOutDuration);
                    fadeImage.color = new Color(0, 0, 0, alpha);
                    yield return null;
                }
                fadeImage.color = new Color(0, 0, 0, 1);
            }

            NetworkRunnerHandler.Instance.RequestSceneChange("INgameScean");
        }

        private void InitializeUI()
        {
            if (yourSelectPrefab != null)
            {
                instantiatedIndicator = Instantiate(yourSelectPrefab);
                instantiatedIndicator.SetActive(false);
            }

            if (loadingImage != null) loadingImage.SetActive(false);
            if (informationText != null) informationText.text = string.Empty;
            if (waitingPeopleText != null) waitingPeopleText.text = "대기중.. (1 / 4)";
            
            if (roomCreatePanel != null) roomCreatePanel.SetActive(false);

            if (openRoomPanelButton != null)
            {
                openRoomPanelButton.onClick.RemoveAllListeners();
                openRoomPanelButton.onClick.AddListener(OnMainButtonClick);
            }
            if (closeRoomPanelButton != null)
            {
                closeRoomPanelButton.onClick.RemoveAllListeners();
                closeRoomPanelButton.onClick.AddListener(() => roomCreatePanel.SetActive(false));
            }
            if (createButton != null)
            {
                createButton.onClick.RemoveAllListeners();
                createButton.onClick.AddListener(() => OnStartNetworkGame(GameMode.Host));
            }
            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(() => OnStartNetworkGame(GameMode.Client));
            }

            UpdateMainButtonText();
        }

        private void OnMainButtonClick()
        {
            if (isReady)
            {
                OnCancelMatchmaking();
                return;
            }

            if (currentSelectedIndex == -1)
            {
                SetInformation("캐릭터를 먼저 선택해주세요!");
                return;
            }

            if (roomCreatePanel != null)
            {
                roomCreatePanel.SetActive(true);
                SetInformation("방 이름을 입력하고 Join/Create를 눌러주세요.");
            }
        }

        private void OnCancelMatchmaking()
        {
            SetInformation("매칭 취소 중...");
            if (matchSuccessSequence != null)
            {
                StopCoroutine(matchSuccessSequence);
                matchSuccessSequence = null;
            }
            NetworkRunnerHandler.Instance.Shutdown();
            ResetLobbyUI();
        }

        private void ResetLobbyUI()
        {
            isReady = false;
            if (loadingImage != null) loadingImage.SetActive(false);
            if (roomCreatePanel != null) roomCreatePanel.SetActive(false);
            if (matchmakingSuccessPanel != null) matchmakingSuccessPanel.SetActive(false);
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, 0);
                fadeImage.raycastTarget = false;
            }

            if (waitingPeoplePanel != null && waitingPeoplePanel.gameObject.activeSelf)
            {
                if (panelMoveCoroutine != null) StopCoroutine(panelMoveCoroutine);
                panelMoveCoroutine = StartCoroutine(AnimatePanel(false));
            }
            
            SetUIInteractable(true);
            UpdateMainButtonText();
            foreach (var btn in charSelectButtons) if (btn != null) btn.interactable = true;
            if (colorFadeCoroutine != null) StopCoroutine(colorFadeCoroutine);
            colorFadeCoroutine = StartCoroutine(FadeCharacterColorsRoutine());
            SetInformation("매칭이 취소되었습니다.");
        }

        private void UpdateMainButtonText()
        {
            if (openRoomPanelButtonText != null)
                openRoomPanelButtonText.text = isReady ? "Cancel" : "Join";
        }

        private void LoadNickName()
        {
            if (nickNameText != null)
            {
                string savedNick = PlayerPrefs.GetString("PlayerNickName", "Guest");
                nickNameText.text = $"{savedNick}";
            }
        }

        private void Update()
        {
            if (isReady && loadingImage != null)
                loadingImage.transform.Rotate(Vector3.back * rotationSpeed * Time.deltaTime);
        }

        public void OnCharacterSelect(int index)
        {
            if (isReady || index < 0 || index >= charSelectPositions.Length) return;
            currentSelectedIndex = index;
            if (NetworkRunnerHandler.Instance != null)
                NetworkRunnerHandler.Instance.SelectedCharacterIndex = index;

            if (instantiatedIndicator != null)
            {
                instantiatedIndicator.SetActive(true);
                instantiatedIndicator.transform.SetParent(charSelectPositions[index]);
                instantiatedIndicator.transform.localPosition = Vector3.zero;
                instantiatedIndicator.transform.localScale = Vector3.one;
                var rt = instantiatedIndicator.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = Vector2.zero;
            }
        }

        private async void OnStartNetworkGame(GameMode mode)
        {
            if (currentSelectedIndex == -1)
            {
                SetInformation("캐릭터를 먼저 선택해주세요!");
                return;
            }

            string roomName = roomNameInputField.text;
            if (string.IsNullOrWhiteSpace(roomName))
            {
                SetInformation("방 이름을 입력해주세요.");
                return;
            }

            SetInformation(mode == GameMode.Host ? "방 생성 중..." : "방 참가 중...");
            SetUIInteractable(false);

            bool success = await NetworkRunnerHandler.Instance.StartGame(mode, roomName);

            if (success)
            {
                SetInformation("세션 접속 완료!");
                EnterReadyState();
            }
            else
            {
                SetInformation(mode == GameMode.Client ? "올바르지않는 방입니다." : "방 생성에 실패했습니다.");
                SetUIInteractable(true);
            }
        }

        private void EnterReadyState()
        {
            isReady = true;
            if (loadingImage != null) loadingImage.SetActive(true);
            if (roomCreatePanel != null) roomCreatePanel.SetActive(false);
            UpdatePlayerCount(1);
            if (waitingPeoplePanel != null)
            {
                waitingPeoplePanel.gameObject.SetActive(true);
                if (panelMoveCoroutine != null) StopCoroutine(panelMoveCoroutine);
                panelMoveCoroutine = StartCoroutine(AnimatePanel(true));
            }
            UpdateMainButtonText();
            foreach (var btn in charSelectButtons) if (btn != null) btn.interactable = false;
            if (colorFadeCoroutine != null) StopCoroutine(colorFadeCoroutine);
            colorFadeCoroutine = StartCoroutine(FadeCharacterColorsRoutine());
        }

        private IEnumerator AnimatePanel(bool show)
        {
            float timer = 0f;
            Vector2 startPos = waitingPeoplePanel.anchoredPosition;
            Vector2 targetPos = show ? panelTargetPos : new Vector2(panelTargetPos.x, panelOffY);
            while (timer < panelMoveDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / panelMoveDuration);
                waitingPeoplePanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            waitingPeoplePanel.anchoredPosition = targetPos;
            if (!show) waitingPeoplePanel.gameObject.SetActive(false);
        }

        private void SetInformation(string message)
        {
            if (informationText != null) informationText.text = message;
            Debug.Log($"[Lobby] {message}");
        }

        private void SetUIInteractable(bool interactable)
        {
            if (createButton != null) createButton.interactable = interactable;
            if (joinButton != null) joinButton.interactable = interactable;
            if (roomNameInputField != null) roomNameInputField.interactable = interactable;
        }

        private IEnumerator FadeCharacterColorsRoutine()
        {
            float timer = 0f;
            Color[] startColors = new Color[charImages.Length];
            Color[] targetColors = new Color[charImages.Length];
            for (int i = 0; i < charImages.Length; i++)
            {
                if (charImages[i] == null) continue;
                startColors[i] = charImages[i].color;
                targetColors[i] = isReady ? (i == currentSelectedIndex ? Color.white : dimmedColor) : Color.white;
            }
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeDuration;
                for (int i = 0; i < charImages.Length; i++)
                {
                    if (charImages[i] != null) charImages[i].color = Color.Lerp(startColors[i], targetColors[i], t);
                }
                yield return null;
            }
        }
    }
}
