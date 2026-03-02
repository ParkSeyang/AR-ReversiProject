using UnityEngine;
using Fusion;

/// <summary>
/// 피구 공의 네트워크 로직을 관리하며, Rigidbody의 속도(Velocity)를 이용해 물리 충돌 판정을 강화합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class NetworkBall : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 120.0f; // 공의 비행 속도
    [SerializeField] private float lifeTime = 5.0f;   // 공의 수명 (초)

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

    public override void Spawned()
    {
        // [수정] 인스턴스 할당 및 리지드바디 초기 설정
        ballRigidbody = GetComponent<Rigidbody>();
        
        // [추가] HitBox.Awake 등에 의해 콜라이더가 꺼지는 현상을 방지하기 위해 명시적으로 활성화
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true; // 충돌 이벤트 감지용
        }

        // 서버에서 초기 속도 한 번만 주입
        if (HasStateAuthority == true && ballRigidbody != null)
        {
            ballRigidbody.linearVelocity = transform.forward * moveSpeed;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.IsValid == false) return;

        // [수정] 서버와 클라이언트 모두에서 속도를 강제로 유지하여 시각적 지연을 최소화합니다.
        if (ballRigidbody != null)
        {
            ballRigidbody.linearVelocity = transform.forward * moveSpeed;
        }

        // 수명 체크 및 제거는 서버(Host)에서만 수행
        if (HasStateAuthority == true && lifeTimer.Expired(Runner) == true)
        {
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 서버에서만 충돌 판정을 수행
        if (HasStateAuthority == false) return;

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
