using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

namespace Study.Examples.Fusion
{
    public class Study_Lamda : MonoBehaviour
    {

        // 람다 표현식 문법 : (매개 변수) => {코드 본문};
        // 특정 조건에 따라 위의 내용중 생략 가능한 내용들이 있음.

        #region 람다의 1단계

        public int Property_Number { get; set; } = 5;

        private int number = 5;

        public int Lamda_Number => number; // 람다는 함수판정이다.


        // 람다 표현식을 이용한 간단한 Get 필드 만들기
        public string MonsterPath => Path.Combine(Application.persistentDataPath, "Monster");

        public string GameDataPath => Path.Combine(Application.persistentDataPath, "GameData");

        public string BossMonsterDataPath => Path.Combine(GameDataPath, "BossMonsterData");

        public string ItemDataPath => Path.Combine(GameDataPath, "ItemData");

        public string UserDataPath => Path.Combine(Application.persistentDataPath, "GameData");

        public string QuickSlotPersetPath => Path.Combine(UserDataPath, "QuickSlotPerset");

        // 람다 표현식(=>)을 이용하면 로직과 필드의 결합이 가능하다.
        // "필드에 로직을 추가할 수 있다" 라고 생각하면 됩니다.


        public Action StartMethod => Start; // 델리게이트를 사용합니다,

        // 람다 표현식을 이용한 public 필드 노출 방법
        public class Person
        {
            public string Name;
        }

        // 자료구조의 특정 객체
        private List<Person> persons = new List<Person>();
        public Person FirstPerson => persons[0];

        // 객체 안의 필드
        private Person BestPerson;
        public string BestPersonName => BestPerson.Name;

        void Start()
        {
            Debug.Log("Start");

            // LambdaTwo();
            // LambdaFour();
            LambdaFive();
        }



        #endregion

        #region 람다의 2단계

        void LambdaTwo()
        {
            // 람다는 기본적으로 함수 판정.
            // 매개변수를 활용 가능
            // 메서드 자체를 저장할 수 있음.
            Action methodA = () => { Debug.Log("methodA"); };

            methodA.Invoke();
            methodA.Invoke();

            Action<string> methodB = (message) => { Debug.Log($"MethodB : {message}"); };

            methodB.Invoke("PSY");

            methodB = (message) => { Debug.Log($"MethodB v02 : {message}"); };

            methodB.Invoke("ZeroDarkMos");
        }


        void LambdaThree(Action<int> method)
        {
            for (int i = 0; i < 10; i++)
            {
                method.Invoke(i);
            }
        }

        #endregion

        #region 람다의 3단계

        void LambdaFour()
        {
            // Func은 반환형을 가지고있는 델리게이트이다.
            Func<int, int, bool> method = (numA, numb) => { return (numA + numb) % 2 == 0; };

            int[] aryA = { 0, 1, 2, 3, 4 };
            int[] aryB = { 10, 11, 12, 13, 14 };


            for (int i = 0; i < 5; i++)
            {
                Debug.Log($"{aryA[i]},  {aryB[i]} = {method.Invoke(aryA[i], aryB[i])}");
            }


        }

        // 조건식(반환형 타입) 람다의 활용

        public enum ItemType
        {
            Weapon,
            Armor,
            Potion
        }

        public enum Rarity
        {
            Common,
            Rare,
            Epic
        }

        public class Item
        {
            public string Name { get; set; }
            public ItemType Type { get; set; }
            public int Price { get; set; }
            public Rarity Rarity { get; set; }
        }

        List<Item> items = new List<Item>
        {
            new Item { Name = "낡은 검", Type = ItemType.Weapon, Price = 150, Rarity = Rarity.Common },
            new Item { Name = "강철 갑옷", Type = ItemType.Armor, Price = 1200, Rarity = Rarity.Rare },
            new Item { Name = "화염의 검", Type = ItemType.Weapon, Price = 2500, Rarity = Rarity.Epic },
            new Item { Name = "체력 물약", Type = ItemType.Potion, Price = 50, Rarity = Rarity.Common },
            new Item { Name = "빛의 방패", Type = ItemType.Armor, Price = 3000, Rarity = Rarity.Epic },
            new Item { Name = "서리 단검", Type = ItemType.Weapon, Price = 1800, Rarity = Rarity.Rare }
        };

        void Foreach(Func<Item, bool> condition)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var result = condition.Invoke(items[i]);
                if (result)
                {
                    Debug.Log($"{items[i].Name}");
                }

            }
        }

        void LambdaFive()
        {
            Debug.Log("======= item.Price > 1000 ====");
            Foreach((item) => item.Price > 1000);

            Debug.Log("======= item.Type == ItemType.Weapon ====");
            Foreach((item) => item.Type == ItemType.Weapon);
        }

        #endregion

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                LambdaThree((number) => { Debug.Log($"Lambda Three Alpha1: {number}"); });
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                LambdaThree((number) => { Debug.Log($"Lambda Three Alpha2: {number * 20}"); });
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                LambdaThree((number) => { Debug.Log($"Lambda Three Alpha3: {number * 30}"); });
            }
        }
    }

    public class LamdaTest
    {
        private Study_Lamda lamda;

        public void Test()
        {
            // 람다식은 프로퍼티의 get처럼 함수로서 인식되어 사용되고있다.
            // 주석을 풀어서 Lamda_Number에 마우스를 가져다 대보세요.
            // ex) lamda.Lamda_Number = 5;



        }

    }
}