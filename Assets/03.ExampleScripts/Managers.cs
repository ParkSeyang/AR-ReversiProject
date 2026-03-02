

// 퍼사드 패턴 예시
public class Managers
{ 
    // 퍼사드 패턴을 사용하면 OOP 객체지향의 규칙에 위배되는부분이있다,
    // 각객체는 독립적인 성향을 가지고있어야되는데 이렇게 작성되면 종속성이 강해져서 
    public static Managers Instance;

    private GameDataManager gameDataManager;
    
    private MonsterDataManager monsterDataManager;
    
    private BattleDataManager battleDataManager;


}


public class GameDataManager
{
}

public class MonsterDataManager
{
}
public class BattleDataManager
{
}