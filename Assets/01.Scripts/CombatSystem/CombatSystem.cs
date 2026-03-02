using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 중 발생하는 모든 이벤트(데미지, 힐 등)를 중앙에서 수집하고 전파하는 시스템입니다.
/// 무한 루프 방지를 위해 데미지를 직접 가하지 않고, 발생한 정보를 알리는 역할만 수행합니다.
/// </summary>
public class CombatSystem : SingletonBase<CombatSystem>
{
    public class Events
    {
        // 데미지가 발생했을 때 호출
        public Action<CombatEvent> OnSomeoneTakeDamage;
        
        // 회복이 발생했을 때 호출 // 요거 지우고
        public Action<CombatEvent> OnSomeoneHeal;

        // 가드에 성공했을 때 호출 // 요거 지우고 
        public Action<CombatEvent> OnSomeoneGuard;
    }

    public Events Subscribe { get; private set; } = new Events();

    private const int EVENT_PROCESS_PER_FRAME = 10;
    
    private Dictionary<Collider, IHitTargetPart> HitTargetDic { get; set; }
    private Queue<CombatEvent> CombatEventQueue { get; set; }

    protected override void OnInitialize()
    {
        if (HitTargetDic == null) HitTargetDic = new Dictionary<Collider, IHitTargetPart>();
        if (CombatEventQueue == null) CombatEventQueue = new Queue<CombatEvent>();
    }

    /// <summary>
    /// 외부(예: 체력 포션 사용 등)에서 힐 이벤트를 발생시킵니다.  요거 지우고 
    /// </summary>
    public void InvokeHealEvent(CombatEvent healEvent)
    {
        Subscribe.OnSomeoneHeal?.Invoke(healEvent);
    }

    private void Update()
    {
        // 프레임당 정해진 개수만큼 이벤트를 처리하여 부하 분산
        for (int i = 0; i < EVENT_PROCESS_PER_FRAME; i++)
        {
            if (CombatEventQueue.Count == 0) break;
            
            var combatEvent = CombatEventQueue.Dequeue();
            HandleCombatEvent(combatEvent);
        }
    }

    /// <summary>
    /// 발생한 전투 이벤트를 시스템 큐에 등록합니다.
    /// </summary>
    public void AddCombatEvent(CombatEvent combatEvent)
    {
        CombatEventQueue.Enqueue(combatEvent);
    }

    /// <summary>
    /// 큐에서 꺼낸 이벤트를 구독자들에게 전파합니다.
    /// </summary>
    private void HandleCombatEvent(CombatEvent combatEvent)
    {
        // [중요 수정] Receiver.TakeDamage를 여기서 호출하지 않습니다. 
        // TakeDamage는 공격 주체(공)가 직접 호출하며, 이벤트는 그 결과를 알리기 위해 등록됩니다.
        if (combatEvent.Receiver != null)
        {
            Subscribe.OnSomeoneTakeDamage?.Invoke(combatEvent);
        }
    }

    #region IHitTargetPart Management Methods

    public void AddHitTarget(Collider col, IHitTargetPart hitTarget)
    {
        if (col == null) return;
        if (HitTargetDic.ContainsKey(col) == false)
        {
            HitTargetDic.Add(col, hitTarget);
        }
    }

    public void RemoveHitTarget(Collider col, IHitTargetPart hitTarget)
    {
        if (HitTargetDic.ContainsKey(col) == false) return;
        HitTargetDic.Remove(col);
    }

    public bool HasHitTarget(Collider collider)
    {
        if (collider == null) return false;
        return HitTargetDic.ContainsKey(collider);
    }

    public IHitTargetPart GetHitTarget(Collider collider)
    {
        if (collider == null) return null;
        return HitTargetDic.TryGetValue(collider, out var target) ? target : null;
    }
    
    #endregion
}
