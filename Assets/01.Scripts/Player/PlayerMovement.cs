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
        if (Object == null || Object.IsValid == false) return;

        // [중요] 모든 유저는 입력 데이터를 수집하지만, 실제 이동 처리는 서버 권한자만 수행합니다.
        if (GetInput(out NetworkInputData inputData))
        {
            // 서버(Host)에서만 NavMesh 목적지를 갱신
            if (HasStateAuthority == true && navAgent != null && navAgent.isActiveAndEnabled == true)
            {
                if (inputData.Buttons.IsSet(NetworkInputData.BUTTON_MOVE))
                {
                    Server_SetMoveDestination(inputData.ClickPosition);
                }
            }
        }

        // --- 서버 전용 이동 업데이트 로직 ---
        if (HasStateAuthority == true && navAgent != null && navAgent.isActiveAndEnabled == true)
        {
            // 공격 중이면 이동 중지
            if (TryGetComponent<PlayerAttack>(out var attack) && attack.IsAttacking == true)
            {
                StopMovement();
            }

            // 실시간 속도 동기화
            if (player != null) navAgent.speed = player.MoveSpeed;

            // NavMeshAgent의 계산된 위치를 Transform에 적용 (서버가 위치의 주도권을 가짐)
            if (navAgent.hasPath == true)
            {
                // 에이전트가 계산한 다음 위치를 transform에 직접 주입
                // (이 좌표가 NetworkTransform을 통해 모든 클라이언트에게 부드럽게 전파됨)
                navAgent.nextPosition = transform.position; 
                transform.position = navAgent.nextPosition;
            }
        }
    }

    /// <summary>
    /// 서버에서 NavMesh 목적지를 설정합니다.
    /// </summary>
    private void Server_SetMoveDestination(Vector3 targetPosition)
    {
        if (navAgent == null || navAgent.isOnNavMesh == false) return;

        // 목적지가 유효한 NavMesh 위인지 확인 후 설정
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 10f, navAgent.areaMask))
        {
            navAgent.SetDestination(hit.position);
        }
        else
        {
            navAgent.SetDestination(targetPosition);
        }
    }

    private Vector3 lastPosition;

    public override void Render()
    {
        if (Object == null || Object.IsValid == false) return;

        // 애니메이션 처리 (로컬/리모트 공통)
        if (animator != null)
        {
            // 이전 프레임과의 위치 차이를 통해 실제 이동 속도 계산 (가장 정확하고 떨림 없음)
            Vector3 moveDelta = transform.position - lastPosition;
            float currentSpeed = moveDelta.magnitude / Time.deltaTime;
            lastPosition = transform.position;
            
            animator.SetFloat(SpeedHash, currentSpeed > 0.1f ? currentSpeed : 0f);
        }
    }

    /// <summary>
    /// 캐릭터의 모든 이동을 즉시 중단합니다. (서버 전용)
    /// </summary>
    public void StopMovement()
    {
        if (HasStateAuthority == true && navAgent != null && navAgent.isActiveAndEnabled == true && navAgent.isOnNavMesh == true)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }
    }

    // --- 애니메이션 이벤트 수신용 (Footstep 경고 방지) ---
    public void FootL() { }
    public void FootR() { }
}
