using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 네트워크 세션 관리 및 랜덤 팀 배정, 플레이어 스폰을 담당하는 핵심 핸들러입니다.
/// </summary>
public class NetworkRunnerHandler : SingletonBase<NetworkRunnerHandler>, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    [SerializeField] private NetworkPrefabRef[] playerCharacterPrefabs; 
    public Transform[] spawnPoints; 
    [SerializeField] private string sessionName = "DodgeballRoom";

    [Header("Game Management")]
    [SerializeField] private NetworkPrefabRef gameManagerPrefab;

    public class GameResult
    {
        public Youstianus.ETeam WinnerTeam;
        public List<string> WinnerNames = new List<string>();
    }
    public static GameResult LastResult { get; set; }

    private NetworkRunner networkRunner;
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    
    // [추가] 랜덤 팀 배정을 위한 매핑 딕셔너리
    private Dictionary<PlayerRef, Youstianus.ETeam> playerTeamMap = new Dictionary<PlayerRef, Youstianus.ETeam>();

    public int SelectedCharacterIndex { get; set; } = 0;

    public void SetSpawnPoints(Transform[] points) => spawnPoints = points;

    public event Action OnShutdownEvent;
    public event Action<int> OnPlayerCountChanged;

    public async System.Threading.Tasks.Task<bool> StartGame(GameMode mode, string customSessionName = "")
    {
        if (networkRunner != null) return false;
        if (!string.IsNullOrEmpty(customSessionName)) sessionName = customSessionName;

        GameObject runnerObj = new GameObject("PhotonNetworkRunner");
        networkRunner = runnerObj.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;
        networkRunner.AddCallbacks(this);

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = runnerObj.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await networkRunner.StartGame(startGameArgs);
        if (result.Ok) return true;
        
        if (networkRunner != null) Destroy(networkRunner.gameObject);
        networkRunner = null;
        return false;
    }

    public void Shutdown()
    {
        if (networkRunner != null) networkRunner.Shutdown();
        else OnShutdownEvent?.Invoke();
    }

    #region 라운드 및 팀 관리 로직

    public void DespawnAllPlayers()
    {
        if (networkRunner == null || !networkRunner.IsServer) return;

        // 1. 딕셔너리에 등록된 플레이어 제거
        foreach (var kvp in spawnedCharacters.ToList())
        {
            if (kvp.Value != null) networkRunner.Despawn(kvp.Value);
        }
        spawnedCharacters.Clear();

        // 2. [보강] 딕셔너리에 없더라도 씬에 남아있는 모든 PlayerController 제거 (더미 포함)
        var allPlayers = GameObject.FindObjectsByType<Youstianus.PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in allPlayers)
        {
            var no = pc.GetComponent<NetworkObject>();
            if (no != null && no.IsValid) networkRunner.Despawn(no);
        }
    }

    /// <summary>
    /// 매 라운드 시작 전 팀을 랜덤하게 재배정하고 모든 플레이어를 소환합니다.
    /// </summary>
    public void RespawnAll()
    {
        if (networkRunner == null || !networkRunner.IsServer) return;

        // 1. 팀 랜덤 배정 (4명 기준)
        AssignRandomTeams();

        // 2. 실제 플레이어 소환
        foreach (var player in networkRunner.ActivePlayers)
        {
            SpawnPlayer(networkRunner, player);
        }

        // 3. 부족한 인원만큼 더미 다시 소환
        SpawnDummies(networkRunner);
    }

    private void AssignRandomTeams()
    {
        var players = networkRunner.ActivePlayers.ToList();
        
        // 리스트 섞기 (Fisher-Yates Shuffle)
        for (int i = players.Count - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            var temp = players[i];
            players[i] = players[r];
            players[r] = temp;
        }

        playerTeamMap.Clear();
        // 1명일 경우 Blue, 그 외에는 절반씩 나눔 (1명: Blue 1, 2명: Blue 1/Red 1, 4명: Blue 2/Red 2)
        int halfCount = Mathf.Max(1, players.Count / 2);
        if (players.Count == 1) halfCount = 1; // 1명일 때는 무조건 Blue

        for (int i = 0; i < players.Count; i++)
        {
            Youstianus.ETeam team = (i < halfCount) ? Youstianus.ETeam.Blue : Youstianus.ETeam.Red;
            playerTeamMap.Add(players[i], team);
        }
    }

    #endregion

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount);
        
        // [복구] 2명이 모였을 때만 게임 시작 시퀀스 (GameManager 스폰 등은 OnSceneLoadDone에서 처리)
        if (runner.IsServer && runner.SessionInfo.PlayerCount >= 2)
        {
            Debug.Log("[Network] 2 Players joined. Waiting for lobby sequence to finish...");
        }
    }

    public void RequestSceneChange(string sceneName)
    {
        if (networkRunner != null && networkRunner.IsServer)
        {
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (sceneIndex != -1) networkRunner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedCharacters.ContainsKey(player)) return;

        // 랜덤 배정된 팀 정보 가져오기
        if (!playerTeamMap.TryGetValue(player, out Youstianus.ETeam assignedTeam))
        {
            assignedTeam = Youstianus.ETeam.Blue; // 예외 방지용 기본값
        }

        // [수정] 본인이면 선택한 캐릭터 인덱스 사용, 그 외(다른 클라이언트)는 일단 0번 (추후 동기화 필요)
        int prefabIndex = 0;
        if (player == runner.LocalPlayer)
        {
            prefabIndex = SelectedCharacterIndex;
        }

        // 팀 내 순서 계산 (해당 팀의 몇 번째 멤버인지)
        int teamMemberIndex = playerTeamMap.Where(x => x.Value == assignedTeam).ToList().FindIndex(x => x.Key == player);
        int spawnPointArrayIndex = (assignedTeam == Youstianus.ETeam.Blue) ? teamMemberIndex : teamMemberIndex + 2;
        
        Transform spawnPoint = (spawnPoints != null && spawnPoints.Length > spawnPointArrayIndex) ? spawnPoints[spawnPointArrayIndex] : null;
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = (assignedTeam == Youstianus.ETeam.Blue) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);

        // 선택한 프리팹으로 소환
        if (prefabIndex < 0 || prefabIndex >= playerCharacterPrefabs.Length) prefabIndex = 0;

        NetworkObject networkPlayerObject = runner.Spawn(playerCharacterPrefabs[prefabIndex], spawnPos, spawnRot, player, (runner, obj) => {
            var pc = obj.GetComponent<Youstianus.PlayerController>();
            if (pc != null)
            {
                pc.Team = assignedTeam;
                pc.SpawnPointIndex = spawnPointArrayIndex + 1;
            }
        });

        spawnedCharacters.Add(player, networkPlayerObject);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner.IsServer && SceneManager.GetActiveScene().name == "INgameScean")
        {
            if (GameObject.FindAnyObjectByType<Youstianus.GameManager>() == null && gameManagerPrefab.IsValid)
            {
                runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
            }
            
            // 부족한 인원만큼 더미 소환 (4명 기준)
            SpawnDummies(runner);
        }
    }

    private void SpawnDummies(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        int currentPlayers = runner.ActivePlayers.Count();
        int dummiesNeeded = 4 - currentPlayers;

        for (int i = 0; i < dummiesNeeded; i++)
        {
            int dummyIndex = currentPlayers + i;
            Youstianus.ETeam dummyTeam = (dummyIndex < 2) ? Youstianus.ETeam.Blue : Youstianus.ETeam.Red;
            
            // 더미 캐릭터도 다양하게 (0~3번 순환)
            int prefabIndex = dummyIndex % playerCharacterPrefabs.Length;

            int spawnPointArrayIndex = dummyIndex;
            Transform spawnPoint = (spawnPoints != null && spawnPoints.Length > spawnPointArrayIndex) ? spawnPoints[spawnPointArrayIndex] : null;
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRot = (dummyTeam == Youstianus.ETeam.Blue) ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);

            runner.Spawn(playerCharacterPrefabs[prefabIndex], spawnPos, spawnRot, null, (runner, obj) => {
                var pc = obj.GetComponent<Youstianus.PlayerController>();
                if (pc != null)
                {
                    pc.Team = dummyTeam;
                    pc.SpawnPointIndex = spawnPointArrayIndex + 1;
                }
            });
        }
    }

    #region INetworkRunnerCallbacks Implementation

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerCountChanged?.Invoke(runner.SessionInfo.PlayerCount);
        if (spawnedCharacters.TryGetValue(player, out NetworkObject obj))
        {
            if (obj != null) runner.Despawn(obj);
            spawnedCharacters.Remove(player);
        }
        if (playerTeamMap.ContainsKey(player)) playerTeamMap.Remove(player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (Youstianus.PlayerController.Local != null)
        {
            var playerInput = Youstianus.PlayerController.Local.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                input.Set(playerInput.GetNetworkInput());
                playerInput.ResetInputFlags();
            }
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Network] Shutdown Reason: {shutdownReason}");
        spawnedCharacters.Clear();
        playerTeamMap.Clear();

        if (networkRunner != null && networkRunner == runner)
        {
            Destroy(networkRunner.gameObject);
            networkRunner = null;
        }

        SceneManager.LoadScene("MainLoby");
        OnShutdownEvent?.Invoke();
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    #endregion
}
