using UnityEngine;
using Fusion;

namespace Youstianus
{
    /// <summary>
    /// 던져진 무기(검 등)의 이동과 회전을 담당하는 네트워크 투사체 스크립트입니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SwordProjectile : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifeTime = 3f;
        
        [Header("Rotation Settings")]
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 0, 720f); 

        [Networked] public ETeam OwnerTeam { get; set; } // 던진 사람의 팀 정보
        [Networked] private TickTimer lifeTimer { get; set; }

        public override void Spawned()
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);

            // [추가] 모든 자식 오브젝트의 음수 스케일 제거 (BoxCollider 경고 해결)
            FixNegativeScale(transform);

            // 물리 설정 (Trigger 충돌 보장)
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void FixNegativeScale(Transform t)
        {
            Vector3 s = t.localScale;
            t.localScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            
            foreach (Transform child in t)
            {
                FixNegativeScale(child);
            }
        }

        public override void FixedUpdateNetwork()
        {
            // 전방으로 이동
            transform.position += transform.forward * speed * Runner.DeltaTime;

            // 시각적 회전 연출
            transform.Rotate(rotationSpeed * Runner.DeltaTime);

            // 시간이 다 되면 파괴
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

                    // [중요] 같은 팀이면 무시 (자폭 방지)
                    if (targetPc.Team == OwnerTeam) return;

                    targetData.TakeDamage(20); // 기본 데미지 20 적용
                    targetPc.RPC_PlayHit();
                    
                    Debug.Log($"[Sword] Hit Enemy: {other.name}!");
                    Runner.Despawn(Object);
                }
            }
        }
    }
}
