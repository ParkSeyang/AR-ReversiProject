using System;

[Serializable]
public class ItemInfo
{
    public string ItemID { get; set; }
    public string ItemName { get; set; }
    public string ItemCategory { get; set; }
    public float Value { get; set; }
    public string Description { get; set; }
    public int Stack { get; set; } // 최대 중첩 개수
}
