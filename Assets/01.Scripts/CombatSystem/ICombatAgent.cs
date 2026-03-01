using UnityEngine;

/// <summary>
/// 충돌 및 타격 정보를 담는 구조체입니다.
/// </summary>
public struct HitInfo
{
    public ICombatAgent sender;   // 공격을 가한 주체
    public ICombatAgent receiver; // 공격을 받은 주체
    public Vector3 position;      // 충돌 발생 지점
    public IHitTargetPart hitTarget; // 적중된 부위 (HurtBox 등)
    public int parameter; 
    
    public GameObject gameObject; // 충돌된 실제 게임 오브젝트
}

public interface ICombatAgent
{
    /// <summary>
    /// 데미지를 입을 때 호출됩니다.
    /// </summary>
    void TakeDamage(float damage, HitInfo hitInfo);

    /// <summary>
    /// 내가 가한 공격이 적중했을 때 호출됩니다.
    /// </summary>
    void OnHitDetected(HitInfo hitInfo);
}
