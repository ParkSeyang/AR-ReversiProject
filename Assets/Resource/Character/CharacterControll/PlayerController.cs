using UnityEngine;
using UnityEngine.AI;
using Fusion;

namespace Youstianus
{
    public enum ETeam { Blue, Red }

    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;       // 8 -> 10으로 상향
        [SerializeField] private float rotationSpeed = 3000f; // 2000 -> 3000 (즉시 회전)
        [SerializeField] private float acceleration = 200f;   // 100 -> 200 (가속 시간 삭제 수준)

        [Networked] public ETeam Team { get; set; }
        [Networked] public bool IsAttacking { get; set; } // 공격 상태 추가

        private NavMeshAgent navAgent;
        private Animator animator;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private bool hasShotInCurrentAttack = false;

        public override void Spawned()
        {
            navAgent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            
            ConfigureNavMeshAgent();
            ApplyTeamAreaMask();
        }

        private void ConfigureNavMeshAgent()
        {
            if (navAgent == null) return;
            
            navAgent.speed = moveSpeed;
            navAgent.angularSpeed = rotationSpeed;
            navAgent.acceleration = acceleration;
            navAgent.stoppingDistance = 0.1f;
            
            navAgent.updateRotation = true;
            navAgent.updatePosition = true;
        }

        public override void Render()
        {
            if (Object == null || !Object.IsValid) return;

            if (animator != null && navAgent != null)
            {
                float currentSpeed = navAgent.velocity.magnitude;
                animator.SetFloat(SpeedHash, currentSpeed > 0.1f ? currentSpeed : 0f);
            }

            // 자동 감지 로직 호출
            CheckAttackProgress();
        }

        private void CheckAttackProgress()
        {
            if (!IsAttacking || animator == null) return;

            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // "Attack"은 Animator의 State 이름입니다.
            if (stateInfo.IsName("Attack"))
            {
                // 약 40% 지점에서 발사 (수동 이벤트가 없을 때의 보험)
                if (!hasShotInCurrentAttack && stateInfo.normalizedTime >= 0.4f)
                {
                    Shoot();
                    hasShotInCurrentAttack = true;
                }

                // 95% 지점에서 종료 (수동 이벤트가 없을 때의 보험)
                if (stateInfo.normalizedTime >= 0.95f)
                {
                    OnAttackEnd();
                }
            }
        }

        public Vector3 MoveTo(Vector3 targetPosition)
        {
            // 공격 중이면 이동 명령 무시
            if (IsAttacking) return transform.position;

            if (Object.HasInputAuthority || Object.HasStateAuthority)
            {
                if (navAgent != null)
                {
                    // 너무 가까운 거리는 무시하고 현재 위치 반환
                    if (Vector3.Distance(transform.position, targetPosition) < 0.1f) return transform.position;

                    // 팀 구역(areaMask)을 고려하여 가장 가까운 유효 위치를 찾습니다.
                    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 100f, navAgent.areaMask))
                    {
                        navAgent.SetDestination(hit.position);
                        return hit.position; // 보정된 위치 반환
                    }
                    else
                    {
                        navAgent.SetDestination(targetPosition);
                        return targetPosition;
                    }
                }
            }
            return targetPosition;
        }

        public void Attack()
        {
            if (Object.HasInputAuthority)
            {
                // [개선] 공격 가능 여부와 상관없이 Q를 누르는 즉시 마우스 방향을 바라보게 하여 반응성을 높입니다.
                FaceMouseDirection();

                if (IsAttacking) return; // 중복 공격 방지

                // 공격 상태로 전환하고 즉시 이동을 멈춥니다.
                IsAttacking = true;
                hasShotInCurrentAttack = false; 

                if (navAgent != null)
                {
                    navAgent.ResetPath();
                    navAgent.velocity = Vector3.zero; // 물리적 관성 제거
                    navAgent.updateRotation = false; // 공격 중 NavMeshAgent가 회전을 제어하지 못하도록 차단
                }

                if (animator != null) animator.SetTrigger("Attack");
                Debug.Log($"{Team} Team Attack (Auto Detection)!");
            }
        }

        private void FaceMouseDirection()
        {
            // 마우스 클릭 지점(또는 평면 교차점)을 향해 즉시 회전합니다.
            if (Camera.main == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDir = (hitPoint - transform.position).normalized;
                lookDir.y = 0;
                
                if (lookDir != Vector3.zero)
                {
                    // [개선] 쿼터니언을 사용하여 즉시 회전(Snap)시킵니다.
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }

        // --- 애니메이션 이벤트 수신용 메서드 ---
        public void Shoot()
        {
            // 애니메이션에서 '던지는 프레임'에 도달했을 때 호출됩니다.
            Debug.Log($"{Team} Team SHOOT! (Projectile Spawned)");
        }

        // 공격 애니메이션이 완전히 끝날 때 호출할 이벤트
        public void OnAttackEnd()
        {
            IsAttacking = false;
            if (navAgent != null) navAgent.updateRotation = true; // 이동 시 다시 회전 허용
            Debug.Log("Attack Finished - Movement Enabled.");
        }

        public void PlayHit()
        {
            if (animator != null) animator.SetTrigger("Hit");
        }

        public void PlayDie()
        {
            if (animator != null) animator.SetTrigger("Die");
        }

        public void FootL() { }
        public void FootR() { }

        private void ApplyTeamAreaMask()
        {
            if (navAgent == null) return;
            int walkableMask = 1 << 0;
            int teamMask = (Team == ETeam.Blue) ? (1 << 3) : (1 << 4); 
            navAgent.areaMask = walkableMask | teamMask;
        }
    }
}
