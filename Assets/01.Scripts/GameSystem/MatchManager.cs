using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EMatchState { Waiting, Playing, RoundOver, MatchOver }

/// <summary>
/// 경기 규칙을 관리하며, 이미 소환된 플레이어들의 위치 리셋 및 승패 판정을 담당합니다.
/// </summary>
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Match Rules")]
    [SerializeField] private float roundDuration = 120f;
    [SerializeField] private int targetWins = 2;

    [Networked] public EMatchState CurrentState { get; set; }
    [Networked] public int BlueScore { get; set; }
    [Networked] public int RedScore { get; set; }
    [Networked] private TickTimer roundTimer { get; set; }

    public override void Spawned()
    {
        Instance = this;
        if (HasStateAuthority == true)
        {
            CurrentState = EMatchState.Waiting;
            StartCoroutine(CoMatchFlow());
        }
    }

    private IEnumerator CoMatchFlow()
    {
        while (LobbyManager.Instance == null || LobbyManager.Instance.IsMatchStarting == false)
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }

        yield return new WaitForSecondsRealtime(3.5f); 
        
        Debug.Log("[Match] 경기 시작! 위치 리셋 중...");
        StartNewRound();
    }

    private void StartNewRound()
    {
        if (HasStateAuthority == false) return;

        // [중요] 이미 캐릭터들이 접속 시 소환되어 있으므로 위치만 포인트로 재배치합니다.
        ResetPlayers();

        roundTimer = TickTimer.CreateFromSeconds(Runner, roundDuration);
        CurrentState = EMatchState.Playing;
    }

    private void ResetPlayers()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        int blueIdx = 0;
        int redIdx = 0;

        foreach (var p in players)
        {
            p.SetHP(p.MaxHP);
            
            // 팀별 스폰 포인트 인덱스 계산 (Blue: 1,2 / Red: 3,4)
            int spawnPointNum = (p.TeamID == 0) ? (++blueIdx) : (++redIdx + 2);
            
            GameObject point = GameObject.Find("SpawnPoint " + spawnPointNum);
            if (point != null)
            {
                p.transform.position = point.transform.position;
                p.transform.rotation = point.transform.rotation;
                if (p.TeamID == 0) p.transform.rotation *= Quaternion.Euler(0, 180, 0);
            }
            
            p.GetComponent<Animator>()?.Rebind();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false || CurrentState != EMatchState.Playing) return;

        if (roundTimer.Expired(Runner) == true)
        {
            DetermineWinnerByRules();
            return;
        }

        CheckAnnihilation();
    }

    private void CheckAnnihilation()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        int blueAlive = players.Count(p => p.TeamID == 0 && p.HP > 0);
        int redAlive = players.Count(p => p.TeamID == 1 && p.HP > 0);

        if (blueAlive == 0) EndRound(1);
        else if (redAlive == 0) EndRound(0);
    }

    private void DetermineWinnerByRules()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        int blueAlive = players.Count(p => p.TeamID == 0 && p.HP > 0);
        int redAlive = players.Count(p => p.TeamID == 1 && p.HP > 0);

        if (blueAlive > redAlive) EndRound(0);
        else if (redAlive > blueAlive) EndRound(1);
        else EndRound(0); 
    }

    private void EndRound(int winnerTeam)
    {
        if (winnerTeam == 0) BlueScore++; else RedScore++;

        if (BlueScore >= targetWins || RedScore >= targetWins)
        {
            CurrentState = EMatchState.MatchOver;
            RPC_OnMatchEnd(winnerTeam);
        }
        else
        {
            CurrentState = EMatchState.RoundOver;
            StartCoroutine(CoWaitAndNextRound());
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnMatchEnd(int finalWinnerTeam)
    {
        if (UIManager.IsInitialized == true)
        {
            UIManager.Instance.SetUIActive(UIType.GameOver, true);
            var resultUI = UnityEngine.Object.FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
            if (resultUI != null) resultUI.SetResult(finalWinnerTeam);
        }
    }

    private IEnumerator CoWaitAndNextRound()
    {
        yield return new WaitForSeconds(3.0f);
        StartNewRound();
    }

    public float GetRemainingTime()
    {
        if (roundTimer.IsRunning == false) return roundDuration;
        return roundTimer.RemainingTime(Runner) ?? 0f;
    }
}
