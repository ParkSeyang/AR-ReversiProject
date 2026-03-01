using UnityEngine;
using Fusion;
using UnityEngine.AI;

/// <summary>
/// 플레이어의 공격 로직을 관리하며, 애니메이션 타임라인의 'Shoot' 이벤트를 직접 수신하여 공을 발사합니다.
/// </summary>
public class PlayerAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private NetworkObject ballPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float attackCooldown = 1.0f;

    [Header("Network States")]
    [Networked] public bool IsAttacking { get; set; }
    [Networked] private TickTimer attackCooldownTimer { get; set; }
    [Networked] private bool hasShot { get; set; }

    private Animator animator;
    private NavMeshAgent navAgent;
    private PlayerMovement movement;
    private Player player;
    private PlayerStatusController status;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    public override void Spawned()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        navAgent = GetComponent<NavMeshAgent>();
        movement = GetComponent<PlayerMovement>();
        player = GetComponent<Player>();
        status = GetComponent<PlayerStatusController>();

        if (shootPoint == null) shootPoint = transform;
    }

    /// <summary>
    /// 애니메이션 타임라인의 이벤트 마커에 의해 호출되는 발사 함수입니다.
    /// </summary>
    public void Shoot()
    {
        // 서버 권한이 있고, 이번 공격에서 아직 발사하지 않았을 때만 실행
        if (HasStateAuthority == true && hasShot == false)
        {
            SpawnBall();
            hasShot = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || Object.IsValid == false) return;

        if (GetInput(out NetworkInputData inputData))
        {
            if (inputData.Buttons.IsSet(NetworkInputData.BUTTON_ATTACK))
            {
                if (attackCooldownTimer.ExpiredOrNotRunning(Runner) && IsAttacking == false)
                {
                    StartAttackSequence(inputData.ClickPosition);
                }
            }
        }

        // 공격 동작 종료 체크 (0.8초 정도 지나면 이동 가능하게 복구)
        if (IsAttacking == true && attackCooldownTimer.RemainingTime(Runner) < (attackCooldown - 0.8f))
        {
            EndAttackSequence();
        }
    }

    private void StartAttackSequence(Vector3 targetPosition)
    {
        IsAttacking = true;
        hasShot = false; // 발사 플래그 초기화
        attackCooldownTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);

        if (movement != null) movement.StopMovement();
        if (navAgent != null) navAgent.updateRotation = false;

        // TransformExtensions 툴 활용 (조준)
        Vector3 lookDir = transform.FlatDirectionTo(targetPosition);
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        if (animator != null) animator.SetTrigger(AttackHash);
    }

    private void SpawnBall()
    {
        if (ballPrefab == null) return;
        float finalDamage = status != null ? status.GetFinalDamageAndConsumeBuff() : player.ATK;

        // 공 스폰
        Runner.Spawn(ballPrefab, shootPoint.position, transform.rotation, Object.InputAuthority, (runner, spawnedBall) =>
        {
            if (spawnedBall.TryGetComponent(out NetworkBall ball))
            {
                ball.Init(finalDamage, Object.InputAuthority, player.TeamID, status);
            }
        });
    }

    private void EndAttackSequence()
    {
        IsAttacking = false;
        if (navAgent != null) navAgent.updateRotation = true;
    }
}
