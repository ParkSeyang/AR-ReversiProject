using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ItemDataManager : SingletonBase<ItemDataManager>
{
    // ID 기반 데이터 딕셔너리 (TSV 원본 데이터 보관용)
    private Dictionary<string, ItemInfo> itemInfoTable = new Dictionary<string, ItemInfo>();
    
    // ID 기반 리소스(SO) 딕셔너리 (실제 게임에서 사용될 에셋 보관용)
    private Dictionary<string, Item> itemResourceTable = new Dictionary<string, Item>();

    public IReadOnlyDictionary<string, ItemInfo> ItemInfoTable => itemInfoTable;

    protected override void OnInitialize()
    {
        LoadItemTable();
        LoadItemResources();
    }

    /// <summary>
    /// StreamingAssets에 있는 TSV 파일을 읽어 딕셔너리에 저장합니다.
    /// </summary>
    private void LoadItemTable()
    {
        // 경로: Assets/StreamingAssets/TSVData/ItemData.tsv
        string path = Path.Combine(Application.streamingAssetsPath, "TSVData", "ItemData.tsv");
        List<ItemInfo> list = TSVReader.ReadTable<ItemInfo>(path);

        if (list == null)
        {
            Debug.LogWarning("[ItemDataManager] TSV 데이터를 찾을 수 없습니다. 경로를 확인하세요.");
            return;
        }

        foreach (var info in list)
        {
            if (itemInfoTable.ContainsKey(info.ItemID) == false)
            {
                itemInfoTable.Add(info.ItemID, info);
            }
        }
    }

    /// <summary>
    /// Resources/Items 폴더의 SO들을 로드하고 TSV 데이터와 동기화합니다.
    /// </summary>
    private void LoadItemResources()
    {
        // Resources/Items 폴더 내의 모든 Item ScriptableObject를 로드
        Item[] items = Resources.LoadAll<Item>("Items");
        
        foreach (var item in items)
        {
            // SO에 설정된 ItemID를 기준으로 TSV 데이터 매칭
            if (itemInfoTable.TryGetValue(item.ItemID, out var info))
            {
                // TSV 데이터를 SO 인스턴스에 주입 (런타임 수치 동기화)
                item.ItemName = info.ItemName;
                item.ItemCategory = info.ItemCategory;
                item.Value = info.Value;
                item.Description = info.Description;
                item.MaxStack = info.Stack;

                if (itemResourceTable.ContainsKey(item.ItemID) == false)
                {
                    itemResourceTable.Add(item.ItemID, item);
                }
            }
        }
        
        Debug.Log($"[ItemDataManager] {itemResourceTable.Count}개의 아이템 리소스를 동기화했습니다.");
    }

    /// <summary>
    /// ID를 통해 개별 인스턴스화된 아이템 객체를 가져옵니다.
    /// </summary>
    public Item GetItem(string itemId)
    {
        if (itemResourceTable.TryGetValue(itemId, out var original))
        {
            // 원본 SO를 복제하여 반환 (데이터 오염 방지)
            Item instance = Instantiate(original);
            instance.name = original.name; 
            return instance;
        }
        return null;
    }
}
