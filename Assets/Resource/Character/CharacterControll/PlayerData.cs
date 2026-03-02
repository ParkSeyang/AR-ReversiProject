using UnityEngine;
using Fusion;

namespace Youstianus
{
    /// <summary>
    /// 플레이어의 체력, 공격력 등 핵심 데이터를 관리하고 네트워크 동기화를 담당합니다.
    /// </summary>
    public class PlayerData : NetworkBehaviour
    {
        [Header("Health Settings")]
        [Networked] public int MaxHP { get; set; } = 100;
        [Networked] public int CurrentHP { get; set; }
        [Networked] public bool IsDead { get; set; } // 사망 여부 동기화

        [Header("Combat Settings")]
        [Networked] public int AttackDamage { get; set; } = 20;
        [Networked] public float AttackCooldown { get; set; } = 1.0f;

        [Header("Movement Settings")]
        [Networked] public float MoveSpeed { get; set; } = 10f;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CurrentHP = MaxHP;
                IsDead = false;
            }
        }

        public void TakeDamage(int damage)
        {
            if (!HasStateAuthority || IsDead) return;

            CurrentHP = Mathf.Clamp(CurrentHP - damage, 0, MaxHP);
            Debug.Log($"[PlayerData] {Object.name} took {damage} damage. HP: {CurrentHP}/{MaxHP}");

            if (CurrentHP <= 0)
            {
                IsDead = true;
                var pc = GetComponent<PlayerController>();
                if (pc != null) pc.PlayDie();
            }
        }
    }
}
