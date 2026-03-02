using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

namespace Youstianus
{
    public class IngameUI : MonoBehaviour
    {
        private static IngameUI instance;
        public static IngameUI Instance
        {
            get
            {
                if (instance == null) instance = FindFirstObjectByType<IngameUI>();
                return instance;
            }
        }

        [Header("Player Nickname Slots")]
        [SerializeField] private TMP_Text[] userNameTexts; 

        [Header("HP Bar Management")]
        [SerializeField] private RectTransform hpBarContainer; 
        public RectTransform HPBarContainer => hpBarContainer;

        [Header("Score UI")]
        [SerializeField] private TMP_Text blueScoreText;      
        [SerializeField] private TMP_Text redScoreText;       

        [Header("Round UI")]
        [SerializeField] private GameObject fadeImageObj;     
        [SerializeField] private Image fadeImageSource;       
        [SerializeField] private TMP_Text informationText;    
        [SerializeField] private TMP_Text timerText;          
        [SerializeField] private Image timerFillImage;        
        [SerializeField] private float flyInDuration = 0.5f;
        [SerializeField] private float flyInOffset = 1000f;

        [Header("Local Player UI")]
        [SerializeField] private Image cooltimeFillImage;     // "Cooltime" Fill 이미지 연결

        private void Awake()
        {
            if (instance == null) instance = this;
            
            if (fadeImageObj != null)
            {
                fadeImageObj.SetActive(true);
                if (fadeImageSource != null) fadeImageSource.color = Color.black;
            }

            if (informationText != null) informationText.text = "";
        }

        private void Update()
        {
            if (GameManager.Instance != null)
            {
                if (timerText != null)
                    timerText.text = Mathf.CeilToInt(GameManager.Instance.RemainingTime).ToString();

                if (timerFillImage != null)
                    timerFillImage.fillAmount = GameManager.Instance.RemainingTime / 60f;

                if (blueScoreText != null) blueScoreText.text = GameManager.Instance.BlueScore.ToString();
                if (redScoreText != null) redScoreText.text = GameManager.Instance.RedScore.ToString();
            }
        }

        /// <summary>
        /// 로컬 플레이어의 공격 쿨타임 UI를 갱신합니다.
        /// </summary>
        /// <param name="remaining">남은 시간</param>
        /// <param name="total">전체 쿨타임</param>
        public void UpdateCooltime(float remaining, float total)
        {
            if (cooltimeFillImage != null && total > 0)
            {
                // 쿨타임 중이면 0(비어있음) -> 1(가득 참)로 차오르는 방식
                // 사용 가능할 때는 1, 쿨타임 중에는 0에서 시작해 1로 복구
                float progress = 1f - (remaining / total);
                cooltimeFillImage.fillAmount = Mathf.Clamp01(progress);
            }
        }

        public void SetLoadingScreen(bool active)
        {
            if (fadeImageObj != null) fadeImageObj.SetActive(active);
        }

        public IEnumerator FadeToBlack(float duration)
        {
            if (fadeImageObj == null || fadeImageSource == null) yield break;
            fadeImageObj.SetActive(true);
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0, 1, timer / duration);
                fadeImageSource.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeImageSource.color = Color.black;
        }

        public void ShowInformation(string msg)
        {
            if (informationText != null) informationText.text = msg;
        }

        public void PlayStartSequence()
        {
            StartCoroutine(StartSequenceRoutine());
        }

        private IEnumerator StartSequenceRoutine()
        {
            yield return StartCoroutine(FlyInText("Ready?"));
            yield return new WaitForSeconds(1.0f);
            yield return StartCoroutine(FlyInText("Fight!"));
            yield return new WaitForSeconds(0.5f);
            if (informationText != null) informationText.text = "";
        }

        private IEnumerator FlyInText(string msg)
        {
            if (informationText == null) yield break;
            informationText.text = msg;
            RectTransform rt = informationText.GetComponent<RectTransform>();
            Vector2 centerPos = Vector2.zero;
            Vector2 startPos = new Vector2(flyInOffset, 0);
            float timer = 0f;
            while (timer < flyInDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timer / flyInDuration);
                rt.anchoredPosition = Vector2.Lerp(startPos, centerPos, t);
                yield return null;
            }
            rt.anchoredPosition = centerPos;
        }

        public void UpdatePlayerNameUI(int slotIndex, string nickName)
        {
            if (slotIndex >= 0 && slotIndex < userNameTexts.Length && userNameTexts[slotIndex] != null)
            {
                userNameTexts[slotIndex].text = nickName;
            }
        }
    }
}
