using Fusion;
using UnityEngine;

namespace Study.Fusion
{
    // 여기에 선언된 정보는 프로젝트의 네트워크로 동기화 되어야할 입력 Data라고 정의한다.
    // 아래처럼 INetworkInput을 상속받은 Data를 기반으로 정보(상태)를 동기화
    
    public struct NetworkInputData : INetworkInput
    {
        public const byte MOUSE_BUTTON_0 = 1;
        public const byte MOUSE_BUTTON_1 = 2;

        public NetworkButtons Buttons;  // 공들을 발사해볼건데 그때 사용할것입니다.
        public Vector3 Direction;
    }
}