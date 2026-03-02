using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

namespace Study.Examples.Fusion
{
    public class TestLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkObject playerPrefab;
        private NetworkRunner _runner;

        private void OnGUI()
        {
            if (_runner == null)
            {
                if (GUI.Button(new Rect(10, 10, 200, 50), "Start Test Mode"))
                {
                    StartTest();
                }
            }
        }

        async void StartTest()
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Single,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (player == runner.LocalPlayer)
            {
                Vector3 spawnPos = Vector3.up;
                Quaternion spawnRot = Quaternion.identity; // 기본 회전값
                
                GameObject spawnPoint = GameObject.Find("SpawnPoint1");
                if (spawnPoint != null)
                {
                    spawnPos = spawnPoint.transform.position;
                    // SpawnPoint의 회전값을 가져오고, 블루팀이면 추가로 180도 회전 시킵니다.
                    spawnRot = spawnPoint.transform.rotation * Quaternion.Euler(0, 180, 0);
                }
                else
                {
                    Debug.LogWarning("SpawnPoint1을 찾을 수 없습니다! 기본 위치에 생성합니다.");
                }

                // 보정된 위치와 회전값으로 캐릭터 스폰
                var playerObj = runner.Spawn(playerPrefab, spawnPos, spawnRot, player);
                var controller = playerObj.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Team = ETeam.Blue;
                }
            }
        }

        // --- 나머지 인터페이스 구현 ---
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
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
    }
}
