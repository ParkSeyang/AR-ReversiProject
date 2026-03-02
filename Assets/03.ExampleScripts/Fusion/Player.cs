using System;
using Fusion;
using UnityEngine;

namespace Study.Examples.Fusion
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private Ball prefabBall;

        [Networked] private TickTimer coolTime { get; set; }

        private NetworkCharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<NetworkCharacterController>();
        }

        // "Tick을 기반으로 호출되는 Update 이벤트 함수다." 라고 생각하시면 됩니다.
        public override void FixedUpdateNetwork()
        {
            // GetInput함수는 NetworkBehaviour에 정의 되어있는 함수 입니다.
            if (GetInput(out NetworkInputData data))
            {
                // 현재 Player의 타입은 논리적으로 두가지가 있습니다.(Local, Remote)
                // Fusion에서는 해당 타입들을 알아서 정의를 해놓습니다.
                // 그리고 권한에 의해 알아서 반응하도록 디자인 되어 있습니다.
                // 권한은 여러가지가 있지만 특정 상태의 변경권한이 있는 여부를 가지고
                // 오브젝트를 스폰하고 제어를 합니다.
                
                // HasStateAuthority 는 이녀석이 데이터를 변경할 수 있는 권한을 가지고 있니? 라고 물어봅니다.
                // PlayerRef에 의해서 보통 설정이 됩니다.
                // 값이나, Network객체의 스폰등이 상태 변경 권한이 있어야 가능합니다.
                
                // 상태변경 권한을 갖고 있으면서 && 쿨타임이 만료되거나 동작하지 않다면 코드블록을 실행하라.
                if (HasStateAuthority && coolTime.ExpiredOrNotRunning(Runner))
                {
                    // Set이 되어있으면서 NetworkInputData.MOUSE_BUTTON_0 값과 같다면
                    if (data.Buttons.IsSet(NetworkInputData.MOUSE_BUTTON_0))
                    {
                        coolTime = TickTimer.CreateFromSeconds(Runner, 0.5f);
                        
                        // 람다로 Ball을 스폰하기전에 초기화하는 로직을 주입 시켜줍시다.
                        // Runner가 특정한 오브젝트를 스폰하기 전에 아래의 람다를 실행시키고 스폰처리 합니다.
                        NetworkRunner.OnBeforeSpawned spawnBall = (runner, obj) =>
                        {
                            obj.GetComponent<Ball>().Init();
                        };
                        
                        // Ball을 스폰해봅시다.
                        Runner.Spawn(prefabBall,  // ball 프리팹(네트워크에 붙은) 
                            transform.position + transform.forward, // 위치 설정
                            Quaternion.LookRotation(transform.forward), // 회전 설정
                            Object.InputAuthority, // 입력 권한 설정(위치 업데이트용)
                            spawnBall); // 스폰되기 전에 호출할 함수 

                    }
                }


                data.Direction.Normalize();
                
                // 네트워크 tick 기반의 deltaTime을 사용 해야합니다.
                // 해당 deltaTime은 Fusion 엔진의 심장 역할을 하는 Runner가 갖고 있습니다.
                characterController.Move(5 * data.Direction * Runner.DeltaTime);
                
            }
            
        }
    }
}