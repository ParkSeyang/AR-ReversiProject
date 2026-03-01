using UnityEngine;
using Fusion;

public class Player : NetworkBehaviour
{
    // 로컬 플레이어(본인)에 대한 접근을 용이하게 하기 위한 정적 인스턴스
    public static Player Instance { get; private set; }

    [Header("Player Stats")]
    [Networked] public float HP { get; set; }
    [Networked] public float MaxHP { get; set; }
    [Networked] public float ATK { get; set; }       // 캐릭터의 기본 공격력
    [Networked] public float MoveSpeed { get; set; } // 캐릭터의 기본 이동 속도 능력치

    [Header("Identity")]
    [Networked] public string PlayerName { get; set; }
    [Networked] public int TeamID { get; set; } // 0: Blue, 1: Red (기획에 따라 확장 가능)

    public override void Spawned()
    {
        // 입력 권한(Input Authority)을 가진 객체가 본인의 로컬 캐릭터임
        if (Object.HasInputAuthority == true)
        {
            Instance = this;
            Debug.Log($"Local Player Spawned: {Object.InputAuthority}");
            
            // 본인 캐릭터가 스폰되면, 로컬에 저장된 이름을 서버로 전송하여 세팅
            string myName = PlayerPrefs.GetString("LocalPlayerName", "Player_" + Random.Range(1000, 9999));
            RPC_SetPlayerName(myName);
        }

        // 서버(State Authority)에서 초기 스탯 설정
        if (HasStateAuthority == true)
        {
            InitializeStats();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetPlayerName(string newName)
    {
        PlayerName = newName;
        Debug.Log($"[Server] Player name set to: {newName}");
    }

    /// <summary>
    /// 게임 시작 시 기본 능력치를 초기화합니다.
    /// </summary>
    private void InitializeStats()
    {
        MaxHP = 100f;
        HP = MaxHP;
        ATK = 35.0f;       // 기획서 사양: 기본 공격력 35
        MoveSpeed = 10.0f; // 동료 작업물(PlayerController)의 상향된 수치 반영
    }

    /// <summary>
    /// 네트워크 상에서 체력을 안전하게 수정합니다. (서버 전용)
    /// </summary>
    public void SetHP(float newHP)
    {
        if (HasStateAuthority == true)
        {
            HP = Mathf.Clamp(newHP, 0, MaxHP);
        }
    }

    /// <summary>
    /// 네트워크 상에서 이동 속도를 안전하게 수정합니다. (서버 전용)
    /// </summary>
    public void SetMoveSpeed(float newSpeed)
    {
        if (HasStateAuthority == true)
        {
            MoveSpeed = Mathf.Max(0, newSpeed);
        }
    }

    public override void Render()
    {
        // 시각적인 보간(Interpolation)이나 UI 갱신 로직이 들어갈 자리입니다.
    }
}
