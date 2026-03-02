using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 네트워크 세션 관리 및 시스템 프리팹 소환, 그리고 팀별 즉시 스폰 로직을 담당합니다.
/// </summary>
public class NetworkRunnerHandler : SingletonBase<NetworkRunnerHandler>, INetworkRunnerCallbacks
{
    [Header("Network Prefabs")]
    [SerializeField] private NetworkObject[] playerPrefabs = new NetworkObject[4]; 
    [SerializeField] private NetworkObject lobbyManagerPrefab; 
    [SerializeField] private NetworkObject matchManagerPrefab; 

    [Header("Session Settings")]
    [SerializeField] private string defaultSessionName = "DodgeballRoom";

    private NetworkRunner networkRunner;
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public async void StartGame(GameMode mode, string customRoomName)
    {
        if (networkRunner != null) return;

        string finalSessionName = string.IsNullOrWhiteSpace(customRoomName) ? defaultSessionName : customRoomName;
        
        if (mode == GameMode.Host && customRoomName == "")
        {
            finalSessionName = UnityEngine.Random.Range(1000, 9999).ToString();
        }

        networkRunner = gameObject.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = finalSessionName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await networkRunner.StartGame(startGameArgs);
        
        if (result.Ok == true)
        {
            if (mode == GameMode.Host)
            {
                if (lobbyManagerPrefab != null) networkRunner.Spawn(lobbyManagerPrefab);
                if (matchManagerPrefab != null) networkRunner.Spawn(matchManagerPrefab);
            }
        }
        else
        {
            Shutdown();
        }
    }

    public void Shutdown()
    {
        if (networkRunner != null)
        {
            networkRunner.Shutdown();
            networkRunner = null;
        }
    }

    /// <summary>
    /// [핵심] 특정 플레이어를 팀에 맞는 스폰 포인트에 즉시 생성합니다.
    /// </summary>
    public void SpawnPlayerAtPoint(PlayerRef player, int charIndex, int teamId)
    {
        if (networkRunner == null || networkRunner.IsServer == false) return;
        
        // 이미 소환된 캐릭터가 있다면 중복 생성 방지
        if (spawnedCharacters.ContainsKey(player) == true) return;

        // 1. 팀별 스폰 포인트 인덱스 결정
        // Team 0 (Blue): 포인트 1, 2 사용 / Team 1 (Red): 포인트 3, 4 사용
        int teamMemberCount = spawnedCharacters.Values.Count(obj => obj.GetComponent<Player>().TeamID == teamId);
        int spawnPointIndex = (teamId == 0) ? (teamMemberCount + 1) : (teamMemberCount + 3);

        // 2. 하이어라키에서 'SpawnPoint X' 찾기
        GameObject point = GameObject.Find("SpawnPoint " + spawnPointIndex);
        Vector3 pos = (point != null) ? point.transform.position : Vector3.zero;
        Quaternion rot = (point != null) ? point.transform.rotation : Quaternion.identity;

        // 블루팀은 레드팀(상대 진영)을 바라보게 회전 보정
        if (teamId == 0) rot *= Quaternion.Euler(0, 180, 0);

        // 3. 캐릭터 스폰
        int prefabIndex = Mathf.Clamp(charIndex, 0, playerPrefabs.Length - 1);
        NetworkObject spawnedObject = networkRunner.Spawn(playerPrefabs[prefabIndex], pos, rot, player);
        
        if (spawnedObject.TryGetComponent(out Player playerComponent))
        {
            playerComponent.TeamID = teamId;
            playerComponent.SelectedCharIndex = prefabIndex;
        }

        spawnedCharacters[player] = spawnedObject;
        Debug.Log($"[Network] Spawned Player {player.PlayerId} at SpawnPoint {spawnPointIndex} (Team: {teamId})");
    }

    #region INetworkRunnerCallbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer == true)
        {
            // 인원수에 맞춰 블루(0), 레드(1) 순차 배정
            int assignedTeam = spawnedCharacters.Count % 2;
            if (LobbyManager.Instance != null)
            {
                LobbyManager.Instance.AddPlayerData(player, assignedTeam);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            if (networkObject != null) runner.Despawn(networkObject);
            spawnedCharacters.Remove(player);
        }

        if (runner.IsServer == true && LobbyManager.Instance != null)
        {
            LobbyManager.Instance.RemovePlayerData(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (Player.Instance != null && Player.Instance.TryGetComponent(out PlayerInput playerInput))
        {
            input.Set(playerInput.GetNetworkInput());
            playerInput.ResetInputFlags();
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { spawnedCharacters.Clear(); networkRunner = null; }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}
