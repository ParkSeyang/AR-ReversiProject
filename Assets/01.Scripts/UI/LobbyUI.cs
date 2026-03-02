using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 메인 로비의 캐릭터 선택 및 취소 가능한 매칭 시퀀스를 관리합니다.
/// 프로젝트 코딩 표준을 엄격히 준수합니다.
/// </summary>
public class LobbyUI : BaseUI
{
    public override UIType UIType => UIType.Lobby;
    public override bool IsPopup => false;

    [Header("User Information")]
    [SerializeField] private TMP_Text nickNameText;

    [Header("Character Selection")]
    [SerializeField] private GameObject yourSelectPrefab;   
    [SerializeField] private Transform[] charSelectPositions; 
    [SerializeField] private Button[] charSelectButtons;    
    [SerializeField] private Image[] charImages;            

    [Header("Ready System")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonText;
    [SerializeField] private GameObject loadingImage;      
    [SerializeField] private float rotationSpeed = 200f;   
    [SerializeField] private float fadeDuration = 0.5f;    

    [Header("Matchmaking Success Effect")]
    [SerializeField] private GameObject successPanel; 
    [SerializeField] private RectTransform successTextTransform; 
    [SerializeField] private float textFlyInDuration = 0.8f;     
    [SerializeField] private float textFlyInOffset = 1000f;     

    [Header("Transition Effect")]
    [SerializeField] private Image fadeImage; 
    [SerializeField] private float slowFadeDuration = 1.0f;

    private GameObject instantiatedIndicator; 
    private bool isReady = false;
    private int currentSelectedIndex = -1;
    private Coroutine colorFadeCoroutine;
    private Coroutine matchSequenceCoroutine;
    private bool isTransitioning = false; 

    // 매직 넘버 제거를 위한 상수 선언
    private static readonly Color DimmedColor = new Color(0.47f, 0.47f, 0.47f, 1f);
    private const string CHARACTER_SAVE_KEY = "SelectedCharIndex";
    private const float MATCH_WAIT_TIME = 3.0f;

    protected override void Awake()
    {
        base.Awake();
        InitializeUI();
        
        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClick);
        }

        if (charSelectButtons != null)
        {
            for (int index = 0; index < charSelectButtons.Length; index++)
            {
                int captureIndex = index;
                charSelectButtons[index]?.onClick.AddListener(() => OnCharacterSelect(captureIndex));
            }
        }
    }

    private void InitializeUI()
    {
        if (nickNameText != null)
        {
            nickNameText.text = PlayerPrefs.GetString("LocalPlayerName", "Guest");
        }

        if (yourSelectPrefab != null)
        {
            instantiatedIndicator = Instantiate(yourSelectPrefab);
            instantiatedIndicator.SetActive(false);
        }

        ResetAllEffects();
        OnCharacterSelect(0);
    }

    private void ResetAllEffects()
    {
        if (loadingImage != null) loadingImage.SetActive(false);
        if (successPanel != null) successPanel.SetActive(false);
        
        if (fadeImage != null)
        {
            Color imageColor = fadeImage.color;
            imageColor.a = 0f;
            fadeImage.color = imageColor;
            fadeImage.gameObject.SetActive(false);
        }

        if (readyButtonText != null)
        {
            readyButtonText.text = "Ready?";
        }
    }

    private void Update()
    {
        if (isReady == true && loadingImage != null)
        {
            loadingImage.transform.Rotate(Vector3.back * rotationSpeed * Time.deltaTime);
        }
    }

    public void OnCharacterSelect(int index)
    {
        if (isReady == true || isTransitioning == true) return; 
        if (index < 0 || index >= charSelectPositions.Length) return;

        currentSelectedIndex = index;
        if (instantiatedIndicator != null)
        {
            instantiatedIndicator.SetActive(true);
            instantiatedIndicator.transform.SetParent(charSelectPositions[index]);
            instantiatedIndicator.transform.localPosition = Vector3.zero;
            instantiatedIndicator.transform.localScale = Vector3.one;
            
            if (instantiatedIndicator.TryGetComponent(out RectTransform rectTransform))
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
    }

    public void OnReadyButtonClick()
    {
        if (currentSelectedIndex == -1 || isTransitioning == true) return;

        isReady = isReady == false;
        UpdateReadyUI();

        if (isReady == true)
        {
            PlayerPrefs.SetInt(CHARACTER_SAVE_KEY, currentSelectedIndex);
            PlayerPrefs.Save();

            matchSequenceCoroutine = StartCoroutine(FullMatchSequence());
        }
        else
        {
            if (matchSequenceCoroutine != null)
            {
                StopCoroutine(matchSequenceCoroutine);
            }
            if (successPanel != null)
            {
                successPanel.SetActive(false);
            }
        }
    }

    private void UpdateReadyUI()
    {
        if (readyButtonText != null) 
        {
            readyButtonText.text = isReady ? "Cancel" : "Ready?";
        }
            
        if (loadingImage != null) 
        {
            loadingImage.SetActive(isReady);
        }

        foreach (var button in charSelectButtons)
        {
            if (button != null)
            {
                button.interactable = isReady == false;
            }
        }

        if (colorFadeCoroutine != null)
        {
            StopCoroutine(colorFadeCoroutine);
        }
        colorFadeCoroutine = StartCoroutine(FadeCharacterColorsRoutine());
    }

    private IEnumerator FadeCharacterColorsRoutine()
    {
        float timer = 0f;
        int imageCount = charImages.Length;
        Color[] startColors = new Color[imageCount];
        Color[] targetColors = new Color[imageCount];

        for (int index = 0; index < imageCount; index++)
        {
            if (charImages[index] == null) continue;
            startColors[index] = charImages[index].color;
            targetColors[index] = isReady ? (index == currentSelectedIndex ? Color.white : DimmedColor) : Color.white;
        }

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;
            for (int index = 0; index < imageCount; index++)
            {
                if (charImages[index] != null)
                {
                    charImages[index].color = Color.Lerp(startColors[index], targetColors[index], progress);
                }
            }
            yield return null;
        }
    }

    private IEnumerator FullMatchSequence()
    {
        yield return new WaitForSeconds(MATCH_WAIT_TIME);
        
        isTransitioning = true;
        if (loadingImage != null)
        {
            loadingImage.SetActive(false);
        }

        yield return StartCoroutine(ShowMatchSuccessRoutine());
        yield return StartCoroutine(SlowFadeOutRoutine());

        if (GameSceneManager.IsInitialized == true)
        {
            GameSceneManager.Instance.MoveToPlayGameFromLobby();
        }
    }

    private IEnumerator ShowMatchSuccessRoutine()
    {
        if (successPanel == null || successTextTransform == null) yield break;

        successPanel.SetActive(true);
        Vector2 targetPosition = Vector2.zero;
        Vector2 startPosition = new Vector2(textFlyInOffset, 0);
        successTextTransform.anchoredPosition = startPosition;

        float timer = 0f;
        while (timer < textFlyInDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, timer / textFlyInDuration);
            successTextTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }
        successTextTransform.anchoredPosition = targetPosition;
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator SlowFadeOutRoutine()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color imageColor = fadeImage.color;

        while (timer < slowFadeDuration)
        {
            timer += Time.deltaTime;
            imageColor.a = Mathf.Lerp(0f, 1f, timer / slowFadeDuration);
            fadeImage.color = imageColor;
            yield return null;
        }
        imageColor.a = 1f;
        fadeImage.color = imageColor;
    }
}
