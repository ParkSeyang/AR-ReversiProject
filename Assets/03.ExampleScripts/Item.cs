using System.Collections.Generic;

namespace Study.Examples.Fusion
{
    // 아래는 테스트하기 위한 데이터 세트
    
    public enum ItemType { Weapon, Armor, Potion, Accessory }
    public enum Rarity { Common, Rare, Epic, Legendary }

    public class Item
    {
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public int Price { get; set; }
        public Rarity Rarity { get; set; }
        
        public static List<Item> GetDummyData()
        {
            List<Item> items = new List<Item>
            {
                // Weapons (10개)
                new Item { Name = "훈련용 목검", Type = ItemType.Weapon, Price = 50, Rarity = Rarity.Common },
                new Item { Name = "녹슨 철검", Type = ItemType.Weapon, Price = 150, Rarity = Rarity.Common },
                new Item { Name = "강철 롱소드", Type = ItemType.Weapon, Price = 1200, Rarity = Rarity.Rare },
                new Item { Name = "사냥꾼의 활", Type = ItemType.Weapon, Price = 800, Rarity = Rarity.Common },
                new Item { Name = "미스릴 단검", Type = ItemType.Weapon, Price = 2500, Rarity = Rarity.Rare },
                new Item { Name = "피를 마시는 도끼", Type = ItemType.Weapon, Price = 5500, Rarity = Rarity.Epic },
                new Item { Name = "화염의 지팡이", Type = ItemType.Weapon, Price = 4200, Rarity = Rarity.Epic },
                new Item { Name = "심판의 망치", Type = ItemType.Weapon, Price = 9000, Rarity = Rarity.Legendary },
                new Item { Name = "암살자의 표창", Type = ItemType.Weapon, Price = 300, Rarity = Rarity.Common },
                new Item { Name = "용살자의 창", Type = ItemType.Weapon, Price = 12000, Rarity = Rarity.Legendary },

                // Armors (10개)
                new Item { Name = "허름한 튜닉", Type = ItemType.Armor, Price = 30, Rarity = Rarity.Common },
                new Item { Name = "가죽 조끼", Type = ItemType.Armor, Price = 200, Rarity = Rarity.Common },
                new Item { Name = "사슬 갑옷", Type = ItemType.Armor, Price = 1500, Rarity = Rarity.Rare },
                new Item { Name = "강철 판금 갑옷", Type = ItemType.Armor, Price = 3500, Rarity = Rarity.Rare },
                new Item { Name = "빛의 로브", Type = ItemType.Armor, Price = 4000, Rarity = Rarity.Epic },
                new Item { Name = "그림자 망토", Type = ItemType.Armor, Price = 4500, Rarity = Rarity.Epic },
                new Item { Name = "용의 비늘 갑옷", Type = ItemType.Armor, Price = 15000, Rarity = Rarity.Legendary },
                new Item { Name = "나무 방패", Type = ItemType.Armor, Price = 100, Rarity = Rarity.Common },
                new Item { Name = "타워 실드", Type = ItemType.Armor, Price = 2000, Rarity = Rarity.Rare },
                new Item { Name = "절대 방어의 방패", Type = ItemType.Armor, Price = 8000, Rarity = Rarity.Epic },

                // Potions (6개) - 소모품 그룹화 테스트용
                new Item { Name = "하급 체력 물약", Type = ItemType.Potion, Price = 50, Rarity = Rarity.Common },
                new Item { Name = "중급 체력 물약", Type = ItemType.Potion, Price = 150, Rarity = Rarity.Common },
                new Item { Name = "상급 체력 물약", Type = ItemType.Potion, Price = 500, Rarity = Rarity.Rare },
                new Item { Name = "마나 물약", Type = ItemType.Potion, Price = 200, Rarity = Rarity.Common },
                new Item { Name = "활력의 비약", Type = ItemType.Potion, Price = 1000, Rarity = Rarity.Epic },
                new Item { Name = "불사의 엘릭서", Type = ItemType.Potion, Price = 50000, Rarity = Rarity.Legendary },

                // Accessories (4개) - 기타 카테고리
                new Item { Name = "구리 반지", Type = ItemType.Accessory, Price = 100, Rarity = Rarity.Common },
                new Item { Name = "힘의 반지", Type = ItemType.Accessory, Price = 3000, Rarity = Rarity.Rare },
                new Item { Name = "민첩의 목걸이", Type = ItemType.Accessory, Price = 3200, Rarity = Rarity.Rare },
                new Item { Name = "고대 왕의 반지", Type = ItemType.Accessory, Price = 25000, Rarity = Rarity.Legendary }
            };
            return items;
        }
    }
}