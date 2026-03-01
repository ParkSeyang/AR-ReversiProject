using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        [Header("Ready System")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private GameObject loadingImage;      
        [SerializeField] private float rotationSpeed = 200f;   
        [SerializeField] private float fadeDuration = 0.5f;    

        [Header("Matchmaking Success UI")]
        [SerializeField] private GameObject matchmakingSuccessPanel; // 'MatchMaking Success' 패널
        [SerializeField] private RectTransform successTextTransform; // 날아올 텍스트의 RectTransform
        [SerializeField] private float textFlyInDuration = 0.8f;     // 텍스트가 날아오는 시간
        [SerializeField] private float textFlyInOffset = 1000f;     // 시작 위치(오른쪽 오프셋)

        private GameObject instantiatedIndicator; 
        private bool isReady = false;
        private int currentSelectedIndex = -1;
        private Coroutine colorFadeCoroutine;
        private Coroutine matchSuccessCoroutine;

        private readonly Color dimmedColor = new Color(120f / 255f, 120f / 255f, 120f / 255f, 1f);

        private void Awake()
        {
            if (nickNameText != null)
            {
                string savedNick = PlayerPrefs.GetString("PlayerNickName", "Guest");
                nickNameText.text = $"{savedNick}";
            }

            if (yourSelectPrefab != null)
            {
                instantiatedIndicator = Instantiate(yourSelectPrefab);
                instantiatedIndicator.SetActive(false);
            }

            if (loadingImage != null) loadingImage.SetActive(false);
            if (matchmakingSuccessPanel != null) matchmakingSuccessPanel.SetActive(false);
            
            UpdateReadyUI();
        }

        private void Update()
        {
            if (isReady && loadingImage != null)
            {
                loadingImage.transform.Rotate(Vector3.back * rotationSpeed * Time.deltaTime);
            }
        }

        #region 캐릭터 선택 및 Ready 로직 (기존 유지)

        public void OnCharacterSelect(int index)
        {
            if (isReady) return; 
            if (index < 0 || index >= charSelectPositions.Length) return;

            currentSelectedIndex = index;
            if (instantiatedIndicator != null)
            {
                instantiatedIndicator.SetActive(true);
                instantiatedIndicator.transform.SetParent(charSelectPositions[index]);
                instantiatedIndicator.transform.localPosition = Vector3.zero;
                instantiatedIndicator.transform.localScale = Vector3.one;
                RectTransform rt = instantiatedIndicator.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = Vector2.zero;
            }
        }

        public void OnReadyButtonClick()
        {
            if (currentSelectedIndex == -1) return;

            isReady = !isReady;
            UpdateReadyUI();

            // Ready 상태가 되면 3초 후 매칭 성공 연출 (테스트용)
            if (isReady)
            {
                matchSuccessCoroutine = StartCoroutine(SimulateMatchmaking());
            }
            else
            {
                if (matchSuccessCoroutine != null) StopCoroutine(matchSuccessCoroutine);
                if (matchmakingSuccessPanel != null) matchmakingSuccessPanel.SetActive(false);
            }
        }

        private void UpdateReadyUI()
        {
            if (readyButtonText != null) readyButtonText.text = isReady ? "Cancel" : "Ready?";
            if (loadingImage != null) loadingImage.SetActive(isReady);

            foreach (var btn in charSelectButtons)
            {
                if (btn != null) btn.interactable = !isReady;
            }

            if (colorFadeCoroutine != null) StopCoroutine(colorFadeCoroutine);
            colorFadeCoroutine = StartCoroutine(FadeCharacterColorsRoutine());
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

        #endregion

        #region 매칭 성공 연출

        private IEnumerator SimulateMatchmaking()
        {
            // 3초 대기 (실제로는 서버 응답을 기다리는 시간)
            yield return new WaitForSeconds(3.0f);
            
            if (isReady) // 대기 중에 취소했을 수도 있으므로 다시 확인
            {
                StartCoroutine(ShowMatchSuccessRoutine());
            }
        }

        private IEnumerator ShowMatchSuccessRoutine()
        {
            if (matchmakingSuccessPanel == null || successTextTransform == null) yield break;

            // 1. 패널 활성화 (이미지 노출)
            matchmakingSuccessPanel.SetActive(true);
            
            // 2. 텍스트 초기 위치 설정 (우측 바깥)
            Vector2 targetPos = Vector2.zero;
            Vector2 startPos = new Vector2(textFlyInOffset, 0);
            successTextTransform.anchoredPosition = startPos;

            // 3. 텍스트가 중앙으로 날아오는 애니메이션
            float timer = 0f;
            while (timer < textFlyInDuration)
            {
                timer += Time.deltaTime;
                float t = timer / textFlyInDuration;
                // 이동 효과를 더 다이나믹하게 하기 위해 SmoothStep 적용
                t = Mathf.SmoothStep(0, 1, t);
                successTextTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            successTextTransform.anchoredPosition = targetPos;
            Debug.Log("매칭 성공 연출 완료!");
        }

        #endregion
    }
}
