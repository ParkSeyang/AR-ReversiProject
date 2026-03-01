using UnityEngine;

namespace Study.Examples.Fusion
{
    public class EventTestScript : MonoBehaviour
    {
        // 씬에서 캐릭터를 수동으로 할당하거나, 
        // 런타임에 소환된 캐릭터를 찾기 위해 FindObjectOfType을 사용할 수 있습니다.
        private PlayerController GetLocalPlayer()
        {
            // 모든 플레이어 중 내 캐릭터(InputAuthority)를 찾습니다.
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.Object.HasInputAuthority) return p;
            }
            return null;
        }

        // Hit 버튼과 연결할 메서드
        public void OnHitButtonClicked()
        {
            var player = GetLocalPlayer();
            if (player != null)
            {
                player.PlayHit();
                Debug.Log("Hit Animation Triggered!");
            }
            else
            {
                Debug.LogWarning("로컬 플레이어를 찾을 수 없습니다.");
            }
        }

        // Die 버튼과 연결할 메서드
        public void OnDieButtonClicked()
        {
            var player = GetLocalPlayer();
            if (player != null)
            {
                player.PlayDie();
                Debug.Log("Die Animation Triggered!");
            }
            else
            {
                Debug.LogWarning("로컬 플레이어를 찾을 수 없습니다.");
            }
        }
    }
}
