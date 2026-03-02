using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections;

/// <summary>
/// 게임 시작 씬의 전체 흐름을 관리하며, Firebase Auth와 연동하여 DisplayName을 설정합니다.
/// </summary>
public class StartSceneController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject nickNameInputPanel;
    [SerializeField] private GameObject gameExitPanel;

    [Header("UI Objects")]
    [SerializeField] private GameObject informationText;
    [SerializeField] private CanvasGroup fadeImageCanvasGroup;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nickNameInputField;

    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float blinkSpeed = 2.0f;

    private const int LIMIT_KOREAN = 8;
    private const int LIMIT_ENGLISH = 13;
    private const string NICKNAME_SAVE_KEY = "LocalPlayerName";

    private bool isExiting = false;
    private bool isTransitioning = false;

    private void Awake()
    {
        SetPanelActive(nickNameInputPanel, false);
        SetPanelActive(gameExitPanel, false);
        InitializeFadeImage();

        if (nickNameInputField != null)
        {
            nickNameInputField.onValueChanged.AddListener(OnNickNameValueChanged);
        }
    }

    private void Start() => StartCoroutine(FadeInRoutine());

    private void Update()
    {
        if (isExiting == true || isTransitioning == true) return;
        HandleGlobalInput();
        HandleInformationBlink();
    }

    private void InitializeFadeImage()
    {
        if (fadeImageCanvasGroup != null)
        {
            fadeImageCanvasGroup.gameObject.SetActive(true);
            fadeImageCanvasGroup.alpha = 1.0f;
            fadeImageCanvasGroup.blocksRaycasts = true;
            fadeImageCanvasGroup.transform.SetAsLastSibling();
        }
    }

    private void HandleGlobalInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleExitPanel();
            return;
        }

        if (Input.anyKeyDown == true && IsAnyPanelActive() == false)
        {
            OpenNickNamePanel();
        }
    }

    private void HandleInformationBlink()
    {
        if (informationText == null) return;

        if (IsAnyPanelActive() == true)
        {
            informationText.SetActive(false);
        }
        else
        {
            bool isVisible = Mathf.Repeat(Time.time * blinkSpeed, 1.0f) < 0.5f;
            informationText.SetActive(isVisible);
        }
    }

    private void OnNickNameValueChanged(string input)
    {
        if (nickNameInputField == null) return;

        bool hasKorean = Regex.IsMatch(input, @"[가-힣ㄱ-ㅎㅏ-ㅣ]");
        int currentLimit = hasKorean ? LIMIT_KOREAN : LIMIT_ENGLISH;
        nickNameInputField.characterLimit = currentLimit;

        if (input.Length > currentLimit)
        {
            nickNameInputField.text = input.Substring(0, currentLimit);
            nickNameInputField.caretPosition = currentLimit;
        }
    }

    private bool IsAnyPanelActive() => (nickNameInputPanel != null && nickNameInputPanel.activeSelf == true) || 
                                       (gameExitPanel != null && gameExitPanel.activeSelf == true);

    private void OpenNickNamePanel()
    {
        if (nickNameInputPanel != null)
        {
            SetPanelActive(nickNameInputPanel, true);
            nickNameInputPanel.transform.SetAsLastSibling();
            
            if (nickNameInputField != null)
            {
                nickNameInputField.text = string.Empty;
                nickNameInputField.characterLimit = LIMIT_ENGLISH;
                nickNameInputField.ActivateInputField();
            }
        }
    }

    private void ToggleExitPanel()
    {
        if (gameExitPanel == null) return;
        SetPanelActive(nickNameInputPanel, false);

        bool nextState = gameExitPanel.activeSelf == false;
        SetPanelActive(gameExitPanel, nextState);
        if (nextState == true) gameExitPanel.transform.SetAsLastSibling();
    }

    private void SetPanelActive(GameObject panel, bool isActive) => panel?.SetActive(isActive);

    #region Firebase Auth & Transition

    /// <summary>
    /// 확인 버튼 클릭 시 로컬 닉네임을 먼저 저장하고, Firebase 인증은 백그라운드에서 시도하며 즉시 로비로 이동합니다.
    /// </summary>
    public async void OnOkButtonClick()
    {
        if (isTransitioning == true) return;

        if (nickNameInputField != null && string.IsNullOrWhiteSpace(nickNameInputField.text) == false)
        {
            string enteredName = nickNameInputField.text;

            // 1. 로컬에 즉시 저장 (우회 로직의 핵심: Firebase 없이도 게임 가능하게 함)
            PlayerPrefs.SetString(NICKNAME_SAVE_KEY, enteredName);
            PlayerPrefs.Save();

            // 2. Firebase 인증은 시도만 하고, 성공 여부를 기다리지 않고 통과시킴
            if (AuthManager.Instance.IsFirebaseInitialized == true)
            {
                // 백그라운드에서 조용히 실행 (await 하지 않거나 실패해도 무시)
                _ = AuthManager.Instance.SignInAnonymously().ContinueWith(async task => {
                    if (task.Result == true) await AuthManager.Instance.UpdateDisplayName(enteredName);
                });
            }

            Debug.Log($"[Auth Bypass] Proceeding to Lobby with name: {enteredName}");
            StartCoroutine(FadeOutAndMoveToLobby());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeImageCanvasGroup == null) yield break;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImageCanvasGroup.alpha = Mathf.Lerp(1.0f, 0f, timer / fadeDuration);
            yield return null;
        }
        fadeImageCanvasGroup.alpha = 0f;
        fadeImageCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeOutAndMoveToLobby()
    {
        isTransitioning = true;
        
        if (fadeImageCanvasGroup != null)
        {
            fadeImageCanvasGroup.transform.SetAsLastSibling();
            fadeImageCanvasGroup.blocksRaycasts = true;

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeImageCanvasGroup.alpha = Mathf.Lerp(0f, 1.0f, timer / fadeDuration);
                yield return null;
            }
            fadeImageCanvasGroup.alpha = 1.0f;
        }

        yield return new WaitForSeconds(0.2f);

        if (GameSceneManager.IsInitialized == true)
        {
            GameSceneManager.Instance.MoveToLobbyFromStart();
        }
    }

    #endregion

    #region Inspector Button Events

    public void OnResetButtonClick()
    {
        if (nickNameInputField != null)
        {
            nickNameInputField.text = string.Empty;
            nickNameInputField.ActivateInputField();
        }
    }

    public void OnCloseNickNamePanel() => SetPanelActive(nickNameInputPanel, false);

    public void OnYesButtonClick()
    {
        isExiting = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnNoButtonClick() => SetPanelActive(gameExitPanel, false);

    #endregion
}
