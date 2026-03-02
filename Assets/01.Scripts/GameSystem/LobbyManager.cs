using UnityEngine;
using Fusion;
using System.Collections;

public struct LobbyPlayerData : INetworkStruct
{
    public bool IsReady;
    public int CharIndex;
    public int TeamID;
    public NetworkString<_16> Nickname;
}

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Networked, Capacity(4)]
    public NetworkDictionary<PlayerRef, LobbyPlayerData> PlayerDataDic => default;

    [Networked] public bool IsMatchStarting { get; set; }

    public override void Spawned()
    {
        Instance = this;
        Debug.Log("[LobbyManager] 인게임 준비 상태 동기화 시스템 활성화.");
    }

    #region RPCs

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetReady(PlayerRef player, bool isReady)
    {
        if (PlayerDataDic.ContainsKey(player) == true)
        {
            var data = PlayerDataDic[player];
            data.IsReady = isReady;
            PlayerDataDic.Set(player, data);
            CheckAllPlayersReady();
        }
    }

    /// <summary>
    /// 유저가 정보를 보고하면 서버가 딕셔너리에 저장하고 즉시 캐릭터를 소환합니다.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetCharacterAndName(PlayerRef player, int index, string name)
    {
        if (PlayerDataDic.ContainsKey(player) == true)
        {
            var data = PlayerDataDic[player];
            data.CharIndex = index;
            data.Nickname = name;
            PlayerDataDic.Set(player, data);

            // [추가] 호스트 권한으로 즉시 캐릭터 소환 시도
            if (HasStateAuthority == true && NetworkRunnerHandler.Instance != null)
            {
                NetworkRunnerHandler.Instance.SpawnPlayerAtPoint(player, index, data.TeamID);
            }
        }
    }

    public void AddPlayerData(PlayerRef player, int teamId)
    {
        if (HasStateAuthority == false) return;
        if (PlayerDataDic.ContainsKey(player) == false)
        {
            PlayerDataDic.Add(player, new LobbyPlayerData { IsReady = false, TeamID = teamId, Nickname = "Joining..." });
        }
    }

    public void RemovePlayerData(PlayerRef player)
    {
        if (HasStateAuthority == false) return;
        if (PlayerDataDic.ContainsKey(player) == true) PlayerDataDic.Remove(player);
    }

    #endregion

    private void CheckAllPlayersReady()
    {
        if (HasStateAuthority == false || IsMatchStarting == true) return;
        int totalCount = PlayerDataDic.Count;
        if (totalCount < 2) return;

        bool allReady = true;
        foreach (var kvp in PlayerDataDic)
        {
            if (kvp.Value.IsReady == false) { allReady = false; break; }
        }

        if (allReady == true) IsMatchStarting = true;
    }
}
