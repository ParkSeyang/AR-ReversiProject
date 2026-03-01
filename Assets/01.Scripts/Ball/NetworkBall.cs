using UnityEngine;
using Fusion;

/// <summary>
/// 피구 게임의 핵심 투사체인 공의 네트워크 로직을 관리하며, Tool의 확장 메서드를 활용합니다.
/// </summary>
public class NetworkBall : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 50.0f; // 공의 비행 속도
    [SerializeField] private float lifeTime = 5.0f;   // 공의 수명 (초)

    [Header("Collision Settings")]
    [SerializeField] private LayerMask wallLayer;    // 벽 레이어 (LayerMaskExtension 활용)

    [Networked] private TickTimer lifeTimer { get; set; } // 공의 수명 타이머
    [Networked] private float damage { get; set; }        // 공이 줄 데미지
    [Networked] private PlayerRef ownerRef { get; set; }  // 공을 던진 플레이어
    [Networked] private int teamId { get; set; }          // 공을 던진 플레이어의 팀 ID

    private ICombatAgent sender; // 데미지 판정 시 전달할 공격자 정보 (서버 전용 캐시)

    /// <summary>
    /// 공을 생성한 후 초기 데이터를 주입합니다. (서버 전용)
    /// </summary>
    public void Init(float baseDamage, PlayerRef owner, int team, ICombatAgent attacker = null)
    {
        if (HasStateAuthority == false) return;

        damage = baseDamage;
        ownerRef = owner;
        teamId = team;
        sender = attacker;

        // 공의 수명 타이머 시작
        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.IsValid == false) return;

        // 1. 앞으로 직선 이동 (Kinematic 방식)
        transform.Translate(Vector3.forward * moveSpeed * Runner.DeltaTime);

        // 2. 수명이 다하면 서버에서 제거
        if (HasStateAuthority == true && lifeTimer.Expired(Runner) == true)
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 서버(State Authority)에서만 충돌 판정을 수행하여 결과의 신뢰성을 확보함
        if (HasStateAuthority == false) return;

        // [Tool 활용] LayerMaskExtensions.Contains를 사용하여 벽 충돌 처리
        if (wallLayer.Contains(other.gameObject))
        {
            Runner.Despawn(Object);
            return;
        }

        // 1. 충돌한 대상이 전투 시스템에 등록된 타겟인지 확인
        if (CombatSystem.Instance.HasHitTarget(other) == true)
        {
            var targetPart = CombatSystem.Instance.GetHitTarget(other);

            // 2. 아군 오사 방지 (TeamID 비교) 및 소유자 본인 제외
            if (targetPart.Owner is PlayerStatusController targetStatus)
            {
                if (targetStatus.GetComponent<Player>().TeamID == teamId) return;

                // 3. 데미지 정보 구성 및 전달
                HitInfo hitInfo = new HitInfo
                {
                    sender = sender,
                    receiver = targetPart.Owner,
                    position = transform.position,
                    hitTarget = targetPart,
                    gameObject = other.gameObject
                };

                targetPart.Owner.TakeDamage(damage, hitInfo);

                // 4. 공격자에게 타격 성공 알림
                sender?.OnHitDetected(hitInfo);

                // 5. 충돌 후 공 제거
                Runner.Despawn(Object);
            }
        }
    }
}
