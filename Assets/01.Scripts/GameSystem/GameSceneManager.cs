using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;

/// <summary>
/// 게임 전체의 씬 흐름을 관리하며, 씬 로드 완료 시 UIManager를 통해 적절한 UI를 활성화합니다.
/// </summary>
public class GameSceneManager : SingletonBase<GameSceneManager>
{
    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "MainLoby";
    [SerializeField] private string gameSceneName = "PlayGame";
    [SerializeField] private string startSceneName = "GameStartScene";

    public bool IsLoading { get; private set; }

    public void MoveToLobbyFromStart()
    {
        if (IsLoading == true) return;
        StartCoroutine(LoadSceneRoutine(lobbySceneName));
    }

    public void MoveToPlayGameFromLobby()
    {
        if (IsLoading == true) return;
        StartCoroutine(LoadSceneRoutine(gameSceneName));
    }

    /// <summary>
    /// [추가] 네트워크 셧다운 후 로컬에서 직접 로비 씬으로 돌아갑니다.
    /// </summary>
    public void ReturnToLobbyManual()
    {
        if (IsLoading == true) return;
        StartCoroutine(LoadSceneRoutine(lobbySceneName));
    }

    public void StartMatch(NetworkRunner runner)
    {
        if (runner == null || runner.IsServer == false) return;
        
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(gameSceneName);
        if (sceneIndex != -1)
        {
            runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
        else
        {
            runner.LoadScene(gameSceneName);
        }
    }

    public void ReturnToLobby(NetworkRunner runner)
    {
        if (runner == null || runner.IsServer == false) return;

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(lobbySceneName);
        if (sceneIndex != -1)
        {
            runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
        else
        {
            runner.LoadScene(lobbySceneName);
        }
    }

    /// <summary>
    /// 비동기 씬 로드 및 씬별 필수 UI 자동 활성화 로직입니다.
    /// </summary>
    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoading = true;

        if (UIManager.IsInitialized == true)
        {
            UIManager.Instance.SetUIActive(UIType.Loading, true);
        }

        AsyncOperation asyncLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (asyncLoadOperation != null && asyncLoadOperation.isDone == false)
        {
            yield return null;
        }

        // 안정화를 위한 짧은 대기
        yield return new WaitForSecondsRealtime(0.5f);

        if (UIManager.IsInitialized == true)
        {
            // 1. 로딩 UI 해제
            UIManager.Instance.SetUIActive(UIType.Loading, false);
            
            // 2. 씬에 따른 전용 UI 활성화 제어
            if (sceneName == lobbySceneName)
            {
                UIManager.Instance.SetUIActive(UIType.Lobby, true);
                UIManager.Instance.SetUIActive(UIType.Matchmaking, false); // [추가] 대기방 UI 해제
                UIManager.Instance.SetUIActive(UIType.HUD, false);
            }
            else if (sceneName == gameSceneName)
            {
                // 경기장 진입 시 대기방 UI를 먼저 켬 (실제 경기는 전원 준비 후 시작)
                UIManager.Instance.SetUIActive(UIType.Lobby, false);
                UIManager.Instance.SetUIActive(UIType.Matchmaking, true); // [수정] 대기방 UI 활성화
                UIManager.Instance.SetUIActive(UIType.HUD, false);        // [수정] 시작 시 HUD는 아직 끔
            }
            else if (sceneName == startSceneName)
            {
                UIManager.Instance.SetUIActive(UIType.Title, true);
            }
            
            // 3. 인게임 UI 그룹 전체 상태 갱신
            UIManager.Instance.SetAllInGameUIActive(sceneName == gameSceneName);
        }

        IsLoading = false;
        Debug.Log($"[Scene] Scene Load Completed: {sceneName}");
    }
}
