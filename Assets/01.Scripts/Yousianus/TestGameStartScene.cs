using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections;

namespace Youstianus
{
    public class TestGameStartScene : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject nickNameInputPanel;
        [SerializeField] private GameObject gameExitPanel;

        [Header("UI Objects")]
        [SerializeField] private GameObject informationText;
        [SerializeField] private CanvasGroup fadeImageCanvasGroup; // Fade용 검은 이미지

        [Header("Input Fields")]
        [SerializeField] private TMP_InputField nickNameInputField;

        [Header("Settings")]
        [SerializeField] private float blinkSpeed = 2.0f;
        [SerializeField] private float fadeDuration = 1.0f; // 페이드 지속 시간

        private bool isExiting = false;

        private void Awake()
        {
            ForceClosePanel(nickNameInputPanel);
            ForceClosePanel(gameExitPanel);

            // 시작 시 페이드 이미지 설정 (알파값이 0이어도 강제로 1로 만들어 시작)
            if (fadeImageCanvasGroup != null)
            {
                fadeImageCanvasGroup.gameObject.SetActive(true);
                fadeImageCanvasGroup.alpha = 1f; // 시작은 검은색
                fadeImageCanvasGroup.blocksRaycasts = true;
                fadeImageCanvasGroup.transform.SetAsLastSibling(); // 맨 앞으로 가져오기
            }

            if (nickNameInputField != null)
            {
                nickNameInputField.onValueChanged.AddListener(OnNickNameValueChanged);
            }
        }

        private void Start()
        {
            // 씬 시작 시 서서히 투명해지는 페이드 인 효과 실행
            StartCoroutine(FadeInRoutine());
        }

        private void Update()
        {
            if (isExiting) return;

            HandleInput();
            HandleInformationText();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleGameExitPanel();
                return;
            }

            if (Input.anyKeyDown && !IsAnyPanelActive())
            {
                OpenNickNamePanel();
            }
        }

        private void HandleInformationText()
        {
            if (informationText == null) return;

            if (IsAnyPanelActive())
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
            int currentLimit = hasKorean ? 8 : 13;
            nickNameInputField.characterLimit = currentLimit;

            if (input.Length > currentLimit)
            {
                nickNameInputField.text = input.Substring(0, currentLimit);
                nickNameInputField.caretPosition = currentLimit;
            }
        }

        private bool IsAnyPanelActive() => (nickNameInputPanel != null && nickNameInputPanel.activeSelf) || 
                                           (gameExitPanel != null && gameExitPanel.activeSelf);

        private void OpenNickNamePanel()
        {
            if (nickNameInputPanel != null)
            {
                nickNameInputPanel.SetActive(true);
                SetupPanelUI(nickNameInputPanel);
                
                if (nickNameInputField != null)
                {
                    nickNameInputField.text = string.Empty;
                    nickNameInputField.characterLimit = 13;
                    nickNameInputField.ActivateInputField();
                }
            }
        }

        private void ToggleGameExitPanel()
        {
            if (gameExitPanel == null) return;
            if (nickNameInputPanel != null) nickNameInputPanel.SetActive(false);

            bool nextState = !gameExitPanel.activeSelf;
            gameExitPanel.SetActive(nextState);
            if (nextState) SetupPanelUI(gameExitPanel);
        }

        private void SetupPanelUI(GameObject panel)
        {
            panel.transform.SetAsLastSibling();
            // 페이드 이미지가 있으면 항상 그 뒤에 위치하도록 관리 (페이드 중에는 이미지가 맨 위여야 함)
            if (fadeImageCanvasGroup != null && isExiting)
            {
                fadeImageCanvasGroup.transform.SetAsLastSibling();
            }
        }

        private void ForceClosePanel(GameObject panel) => panel?.SetActive(false);

        #region 페이드 효과 코루틴

        // 씬 시작 시: 검정 -> 투명
        private IEnumerator FadeInRoutine()
        {
            if (fadeImageCanvasGroup == null) yield break;

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeImageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            fadeImageCanvasGroup.alpha = 0f;
            fadeImageCanvasGroup.blocksRaycasts = false;
        }

        // 버튼 클릭 시: 투명 -> 검정 후 씬 전환
        private IEnumerator FadeOutAndLoadScene(string sceneName)
        {
            isExiting = true;
            
            if (fadeImageCanvasGroup != null)
            {
                fadeImageCanvasGroup.transform.SetAsLastSibling(); // 확실하게 맨 앞으로
                fadeImageCanvasGroup.blocksRaycasts = true;

                float timer = 0f;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    fadeImageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                    yield return null;
                }
                fadeImageCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(0.2f);
            SceneManager.LoadScene(sceneName);
        }

        #endregion

        #region 버튼 이벤트 (인스펙터 연결용)

        public void OnOkButtonClick()
        {
            if (isExiting) return;

            if (nickNameInputField != null && !string.IsNullOrWhiteSpace(nickNameInputField.text))
            {
                // 닉네임을 저장하여 다른 씬(MainLoby)에서 쓸 수 있게 함
                PlayerPrefs.SetString("PlayerNickName", nickNameInputField.text);
                PlayerPrefs.Save();
                
                StartCoroutine(FadeOutAndLoadScene("MainLoby"));
            }
        }

        public void OnResetButtonClick()
        {
            if (nickNameInputField != null)
            {
                nickNameInputField.text = string.Empty;
                nickNameInputField.ActivateInputField();
            }
        }

        public void OnCloseNickNamePanel() => ForceClosePanel(nickNameInputPanel);

        public void OnYesButtonClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnNoButtonClick() => ForceClosePanel(gameExitPanel);

        public void OnCloseExitPanel() => ForceClosePanel(gameExitPanel);

        #endregion
    }
}
