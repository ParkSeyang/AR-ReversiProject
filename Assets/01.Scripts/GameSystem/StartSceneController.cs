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
    /// 확인 버튼 클릭 시 Firebase 익명 로그인을 수행하고 닉네임을 등록합니다.
    /// </summary>
    public async void OnOkButtonClick()
    {
        if (isTransitioning == true) return;

        if (nickNameInputField != null && string.IsNullOrWhiteSpace(nickNameInputField.text) == false)
        {
            string enteredName = nickNameInputField.text;

            // 1. AuthManager 초기화 대기 (혹시 아직 초기화 중일 경우)
            if (AuthManager.Instance.IsFirebaseInitialized == false)
            {
                Debug.LogWarning("[Start] AuthManager is still initializing...");
                return;
            }

            // 2. Firebase 익명 로그인 시도
            bool isLoginSuccess = await AuthManager.Instance.SignInAnonymously();
            
            if (isLoginSuccess == true)
            {
                // 3. Firebase DisplayName 업데이트
                await AuthManager.Instance.UpdateDisplayName(enteredName);
                
                // 4. 기존 로직과의 호환성을 위해 로컬 저장도 병행
                PlayerPrefs.SetString(NICKNAME_SAVE_KEY, enteredName);
                PlayerPrefs.Save();
                
                StartCoroutine(FadeOutAndMoveToLobby());
            }
            else
            {
                Debug.LogError("[Start] Firebase Login Failed.");
            }
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
