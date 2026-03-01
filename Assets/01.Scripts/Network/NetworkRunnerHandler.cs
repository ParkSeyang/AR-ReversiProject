using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 네트워크 세션 관리(방 생성/입장) 및 플레이어 스폰을 담당하는 핵심 핸들러입니다.
/// </summary>
public class NetworkRunnerHandler : SingletonBase<NetworkRunnerHandler>, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private string sessionName = "DodgeballRoom";

    private NetworkRunner networkRunner;
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public async void StartGame(GameMode mode, string customSessionName = "")
    {
        if (networkRunner != null) return;

        if (string.IsNullOrEmpty(customSessionName) == false)
        {
            sessionName = customSessionName;
        }

        networkRunner = gameObject.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        await networkRunner.StartGame(startGameArgs);
        
        Debug.Log($"[Network] Started as {mode} in session: {sessionName}");
    }

    public void Shutdown()
    {
        if (networkRunner != null)
        {
            networkRunner.Shutdown();
            networkRunner = null;
        }
    }

    #region INetworkRunnerCallbacks Implementation

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer == true)
        {
            Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 1, UnityEngine.Random.Range(-5f, 5f));
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
            spawnedCharacters.Add(player, networkPlayerObject);

            Debug.Log($"[Network] Player {player.PlayerId} Joined.");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkPlayerObject))
        {
            if (networkPlayerObject != null)
            {
                runner.Despawn(networkPlayerObject);
            }
            spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // 씬에 존재하는 로컬 플레이어(본인) 인스턴스를 통해 입력을 가져옴
        if (Player.Instance != null)
        {
            var playerInput = Player.Instance.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                // 데이터를 가져오고
                input.Set(playerInput.GetNetworkInput());
                
                // 가져온 직후 플래그를 초기화하여 다음 입력 대기 상태로 만듦
                playerInput.ResetInputFlags();
            }
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Network] Shutdown Reason: {shutdownReason}");
        spawnedCharacters.Clear();
        networkRunner = null;
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    #endregion

    private void OnGUI()
    {
        if (networkRunner == null)
        {
            // GUI 박스 및 버튼 크기 대폭 확대
            float boxWidth = 500f;
            float boxHeight = 350f;

            // 화면 중앙 좌표 계산
            float posX = (Screen.width - boxWidth) / 2f;
            float posY = (Screen.height - boxHeight) / 2f;

            GUI.Box(new Rect(posX, posY, boxWidth, boxHeight), "DODGEBALL MULTIPLAYER");

            float contentWidth = boxWidth - 60f; 
            float currentY = posY + 50f;

            GUI.Label(new Rect(posX + 30f, currentY, contentWidth, 30f), "ROOM NAME:");
            currentY += 35f;
            sessionName = GUI.TextField(new Rect(posX + 30f, currentY, contentWidth, 40f), sessionName);

            currentY += 70f;
            if (GUI.Button(new Rect(posX + 30f, currentY, contentWidth, 70f), "CREATE ROOM (HOST)")) StartGame(GameMode.Host);

            currentY += 85f;
            if (GUI.Button(new Rect(posX + 30f, currentY, contentWidth, 70f), "ENTER ROOM (JOIN)")) StartGame(GameMode.Client);
        }
        else
        {
            if (GUI.Button(new Rect(20, 20, 160, 60), "DISCONNECT")) Shutdown();
        }
    }
}
