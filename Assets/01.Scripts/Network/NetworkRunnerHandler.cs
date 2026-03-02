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

    private Transform[] blueSpawnPoints; 
    private Transform[] redSpawnPoints;  

    /// <summary>
    /// [추가] PlayGame 씬 로드 시 씬에 배치된 스폰 포인트들을 주입받습니다.
    /// </summary>
    public void SetSpawnPoints(Transform[] blue, Transform[] red)
    {
        blueSpawnPoints = blue;
        redSpawnPoints = red;
        Debug.Log("[Network] Spawn Points registered from the current scene.");
    }

    [Header("Session Settings")]
    [SerializeField] private string defaultSessionName = "DodgeballRoom";

    private NetworkRunner networkRunner;
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public async void StartGame(GameMode mode, string customRoomName)
    {
        if (networkRunner != null) return;

        // [수정] 4자리 숫자 조합의 랜덤 세션 이름 생성 (예: 0123, 7788)
        string finalSessionName = customRoomName;
        
        if (mode == GameMode.Host && string.IsNullOrWhiteSpace(customRoomName))
        {
            finalSessionName = UnityEngine.Random.Range(0, 10000).ToString("D4");
        }
        else if (string.IsNullOrWhiteSpace(customRoomName))
        {
            finalSessionName = defaultSessionName;
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
        
        if (result.Ok == false)
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
    /// [핵심] 특정 플레이어를 팀에 맞는 스폰 포인트에 즉시 생성합니다. (인스펙터 참조 방식)
    /// </summary>
    public void SpawnPlayerAtPoint(PlayerRef player, int charIndex, int teamId)
    {
        if (networkRunner == null || networkRunner.IsServer == false) return;
        
        // 이미 소환된 캐릭터가 있다면 중복 생성 방지
        if (spawnedCharacters.ContainsKey(player) == true) return;

        // 1. 해당 팀의 소환된 인원 확인
        int teamMemberCount = spawnedCharacters.Values.Count(obj => obj.GetComponent<Player>().TeamID == teamId);
        
        // 2. 인스펙터에서 할당된 포인트 리스트에서 적절한 위치 선택
        Transform[] targetPoints = (teamId == 0) ? blueSpawnPoints : redSpawnPoints;
        int pointIndex = Mathf.Clamp(teamMemberCount, 0, targetPoints.Length - 1);
        
        Transform targetTransform = targetPoints[pointIndex];
        Vector3 pos = (targetTransform != null) ? targetTransform.position : Vector3.zero;
        Quaternion rot = (targetTransform != null) ? targetTransform.rotation : Quaternion.identity;

        // 블루팀(0)은 상대 진영을 바라보게 보정 (포인트의 방향이 정방향일 경우)
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
        Debug.Log($"[Network] Spawned Player {player.PlayerId} at {targetTransform.name} (Team: {teamId})");
    }

    #region INetworkRunnerCallbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer == true && LobbyManager.Instance != null)
        {
            // [수정] 소환된 캐릭터 수가 아니라, 로비 데이터에 등록된 인원수를 기준으로 팀을 배정합니다 (더 정확함)
            int assignedTeam = LobbyManager.Instance.PlayerDataDic.Count % 2;
            
            Debug.Log($"[Network] Player {player.PlayerId} joined. Assigned Team: {(assignedTeam == 0 ? "Blue" : "Red")}");
            LobbyManager.Instance.AddPlayerData(player, assignedTeam);
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
