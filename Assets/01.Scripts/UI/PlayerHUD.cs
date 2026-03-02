using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 머리 위에 표시되는 체력바와 닉네임을 관리하며, 카메라를 정면으로 바라봅니다.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;         // HP바의 실질적인 Fill 이미지
    [SerializeField] private TextMeshProUGUI nameText; // [추가] 플레이어 닉네임 텍스트

    [Header("Team Colors")]
    [SerializeField] private Color blueTeamColor = Color.cyan;
    [SerializeField] private Color redTeamColor = Color.red;

    private Player player;
    private Camera mainCamera;

    private void Awake()
    {
        // 최상위 부모(캐릭터)로부터 Player 데이터 관리 스크립트를 찾아냄
        player = GetComponentInParent<Player>();
        mainCamera = Camera.main;
    }

    private void Start()
    {
        RefreshStaticInfo();
    }

    /// <summary>
    /// 닉네임, 팀 컬러 등 정적인 정보를 초기화합니다.
    /// </summary>
    public void RefreshStaticInfo()
    {
        if (player == null) player = GetComponentInParent<Player>();
        if (player == null) return;

        // [1] 닉네임 표시 (NetworkString을 string으로 변환)
        if (nameText != null)
        {
            nameText.text = player.PlayerName.ToString();
        }

        // [2] 팀에 따른 HP바 색상 설정
        if (fillImage != null)
        {
            fillImage.color = (player.TeamID == 0) ? blueTeamColor : redTeamColor;
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // [1] 체력바 실시간 갱신 (Fill Amount)
        if (fillImage != null && player.MaxHP > 0)
        {
            fillImage.fillAmount = player.HP / player.MaxHP;
        }

        // [2] 빌보드 효과 (항상 카메라 정면 바라보기)
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }

        // [3] 닉네임 실시간 동기화 (텍스트가 비었거나 실제 데이터와 다를 때만 갱신)
        if (nameText != null)
        {
            string currentSyncedName = player.PlayerName.ToString();
            if (string.IsNullOrEmpty(nameText.text) == true || nameText.text != currentSyncedName)
            {
                nameText.text = currentSyncedName;
            }
        }
    }
}
