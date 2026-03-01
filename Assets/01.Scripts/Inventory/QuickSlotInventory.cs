using UnityEngine;

/// <summary>
/// 피구 게임 전용 3칸짜리 퀵슬롯 시스템입니다.
/// 숫자키 1, 2, 3을 통해 슬롯의 아이템을 사용합니다.
/// </summary>
public class QuickSlotInventory : BaseUI
{
    public override UIType UIType => UIType.QuickSlot;
    public override bool IsPopup => false;

    [Header("Slots (Maximum 3)")]
    [Tooltip("자식 객체로 있는 SlotSystem 3개를 연결해주세요.")]
    [SerializeField] private SlotSystem[] slots = new SlotSystem[3];

    protected override void Awake()
    {
        base.Awake();
        
        // 인스펙터에 할당되지 않았다면 자식들 중에서 자동으로 찾음 (최대 3개)
        if (slots[0] == null)
        {
            var foundSlots = GetComponentsInChildren<SlotSystem>(true);
            for (int i = 0; i < Mathf.Min(foundSlots.Length, 3); i++)
            {
                slots[i] = foundSlots[i];
            }
        }
    }

    private void Update()
    {
        // 팝업(메뉴 등)이 열려있으면 입력 차단
        if (UIManager.IsInitialized == true && UIManager.Instance.IsPopupOpen == true) return;

        // 1, 2, 3 숫자키 입력 감지
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryUseSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryUseSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TryUseSlot(2);
    }

    /// <summary>
    /// 필드에서 아이템 획득 시 호출됩니다.
    /// </summary>
    public bool TryAddItem(Item newItem, int count = 1)
    {
        if (newItem == null) return false;

        // 1. 이미 동일한 아이템이 있고 중첩(Stack)이 가능한지 확인
        foreach (var slot in slots)
        {
            if (slot != null && slot.IsEmpty == false && slot.Item.ItemID == newItem.ItemID)
            {
                if (slot.ItemCount + count <= slot.Item.MaxStack)
                {
                    slot.SetItem(slot.Item, slot.ItemCount + count);
                    return true;
                }
            }
        }

        // 2. 비어있는 슬롯을 찾아서 추가
        foreach (var slot in slots)
        {
            if (slot != null && slot.IsEmpty == true)
            {
                slot.SetItem(newItem, count);
                return true;
            }
        }

        // 3. 슬롯이 꽉 찼을 경우: 랜덤한 슬롯을 선택하여 기존 아이템을 덮어씌움
        int randomSlotIndex = Random.Range(0, slots.Length);
        SlotSystem targetSlot = slots[randomSlotIndex];

        if (targetSlot != null)
        {
            Debug.Log($"[QuickSlot] 인벤토리 가득 참! 슬롯 {randomSlotIndex + 1}의 {targetSlot.Item.ItemName}을(를) 버리고 {newItem.ItemName}을(를) 획득합니다.");
            
            // 기존 아이템 덮어쓰기 (수량도 새로 획득한 수량으로 초기화)
            targetSlot.SetItem(newItem, count);
            
            // 만약 기존 슬롯이 토글형 활성화 상태였다면 이펙트 강제 종료
            targetSlot.SetToggleState(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 특정 인덱스의 슬롯 아이템을 사용 시도합니다.
    /// </summary>
    private void TryUseSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        
        var slot = slots[index];
        if (slot == null || slot.IsEmpty == true) return;

        // 아이템 사용 효과 발동
        bool isUsed = ApplyItemEffect(slot.Item, slot);

        if (isUsed == true)
        {
            slot.UseItem(); // UI 상에서 1개 소모
        }
    }

    /// <summary>
    /// 아이템 종류(포션/토글)에 따라 효과를 적용합니다.
    /// </summary>
    private bool ApplyItemEffect(Item item, SlotSystem slot)
    {
        if (Player.Instance == null) return false;

        var statusController = Player.Instance.GetComponent<PlayerStatusController>();
        if (statusController == null) return false;

        // 기획서 기반 아이템 분류 (임시 로직 - 추후 ItemID에 맞춰 세분화)
        // I001: 체력 포션, I002: 파워볼 등 (데이터 시트에 맞게 수정 필요)
        Debug.Log($"[QuickSlot] {item.ItemName} 아이템 사용!");

        // TODO: 향후 PlayerStatusController 쪽에 아이템별 실제 적용 로직을 구현하고 연결
        // statusController.ApplyItemEffect(item.ItemID);

        // 토글형 아이템의 경우 시각적 피드백 제공 (예시)
        if (item.Description.Contains("토글"))
        {
            slot.SetToggleState(true);
            // 1회 발사 후 꺼지도록 이벤트를 연결해야 함
        }

        return true; // 사용 성공 (임시)
    }
}
