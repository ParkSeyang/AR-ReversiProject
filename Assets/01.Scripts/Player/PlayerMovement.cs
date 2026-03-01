using UnityEngine;
using UnityEngine.AI;
using Fusion;

/// <summary>
/// 로컬 플레이어는 NavMesh Agent로 이동하고, 
/// 리모트 플레이어는 Network Transform에 의해 위치만 동기화되도록 관리합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Tuning")]
    [SerializeField] private float rotationSpeed = 3000f;
    [SerializeField] private float acceleration = 200f;

    private NavMeshAgent navAgent;
    private Animator animator;
    private Player player;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public override void Spawned()
    {
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        player = GetComponent<Player>();

        ConfigureNavMeshAgent();

        // [핵심] 본인 캐릭터(Input Authority)가 아니라면 NavMesh Agent를 비활성화함
        // 리모트 플레이어는 Network Transform이 전달하는 위치 정보만 따르게 하여 떨림을 방지함
        if (Object.HasInputAuthority == false)
        {
            navAgent.enabled = false;
        }
    }

    private void ConfigureNavMeshAgent()
    {
        if (navAgent == null) return;

        if (player != null)
        {
            navAgent.speed = player.MoveSpeed;
        }

        navAgent.angularSpeed = rotationSpeed;
        navAgent.acceleration = acceleration;
        navAgent.stoppingDistance = 0.1f;

        // 에이전트 본연의 이동 및 회전 기능을 사용함
        navAgent.updatePosition = true;
        navAgent.updateRotation = true;
    }

    public override void FixedUpdateNetwork()
    {
        // 입력 권한이 있는 로컬 플레이어만 이동 명령을 내림
        if (Object.HasInputAuthority == false) return;

        if (Object == null || Object.IsValid == false) return;

        // 공격 중이면 이동 중지
        if (TryGetComponent<PlayerAttack>(out var attack) && attack.IsAttacking == true)
        {
            StopMovement();
            return;
        }

        // [중요] 실시간 속도 동기화: Player.cs의 MoveSpeed 변수를 NavMeshAgent에 대입
        if (player != null && navAgent != null && navAgent.isActiveAndEnabled == true)
        {
            navAgent.speed = player.MoveSpeed;
        }

        // 입력 수집 및 이동 실행
        if (GetInput(out NetworkInputData inputData))
        {
            if (inputData.Buttons.IsSet(NetworkInputData.BUTTON_MOVE))
            {
                MoveTo(inputData.ClickPosition);
            }
        }
    }

    public override void Render()
    {
        if (Object == null || Object.IsValid == false) return;

        // 애니메이션 처리
        if (animator != null)
        {
            float currentSpeed = 0f;
            
            // 로컬 플레이어인 경우 에이전트의 속도를 직접 참조
            if (Object.HasInputAuthority == true && navAgent != null && navAgent.isActiveAndEnabled == true)
            {
                currentSpeed = navAgent.velocity.magnitude;
            }
            else
            {
                // 리모트 플레이어(Proxy)는 Network Transform에 의해 위치가 이동하므로,
                // 시각적인 보간(Interpolation) 결과나 이동 변화량을 기반으로 애니메이션 재생
                // 여기서는 간단하게 속도 파라미터를 0으로 두거나, 추후 NetworkTransform의 속도값으로 보완 가능
            }
            
            animator.SetFloat(SpeedHash, currentSpeed > 0.1f ? currentSpeed : 0f);
        }
    }

    /// <summary>
    /// 지정된 목적지로 NavMesh를 사용하여 이동 명령을 내립니다. (로컬 전용)
    /// </summary>
    public Vector3 MoveTo(Vector3 targetPosition)
    {
        if (Object.HasInputAuthority == false) return transform.position;
        if (navAgent == null || navAgent.isActiveAndEnabled == false) return transform.position;

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f) return transform.position;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 100f, navAgent.areaMask))
        {
            navAgent.SetDestination(hit.position);
            return hit.position;
        }
        else
        {
            navAgent.SetDestination(targetPosition);
            return targetPosition;
        }
    }

    /// <summary>
    /// 캐릭터의 모든 이동을 즉시 중단합니다.
    /// </summary>
    public void StopMovement()
    {
        if (navAgent != null && navAgent.isActiveAndEnabled == true && navAgent.isOnNavMesh == true)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }
    }

    // --- 애니메이션 이벤트 수신용 (Footstep 경고 방지) ---
    public void FootL() { }
    public void FootR() { }
}
