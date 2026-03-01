using UnityEngine;

[System.Serializable]
public class CombatEffectData
{
    public GameObject hpHealEffect;
    public GameObject mpHealEffect;
    public GameObject guardEffect;
}

public class CombatEventBinder
{
    private CombatEffectData effectData;

    public void Initialize(CombatEffectData data)
    {
        effectData = data;
    }

    public void Enable()
    {
        if (CombatSystem.Instance != null)
        {
            CombatSystem.Instance.Subscribe.OnSomeoneTakeDamage += OnSomeoneTakeDamage;
            CombatSystem.Instance.Subscribe.OnSomeoneHeal += OnSomeoneHeal;
            
        }
    }

    public void Disable()
    {
        // [수정] 종료 시점에 CombatSystem이 이미 파괴되었을 수 있으므로 안전하게 체크
        if (CombatSystem.IsInitialized == false) return;

        var system = CombatSystem.Instance;
        if (system != null && system.Subscribe != null)
        {
            system.Subscribe.OnSomeoneTakeDamage -= OnSomeoneTakeDamage;
            system.Subscribe.OnSomeoneHeal -= OnSomeoneHeal;
            
        }
    }
    
    private void OnSomeoneTakeDamage(CombatEvent combatEvent)
    {
        // 데미지 텍스트나 피격 이펙트 처리 (필요 시)
    }

    private void OnSomeoneHeal(CombatEvent combatEvent)
    {
        /* [임시 주석] 피구 게임 사양에 맞는 이펙트 시스템으로 교체 예정
        if (effectData == null) return;

        GameObject prefab = combatEvent.HitInfo.parameter == 0 ? effectData.hpHealEffect : effectData.mpHealEffect;
        if (prefab == null) return;

        if (combatEvent.Receiver is PlayerStatusController playerController)
        {
            GameObject healEffectInstance = Object.Instantiate(prefab, playerController.transform.position, Quaternion.identity);
            healEffectInstance.transform.SetParent(playerController.transform);
            Object.Destroy(healEffectInstance, 2.0f);
        }
        */
    }

  

    private void OnSomeoneCastSkill(CombatEvent combatEvent)
    {
      
    }
}

