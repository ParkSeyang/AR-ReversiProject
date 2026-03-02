using UnityEngine;
using UnityEngine.UI;

namespace Youstianus
{
    public class UI_HpBar : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private UnityEngine.UI.Image hpFillImage; 
        [SerializeField] private Color allyColor = new Color(0, 1, 0, 1); // 기본 초록/파랑계열
        [SerializeField] private Color enemyColor = Color.red;
        
        private Transform targetTransform;
        private Camera mainCamera;
        private RectTransform rectTransform;
        
        [Header("Offset Settings")]
        [SerializeField] private float yOffset = 2.0f; 
        [SerializeField] private float xOffset = 1.0f; // [요청] 오른쪽으로 1만큼 이동

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            mainCamera = Camera.main;
            rectTransform.localScale = Vector3.one;
        }

        public void Initialize(Transform target, bool isEnemy)
        {
            targetTransform = target;
            SetColor(isEnemy);
            transform.SetAsLastSibling();
        }

        public void SetColor(bool isEnemy)
        {
            if (hpFillImage != null)
            {
                hpFillImage.color = isEnemy ? enemyColor : allyColor;
            }
        }

        public void UpdateHP(float current, float max, bool isDead)
        {
            // [요청] 죽었으면 체력바 끄기
            if (isDead)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (hpFillImage != null && max > 0)
            {
                hpFillImage.fillAmount = Mathf.Clamp01(current / max);
            }
        }

        private void LateUpdate()
        {
            if (targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // 1. 월드 좌표 계산 (오프셋 적용)
            // xOffset을 transform.right 방향으로 더해 캐릭터의 오른쪽으로 이동시킴
            Vector3 worldPos = targetTransform.position + (Vector3.up * yOffset) + (mainCamera.transform.right * xOffset);
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                rectTransform.position = new Vector3(-10000, -10000, 0);
                return;
            }

            screenPos.z = 0;
            rectTransform.position = screenPos;
        }
    }
}
