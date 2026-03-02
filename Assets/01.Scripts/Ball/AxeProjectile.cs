using UnityEngine;
using Fusion;

namespace Youstianus
{
    [RequireComponent(typeof(Rigidbody))]
    public class AxeProjectile : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 18f;
        [SerializeField] private float lifeTime = 2.5f;
        
        [Header("Visual Settings")]
        [SerializeField] private Transform visualChild;      
        [SerializeField] private Vector3 spinAxis = new Vector3(1, 0, 0); 
        [SerializeField] private float rotationSpeed = 1000f;             

        [Networked] public ETeam OwnerTeam { get; set; } // 던진 사람의 팀 정보
        [Networked] private TickTimer lifeTimer { get; set; }

        public override void Spawned()
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            
            // 물리 설정 초기화 (Trigger 충돌을 위해 필요)
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true; // 코드로 이동시키므로 Kinematic
                rb.useGravity = false;
            }

            if (visualChild == null && transform.childCount > 0)
                visualChild = transform.GetChild(0);

            if (visualChild != null)
            {
                visualChild.localRotation = Quaternion.Euler(0, 0, -90f);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // 1. 직선 이동
            transform.position += transform.forward * speed * Runner.DeltaTime;

            // 2. 지정된 축으로 회전
            if (visualChild != null)
            {
                visualChild.Rotate(spinAxis, rotationSpeed * Runner.DeltaTime, Space.Self);
            }

            // 3. 수명 다하면 제거
            if (lifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!HasStateAuthority) return;

            if (other.CompareTag("Player"))
            {
                var targetPc = other.GetComponent<PlayerController>();
                var targetData = other.GetComponent<PlayerData>();

                if (targetPc != null && targetData != null)
                {
                    // [중요] 죽은 플레이어는 통과
                    if (targetData.IsDead) return;

                    // 같은 팀이면 무시 (자폭 방지)
                    if (targetPc.Team == OwnerTeam) return;

                    // 데미지 처리 및 피격 애니메이션
                    targetData.TakeDamage(20); // 기본 데미지 20 적용
                    targetPc.RPC_PlayHit();
                    
                    Debug.Log($"[Axe] Hit Enemy: {other.name}");
                    Runner.Despawn(Object);
                }
            }
        }
    }
}
