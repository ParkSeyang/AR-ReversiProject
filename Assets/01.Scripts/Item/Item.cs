using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Dodgeball/Item")]
public class Item : ScriptableObject
{
    [Header("Identify")]
    public string ItemID;      // TSV의 ItemID와 매칭
    public string ItemName;    // TSV의 ItemName

    [Header("Data")]
    public string ItemCategory;
    public float Value;        // 공의 파워, 회복량 등 핵심 수치
    [TextArea] 
    public string Description;
    public int MaxStack;       // 최대 중첩 가능 개수

    [Header("Visual & Resources")]
    public Sprite Icon;        // UI 아이콘
    public GameObject Prefab;  // 실제 생성될 공 프리팹 등
}
