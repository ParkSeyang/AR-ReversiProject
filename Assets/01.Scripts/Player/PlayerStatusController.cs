using UnityEngine;
using Fusion;

/// <summary>
/// 플레이어의 실시간 상태(공격력 배율, 데미지 판정 등)를 관리하는 컨트롤러입니다.
/// 각 플레이어 프리팹에 부착되어 독립적으로 동작합니다.
/// </summary>
public class PlayerStatusController : NetworkBehaviour, ICombatAgent
{
    private Player player;

    // 현재 공격력 배율 (네트워크 동기화)
    [Networked] public float CurrentATKMultiplier { get; set; } = 1.0f;

    private Player EnsurePlayer()
    {
        if (player == null)
        {
            player = GetComponent<Player>();
        }
        return player;
    }

    public override void Spawned()
    {
        EnsurePlayer();
        InitializeCombat();
    }

    private void InitializeCombat()
    {
        var targetPlayer = EnsurePlayer();
        if (targetPlayer == null) return;

        // 본인 하위의 모든 HitBox와 HurtBox를 이 컨트롤러에 연결
        var hitBoxes = targetPlayer.GetComponentsInChildren<HitBox>(true);
        foreach (var hitBox in hitBoxes)
        {
            hitBox.Initialize(this);
        }

        var hurtBoxes = targetPlayer.GetComponentsInChildren<HurtBox>(true);
        foreach (var hurtBox in hurtBoxes)
        {
            hurtBox.Initialize(this);
        }
    }

    // --- 공격력 로직 ---

    /// <summary>
    /// 최종 공격력을 계산하고, 일회성 버프가 있다면 소모합니다. (서버 전용)
    /// </summary>
    public float GetFinalDamageAndConsumeBuff()
    {
        if (HasStateAuthority == false || player == null) return 0f;

        // Player.cs에 설정된 기본 공격력에 배율 적용
        float finalDamage = player.ATK * CurrentATKMultiplier;

        // 버프가 적용된 상태라면(1.0 초과) 사용 후 즉시 배율 초기화
        if (CurrentATKMultiplier > 1.0f)
        {
            CurrentATKMultiplier = 1.0f;
            Debug.Log($"[Buff Consumed] Final Damage: {finalDamage}");
        }

        return finalDamage;
    }

    /// <summary>
    /// 공격력 버프 아이템 획득 시 호출됩니다. (서버 전용)
    /// </summary>
    public void ApplyAttackBuff(float multiplier = 1.5f)
    {
        if (HasStateAuthority == true)
        {
            CurrentATKMultiplier = multiplier;
            Debug.Log($"[Buff Applied] ATK Multiplier set to {multiplier}x");
        }
    }

    // --- ICombatAgent 구현 (데미지 처리) ---

    /// <summary>
    /// 공에 맞았을 때 데미지를 적용합니다. (서버 전용)
    /// </summary>
    public void TakeDamage(float damage, HitInfo hitInfo)
    {
        // 1. 서버(Host)만 실제 체력을 수정할 수 있음
        if (HasStateAuthority == false || player == null) return;

        // 2. 이미 탈락(HP <= 0)한 상태면 중복 처리 방지
        if (player.HP <= 0) return;

        // 3. 체력 차감 및 네트워크 동기화 (HP는 [Networked] 속성)
        float nextHP = Mathf.Max(0, player.HP - damage);
        player.HP = nextHP;

        Debug.Log($"[Combat] {player.PlayerName} took {damage} damage. Remaining HP: {player.HP}");

        // 4. [옵저버 패턴] 전투 이벤트 등록 (모든 클라이언트에 이펙트 등을 띄우기 위함)
        CombatEvent damageEvent = new CombatEvent 
        { 
            Sender = hitInfo.sender,
            Receiver = this, 
            Damage = damage, 
            HitInfo = hitInfo 
        };
        CombatSystem.Instance.AddCombatEvent(damageEvent);

        // 5. 상태에 따른 후속 처리
        if (player.HP <= 0)
        {
            RPC_HandleDeath();
        }
        else
        {
            // 모든 클라이언트에서 피격 애니메이션 재생
            RPC_PlayHitAnimation();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitAnimation()
    {
        GetComponent<Animator>()?.SetTrigger("Hit");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HandleDeath()
    {
        Debug.Log($"[Combat] {player.PlayerName} is OUT!");
        GetComponent<Animator>()?.SetTrigger("Dead");
        
        // 사망 후 콜라이더 비활성화 등을 통해 추가 타격 방지
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    /// <summary>
    /// 본인이 던진 공이 적에게 적중했을 때 호출됩니다.
    /// </summary>
    public void OnHitDetected(HitInfo hitInfo)
    {
        // 피격 성공 시의 피드백(사운드, 점수 등) 처리 로직이 들어갈 자리입니다.
    }
}
