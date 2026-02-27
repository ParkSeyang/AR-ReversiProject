using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Study.Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    #region NetworkRunnerCallbacks Methods

     public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
       
    }
    
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
  
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
       
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
     
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
       
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
       
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
       
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
      
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
   
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
      
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
       
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }

    #endregion


    #region Network Methods

    private NetworkRunner networkRunner;

    private async void StartGame(GameMode mode)
    {
        // GameMode에 따른 처리가 들어가 줘야 합니다.
        // 제일 먼저 만들어야 하는것은. NetworkRunner에 관한 설정 입니다.
        // NetworkRunner를 AddComponent 하는것도 됩니다만, 동적으로 붙히는 방식으로 구현하겠습니다.
        
        networkRunner = gameObject.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true; // input을 쓸꺼냐? 
        
        // 네트워크 Scene 설정을 해줍니다.
        // Scene은 무대입니다. 이 무대를 다른 지역의 극장의 무대와 동기화 시켜준다고 생각하면 됩니다.
        // Fusion에서 관리하는 NetworkObject뿐만 아니라 Scene자체도 Network적으로 동기화 시켜서
        // 각 참여자들이 Scene안에서 동일한 Network 식별자를 가지고 서로 통신할 수 있도록 해줍니다.

        // 지금 활성화된 Scene의 buildIndex를 가지고 옵니다.
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        
        // runner를 통해서 게임을 시작해 줍니다.
        // - Args : Args라는 키워드는 Arguments(인수 / 매개변수)의 복수적 표현입니다.

        await networkRunner.StartGame(new StartGameArgs()  // 아래의 코드블록에서 바로 필드를 설정해줍니다.
        {
            GameMode = mode,  // Host와 Client간의 시작 타입이 다르다. 호스트나 클라 둘중 하나로 시작하도록 설정
            SessionName = "VR8",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    // NetworkPrefabRef : 네트워크 객체의 프리팹입니다.
    [SerializeField] private NetworkPrefabRef playerPrefab;
    
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters 
        = new Dictionary<PlayerRef, NetworkObject>();
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
       // PlayerRef : 해당 Network Player의 고유 정보라고 생각하셔도 됩니다.
       Debug.Log($"OnPlayerJoined : AsIndex : {player.AsIndex}, {player.PlayerId}");

       // 서버인 runner만 Spawn을 합니다.
       // 서버측 runner의 정보를 가지고 Client측이 동기화를 합니다.
       if (runner.IsServer)
       {
           Vector3 spawnPosition = new Vector3(Random.Range(0f, 5f), 1, 0);
           
           // runner에게 spawn명령을 내립니다.
           // NetworkObject가 존재하는 playerPrefab을
           // spawnPosition 위치와 Quaternion.identity 회전값을 적용시키고
           // PlayerRef(플레이어 고유정보)인 player를 주입 시켜 줍니다.
           
           NetworkObject networkPlayerObject = 
               runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
           
            spawnedCharacters.Add(player, networkPlayerObject);
           
           
       }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerLeft : AsIndex : {player.AsIndex}, {player.PlayerId}");

        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkPlayerObject))
        {
            runner.Despawn(networkPlayerObject);
            spawnedCharacters.Remove(player);
        }
    }

    private bool mouseButton0;
    private bool mouseButton1;
    
    
    private void Update()
    {
        mouseButton0 = mouseButton0 | Input.GetMouseButton(0);
        mouseButton1 = mouseButton1 | Input.GetMouseButton(1);
    }
    
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // 입력을 감지해서 새로운 data를 생성하고 input에 담아서 동기화 합니다.
        NetworkInputData data = new NetworkInputData();

        // data에 방향값을 넣어 줍니다.
        if (Input.GetKey(KeyCode.W))
        {
            data.Direction += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            data.Direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            data.Direction += Vector3.back;
        }
        if (Input.GetKey(KeyCode.D))
        {
            data.Direction += Vector3.right;
        }
        
        // 입력을 보내고 
        data.Buttons.Set(NetworkInputData.MOUSE_BUTTON_0, mouseButton0);
        // mouseButton1을 false 처리 해줍니다.
        mouseButton0 = false;
        
        data.Buttons.Set(NetworkInputData.MOUSE_BUTTON_1, mouseButton1);
        mouseButton1 = false;
        
        // input에 해당 data를 설정해줍니다.
        input.Set(data);

    }
    
    
    #endregion
    
    
    #region OnGuI Methods
    
    private void OnGUI()
    {
        if (networkRunner == null)
        {
            if (GUI.Button(new Rect(0,0, 300, 50), "Host"))
            {
                //P2P의 Host 역할을 하는 모드로 게임을 시작한다.
                StartGame(GameMode.Host);
            }
            
            if (GUI.Button(new Rect(0,50, 300, 50), "Join"))
            {
                // P2P의 Guest(Client) 역할을 하는 모드로 게임을 시작한다.
                StartGame(GameMode.Client);
            }
            
        }
    }
    
    #endregion
    
}
