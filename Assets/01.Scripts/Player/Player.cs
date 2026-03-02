using UnityEngine;
using Fusion;

/// <summary>
/// 플레이어의 데이터와 네트워크 상태를 관리합니다. 
/// Firebase Auth 정보 및 로컬 저장소 정보를 단계적으로 검사하여 닉네임을 확정합니다.
/// </summary>
public class Player : NetworkBehaviour
{
    public static Player Instance { get; private set; }

    [Header("Player Stats")]
    [Networked] public float HP { get; set; }
    [Networked] public float MaxHP { get; set; }
    [Networked] public float ATK { get; set; }
    [Networked] public float MoveSpeed { get; set; }

    [Header("Identity")]
    [Networked] public string PlayerName { get; set; }
    [Networked] public int TeamID { get; set; }
    [Networked] public bool IsReady { get; set; }
    [Networked] public int SelectedCharIndex { get; set; }

    // 닉네임 저장 키 상수 정의
    private const string NICKNAME_KEY_PRODUCTION = "LocalPlayerName";
    private const string NICKNAME_KEY_LEGACY = "PlayerNickName";
    private const string CHARACTER_KEY = "SelectedCharIndex";

    public override void Spawned()
    {
        if (Object.HasInputAuthority == true)
        {
            Instance = this;
            
            // [방어 로직] 3단계 닉네임 결정 프로세스
            string finalName = DetermineFinalNickname();
            int localCharIndex = PlayerPrefs.GetInt(CHARACTER_KEY, 0);
            
            RPC_SetInitialPlayerData(finalName, localCharIndex);
            
            Debug.Log($"[Network] Player Spawned with Nickname: {finalName}");
        }

        if (HasStateAuthority == true)
        {
            InitializePlayerStats();
        }
    }

    /// <summary>
    /// 우선순위에 따라 유효한 닉네임을 찾아 반환합니다.
    /// </summary>
    private string DetermineFinalNickname()
    {
        // 1순위: Firebase Auth의 실제 동기화된 이름
        if (AuthManager.Instance.IsFirebaseInitialized == true)
        {
            string firebaseName = AuthManager.Instance.DisplayName;
            if (string.IsNullOrEmpty(firebaseName) == false && firebaseName != "Guest")
            {
                return firebaseName;
            }
        }

        // 2순위: 현재 우리 프로젝트의 로컬 저장소 이름
        string productionName = PlayerPrefs.GetString(NICKNAME_KEY_PRODUCTION, string.Empty);
        if (string.IsNullOrEmpty(productionName) == false)
        {
            return productionName;
        }

        // 3순위: 동료가 사용하던 레거시 로컬 저장소 이름
        string legacyName = PlayerPrefs.GetString(NICKNAME_KEY_LEGACY, string.Empty);
        if (string.IsNullOrEmpty(legacyName) == false)
        {
            return legacyName;
        }

        // 최종: 모든 정보가 없을 때의 기본값
        return "Player_" + Random.Range(1000, 9999);
    }

    #region RPCs (Input Authority -> State Authority)

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetInitialPlayerData(string newName, int characterIndex)
    {
        PlayerName = newName;
        SelectedCharIndex = characterIndex;
        Debug.Log($"[Server] Synchronized Player: {newName} (Ref: {Object.InputAuthority})");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool readyStateValue)
    {
        IsReady = readyStateValue;
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.RPC_SetReady(Object.InputAuthority, readyStateValue);
        }
    }

    #endregion

    private void InitializePlayerStats()
    {
        MaxHP = 100f;
        HP = MaxHP;
        ATK = 35.0f;       
        MoveSpeed = 10.0f; 
    }

    public void SetHP(float newHPValue)
    {
        if (HasStateAuthority == true)
        {
            HP = Mathf.Clamp(newHPValue, 0, MaxHP);
        }
    }

    public override void Render() { }
}
