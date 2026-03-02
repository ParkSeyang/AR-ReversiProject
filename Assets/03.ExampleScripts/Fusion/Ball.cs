using Fusion;
using UnityEngine.Rendering;

namespace Study.Examples.Fusion
{
    public class Ball : NetworkBehaviour
    {
        // Tick(네트워크)에 의한 타이머 입니다.
        [Networked] private TickTimer life { get; set; }
        
        public void Init()
        {
            // 초기화하면서 life를 설정해줍니다.
            life = TickTimer.CreateFromSeconds(Runner, 5.0f);
            
        }

        public override void FixedUpdateNetwork()
        {
            // Runner에게 물어봅니다. 나의 life가 만료가 되었니?
            // Runner는 Tick기반의 모든것을 관리합니다.
            // 아래의 if문은 Runner를 통해 정확하게 나의 life가 만료가 되었는지
            // 물어보는 트리거역할의 로직 입니다.
            if (life.Expired(Runner))
            {
                // Runner야 나를 Despawn처리해줘. (Runner가 알아서 맞춰서 없애줌.)
                Runner.Despawn(Object);
            }
            else
            {
                transform.position += 5f * transform.forward * Runner.DeltaTime;
            }
            
        }
    }
}