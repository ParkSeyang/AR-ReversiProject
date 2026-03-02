using UnityEngine;
using Fusion;

/// <summary>
/// 피구 공의 네트워크 로직을 관리하며, Rigidbody의 속도(Velocity)를 이용해 물리 충돌 판정을 강화합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class NetworkBall : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 50.0f; // 공의 비행 속도
    [SerializeField] private float lifeTime = 5.0f;   // 공의 수명 (초)

    [Header("Collision Settings")]
    [SerializeField] private LayerMask wallLayer;    

    [Networked] private TickTimer lifeTimer { get; set; }
    [Networked] private float damage { get; set; }        
    [Networked] private PlayerRef ownerRef { get; set; }  
    [Networked] private int teamId { get; set; }          

    private ICombatAgent sender; 
    private Rigidbody ballRigidbody;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
    }

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

        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.IsValid == false) return;

        // [물리 기반 비행] Rigidbody의 속도를 직접 제어하여 물리 충돌 감지 성능을 극대화함
        if (ballRigidbody != null)
        {
            // 중력 영향 없이 정면으로 일직선 비행 유지 (Unity 6 표준: linearVelocity)
            ballRigidbody.linearVelocity = transform.forward * moveSpeed;
        }

        // 수명이 다하면 서버에서 제거
        if (HasStateAuthority == true && lifeTimer.Expired(Runner) == true)
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 서버에서만 충돌 판정을 수행
        if (HasStateAuthority == false) return;

        // 벽 레이어 충돌 시 즉시 소멸
        if (wallLayer.Contains(other.gameObject))
        {
            Runner.Despawn(Object);
            return;
        }

        // 전투 시스템 타겟 확인
        if (CombatSystem.Instance.HasHitTarget(other) == true)
        {
            var targetPart = CombatSystem.Instance.GetHitTarget(other);

            // 아군 오사 방지 및 소유자 제외
            if (targetPart.Owner is PlayerStatusController targetStatus)
            {
                // 다른 팀일 때만 타격 판정
                if (targetStatus.GetComponent<Player>().TeamID != teamId)
                {
                    HitInfo hitInfo = new HitInfo
                    {
                        sender = sender,
                        receiver = targetPart.Owner,
                        position = transform.position,
                        hitTarget = targetPart,
                        gameObject = other.gameObject
                    };

                    targetPart.Owner.TakeDamage(damage, hitInfo);
                    sender?.OnHitDetected(hitInfo);

                    // 적중 후 공 제거
                    Runner.Despawn(Object);
                }
            }
        }
    }
}
