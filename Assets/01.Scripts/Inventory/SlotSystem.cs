using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 피구 게임 전용 퀵슬롯 UI 스크립트입니다. (드래그 앤 드롭 제거, 순수 아이템 표시/사용용)
/// </summary>
public class SlotSystem : MonoBehaviour
{
    public Item Item { get; private set; }
    public int ItemCount { get; private set; }

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    
    [Tooltip("토글형 아이템 활성화 시 켜질 테두리 이펙트")]
    [SerializeField] private GameObject activeEffectObj; 

    public bool IsEmpty => Item == null || ItemCount <= 0;

    private void Awake()
    {
        if (iconImage == null) iconImage = GetComponentInChildren<Image>(true);
        if (activeEffectObj != null) activeEffectObj.SetActive(false);
        UpdateUI();
    }

    /// <summary>
    /// 슬롯에 아이템을 세팅합니다.
    /// </summary>
    public void SetItem(Item item, int count = 1)
    {
        Item = item;
        ItemCount = count > 0 ? count : 0;
        
        if (ItemCount == 0) Item = null;

        UpdateUI();
    }

    /// <summary>
    /// 슬롯의 아이템을 1개 소모합니다.
    /// </summary>
    public void UseItem()
    {
        if (IsEmpty == true) return;

        ItemCount--;
        if (ItemCount <= 0)
        {
            Item = null;
        }
        UpdateUI();
    }

    /// <summary>
    /// 파워볼, 신속 등 토글형 아이템의 시각적 활성화 상태를 켭니다.
    /// </summary>
    public void SetToggleState(bool isActive)
    {
        if (activeEffectObj != null)
        {
            activeEffectObj.SetActive(isActive);
        }
    }

    private void UpdateUI()
    {
        if (IsEmpty == true)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (countText != null) countText.text = "";
            SetToggleState(false);
        }
        else
        {
            if (iconImage != null)
            {
                iconImage.sprite = Item.Icon;
                iconImage.enabled = true;
            }
            if (countText != null) countText.text = ItemCount > 1 ? ItemCount.ToString() : "";
        }
    }
}
