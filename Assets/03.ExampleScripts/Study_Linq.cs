using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;  // C#에서 지원하는 Linq 네임스페이스
using Study;

namespace Study.Examples.Fusion
{ 
    public class Study_Linq : MonoBehaviour 
    {
    private List<Item> items;
    
    #region Linq Part 1

    void Linq1() 
    {
        //Linq의 원형 = 어디서든 만들어서 쓸 수 있다.
        
        var accessoryItems = from item in items
            where item.Type == ItemType.Accessory
            select item;
        Debug.Log("=====accessoryItems======");
        foreach (var item in accessoryItems)
        {
            Debug.Log(item.Name);
        }

        var legendaryItems = from item in items
            where item.Rarity == Rarity.Legendary
            select item;
        Debug.Log("=====legendaryItems======");
        foreach (var item in legendaryItems)
        {
            Debug.Log(item.Name);
        }

        var accOrLegendItems =
            from item in items
            where item.Type == ItemType.Accessory || item.Rarity == Rarity.Legendary 
            select item;
        
        Debug.Log("=====accOrLegendItems======");
        foreach (var item in accOrLegendItems)
        {
            Debug.Log(item.Name);
        }
        
    }
    
    #endregion

    #region Linq Part 2

    /*  Where(필터링)
     * 컬렉션(IEnumerable, List, Dictionary, Stack, Queue, HashSet 등등)에서
     * 특정 조건을 만족하는 요소만 추출
     * bool을 반환하는 람다식(함수를 넘겨도 되긴함)을 인자(매개변수)로 받으며, SQL의 Where 절과 유사함.
     * 예시 : items.Where(item => item.Type == ItemType.Accessory)
     * 
     * Select(프로젝션)
     * 컬렉션의 각 요소를 새로운 형태(타입)으로 변환 합니다.
     * 속성만 추출하거나, 완전히 새로운 익명 타입 또는 객체로 변환할때 사용,
     * PS : DTO => 런타임 객체로 바꿀때 매우 유용함.
     * 예시 : items.Select(item => item.Name);
     *
     * First / FirstOrDefault(단일 요소 추출)
     * 조건에 맞는 첫번째 요소를 반환합니다.
     * 예시 : items.FirstOrDefault(item => item.Rarity == Rarity.Legendary)
     *
     * ToList / ToArray / To{SomeCollection} (즉시 실행 및 변환)
     * Linq 쿼리의 결과를 리스트나 배열로 만들어서 반환합니다.
     * 메모리에 결과를 남겨두고 싶거나 결과를 반복적으로 참조해야 할 때 사용합니다.
     *
     *  All / Any(조건 검증)
     * 컬렉션 내 요소들이 조건을 만족하는지 bool로 확인합니다.
     * All은 모든 요소가 만족해야 true 반환.
     * Any는 하나만 만족해도 true를 반환.
     * 예시 : items.Any(item => item.Price > 10000);
     *
     * ============ 기본적인 Linq 종료 =========
     *
     * 아래부터는 실용적인 내용입니다.
     *
     * OrderBy / OrderByDescending (정렬)
     * 특정 기준에 따라서 컬렉션을 오름차순 또는 내림차순으로 정렬 합니다.
     * ThenBy를 추가하여 연속 정렬도 가능함.
     * 예시 : items.OrderBy(item => item.Name).ThenBy(item => item.Price);
     *
     * GroupBy (그룹화)
     * 동일한 키 값을 가진 요소들끼리 그룹으로 묶습니다.
     * 딕셔너리화 해줄때 보통 씁니다. 통계같은거 낼때 사용함.
     *
     * SelectMany
     * 중첩된 컬렉션 구조를 하나의 단일 컬렉션으로 변환합니다.
     * 리스트안에 리스트가 있거나, 딕셔너리 안에 리스트가 있는 등의 컬렉션을 처리할때 유용합니다.
     * PS : 사용하다보면 이해가 됩니다.
     *
     *
     * Distinct (중복 제거)
     * 컬렉션에서 중복된 요소를 제거하고 고유한 값만 남깁니다.
     * 예시 : var distinctSample = items.Distinct().ToList();
     * 
     * Count / Sum / Average / Min / Max
     * 데이터 합계, 평균, 최소값, 최대값을 구해줍니다.
     *
     * 예시 : int totalPrice = items.Sum(item => item.Price);
     */
    
    void Linq2()
    {
        var accessoryItems = items.Where(item => item.Type == ItemType.Accessory);
        
        var accessoryList = accessoryItems.ToList();

        Debug.Log("=====accessoryItemNames======");
        foreach (var name in accessoryList)
        {
            Debug.Log(name);
        }
        
        var allItemNames = items.Select(item => item.Name);
        
        Debug.Log("=====allItemNames======");
        foreach (var name in allItemNames)
        {
            Debug.Log(name);
        }

        var firstLegendaryItem = items.FirstOrDefault(item => item.Rarity == Rarity.Legendary);
        
        Debug.Log("=====firstLegendaryItem======");
        
        Debug.Log(firstLegendaryItem.Name);


        var anyResult = items.Any(item => item.Name.Equals("심판의 망치"));
        
        Debug.Log($"심판의 망치가 있니? {anyResult}");

        var anyResult2 = items.Any(item => item.Name.Equals("토르의 망치"));
        
        Debug.Log($"토르의 망치가 있니? {anyResult2}");
        
        var anyResult3 = items.Any(item => item.Price > 10000);
        
        Debug.Log($"10000원이 넘는 아이템이 있니? {anyResult3}");

        var orderByAscending = items.OrderBy(item => item.Price);
        
        Debug.Log("=====orderByAscending======");
        foreach (var item in orderByAscending)
        {
            Debug.Log($"{item.Name}, {item.Price}");
        }

        var orderByDescending = items.OrderByDescending(item => item.Price);
        Debug.Log("=====orderByDescending======");
        foreach (var item in orderByDescending)
        {
            Debug.Log($"{item.Name}, {item.Price}");
        }

        var orderBySample = items.OrderBy(item => item.Name).ThenBy(item => item.Price);
        
        Debug.Log("=====orderBySample=======");
        foreach (var item in orderBySample)
        {
            Debug.Log($"{item.Name}, {item.Price}");
        }

       
        Debug.Log("=====groupBySample======");
        var groupBySample = items.GroupBy(item => item.Rarity);
        Dictionary<Rarity, List<Item>> itemDic = items.GroupBy(item => item.Rarity).ToDictionary
        (
            group => group.Key, 
            group => group.ToList()
        );
        
        foreach (var group in itemDic)
        {
            foreach (var item in group.Value)
            {
                Debug.Log($"{group.Key}, {item.Name}");
            }
        }

        Debug.Log("=====selectManySample======");

        var selectManySample = itemDic.SelectMany(group => group.Value);

        foreach (var item in selectManySample)
        {
            Debug.Log($"{item.Name}");
        }
        
        Debug.Log("=====AddRangeSample======");

        items.AddRange(selectManySample.ToList());
        items = items.OrderBy(item => item.Name).ToList();
        foreach (var item in items)
        {
            Debug.Log($"{item.Name}");
        }

        var distinctSample = items.Distinct().ToList();
        
        Debug.Log("=====distinctSample=======");
        
        foreach (var item in distinctSample)
        {
            Debug.Log($"{item.Name}");
        }

        
        Debug.Log("=====sumSample=======");

        int totalPrice = items.Sum(item => item.Price);

        Debug.Log($"total Price {totalPrice}");

    }

    #endregion


    public void Start()
    {
        items = Item.GetDummyData();
       // Linq1();
        Linq2(); 
    } 
    
    
    }
}


