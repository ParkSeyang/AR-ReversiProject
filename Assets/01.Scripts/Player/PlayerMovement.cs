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
        ApplyAreaMaskByTeam(); // [추가] 팀에 따른 이동 가능 구역 설정

        // [핵심 수정] 서버(Host)이거나 본인 캐릭터(Input Authority)인 경우에만 NavMesh Agent를 활성화함
        // 서버에서 꺼버리면 클라이언트가 장애물을 뚫고 가는 것을 서버가 물리적으로 막지 못함
        if (Object.HasInputAuthority == false && Runner.IsServer == false)
        {
            navAgent.enabled = false;
        }
    }

    /// <summary>
    /// [추가] 플레이어의 팀ID를 기반으로 NavMesh Area Mask를 동적으로 할당합니다.
    /// </summary>
    private void ApplyAreaMaskByTeam()
    {
        if (navAgent == null || player == null) return;

        // 기본적으로 모든 팀은 Walkable(0번) 구역을 갈 수 있습니다.
        int mask = 1 << NavMesh.GetAreaFromName("Walkable");

        // 팀ID에 따라 전용 구역 추가 (Areas 탭에 등록된 이름과 정확히 일치해야 함)
        if (player.TeamID == 0) // Blue Team
        {
            int blueArea = NavMesh.GetAreaFromName("BlueArea");
            if (blueArea != -1) mask |= (1 << blueArea);
        }
        else if (player.TeamID == 1) // Red Team
        {
            int redArea = NavMesh.GetAreaFromName("RedArea");
            if (redArea != -1) mask |= (1 << redArea);
        }

        navAgent.areaMask = mask;
        Debug.Log($"[NavMesh] Player {player.PlayerName} (Team {player.TeamID}) AreaMask set to: {mask}");
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

        // [수정] NavMeshAgent가 직접 transform을 수정하지 못하게 막습니다. (순간이동 방지의 핵심)
        // 대신 경로 계산만 담당하게 하고, 실제 이동은 FixedUpdateNetwork에서 수동으로 처리합니다.
        navAgent.updatePosition = false; 
        navAgent.updateRotation = true;
    }

    public override void FixedUpdateNetwork()
    {
        // 입력 권한이 있는 로컬 플레이어만 이동 명령을 내림
        if (Object.HasInputAuthority == false) return;

        if (Object == null || Object.IsValid == false) return;

        // [추가] NavMeshAgent의 계산된 위치를 현재 transform 위치와 동기화시킵니다.
        // 이렇게 해야 에이전트가 캐릭터를 잃어버리지 않고 장애물을 정확히 판단합니다.
        if (navAgent != null && navAgent.isActiveAndEnabled == true)
        {
            navAgent.nextPosition = transform.position;
        }

        // 공격 중이면 이동 중지
        if (TryGetComponent<PlayerAttack>(out var attack) && attack.IsAttacking == true)
        {
            StopMovement();
            return;
        }

        // 실시간 속도 동기화
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

        // [추가] 경로 계산 결과를 transform에 수동으로 적용 (장애물 충돌 유지)
        if (navAgent != null && navAgent.isActiveAndEnabled == true && navAgent.hasPath)
        {
            // 에이전트가 가고자 하는 방향으로 transform을 실제로 이동시킵니다.
            // (Fusion의 위치 동기화와 호환되도록 transform.position을 직접 수정)
            transform.position = navAgent.nextPosition;
        }
    }

    private Vector3 lastPosition;

    public override void Render()
    {
        if (Object == null || Object.IsValid == false) return;

        // 애니메이션 처리
        if (animator != null)
        {
            float currentSpeed = 0f;
            
            if (Object.HasInputAuthority == true && navAgent != null && navAgent.isActiveAndEnabled == true)
            {
                // 로컬 플레이어: 에이전트의 실제 속도 사용
                currentSpeed = navAgent.velocity.magnitude;
            }
            else
            {
                // [수정] 리모트 플레이어: 이전 프레임과의 위치 차이를 통해 가상의 속도 계산 (슬라이딩 방지)
                Vector3 moveDelta = transform.position - lastPosition;
                currentSpeed = moveDelta.magnitude / Time.deltaTime;
                lastPosition = transform.position;
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
