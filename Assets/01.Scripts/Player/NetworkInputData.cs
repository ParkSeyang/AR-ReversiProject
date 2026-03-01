using Fusion;
using UnityEngine;

/// <summary>
/// 네트워크 상에서 동기화될 플레이어 입력 데이터를 정의합니다.
/// </summary>
public struct NetworkInputData : INetworkInput
{
    // 버튼 정의 (BitMask)
    public const int BUTTON_MOVE = 0;   // 마우스 우클릭 (이동)
    public const int BUTTON_ATTACK = 1; // Q 키 (공격)

    public NetworkButtons Buttons;
    
    // 마우스 클릭 시점의 월드 좌표 또는 조준 좌표
    public Vector3 ClickPosition;
}
