using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Youstianus
{
    public enum EGameState { Waiting, Ready, Playing, RoundEnd, GameOver }

    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Rules")]
        [Networked] public int BlueScore { get; set; }
        [Networked] public int RedScore { get; set; }
        [Networked] public int CurrentRound { get; set; } = 1;
        [Networked] public float RemainingTime { get; set; } = 60f;
        [Networked] public EGameState State { get; set; } = EGameState.Waiting;

        private const float ROUND_TIME = 60f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                StartCoroutine(GameLoopRoutine());
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && State == EGameState.Playing)
            {
                RemainingTime -= Runner.DeltaTime;
                if (RemainingTime <= 0)
                {
                    RemainingTime = 0;
                    DetermineRoundWinner(true);
                }
                else
                {
                    CheckTeamElimination();
                }
            }
        }

        private IEnumerator GameLoopRoutine()
        {
            // 모든 클라이언트가 씬 로드를 마칠 때까지 대기
            yield return new WaitForSeconds(2.0f); 

            while (BlueScore < 3 && RedScore < 3)
            {
                // 매 라운드 시작 전 팀 랜덤 배정 및 소환 수행 (첫 라운드 포함)
                yield return StartCoroutine(RoundFlowRoutine());
                CurrentRound++;
            }

            State = EGameState.GameOver;
            Youstianus.ETeam winner = (BlueScore >= 3) ? Youstianus.ETeam.Blue : Youstianus.ETeam.Red;
            string finalWinner = (winner == Youstianus.ETeam.Blue) ? "BlueTeam" : "RedTeam";
            RPC_ShowMessage($"GAME OVER! {finalWinner} VICTORY!");

            if (HasStateAuthority)
            {
                var result = new NetworkRunnerHandler.GameResult { WinnerTeam = winner };
                var winnerPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                                    .Where(p => p.Team == winner);
                foreach (var p in winnerPlayers) result.WinnerNames.Add(p.NickName.ToString());
                NetworkRunnerHandler.LastResult = result;
            }

            yield return new WaitForSeconds(5.0f);
            
            if (IngameUI.Instance != null)
            {
                yield return IngameUI.Instance.FadeToBlack(1.5f);
            }

            if (HasStateAuthority)
            {
                NetworkRunnerHandler.Instance.Shutdown();
            }
        }

        private IEnumerator RoundFlowRoutine()
        {
            State = EGameState.Ready;
            RemainingTime = ROUND_TIME;

            if (HasStateAuthority)
            {
                RPC_ToggleGrounds(false); 
                NetworkRunnerHandler.Instance.DespawnAllPlayers();
                yield return new WaitForSeconds(0.5f); 
                NetworkRunnerHandler.Instance.RespawnAll();
            }

            yield return new WaitForSeconds(0.5f);
            if (IngameUI.Instance != null) IngameUI.Instance.SetLoadingScreen(false);

            yield return new WaitForSeconds(2.0f);

            RPC_StartRoundUI();
            yield return new WaitForSeconds(3.0f);

            State = EGameState.Playing;
            RPC_ToggleGrounds(true); 

            while (State == EGameState.Playing)
            {
                yield return null;
            }

            yield return new WaitForSeconds(3.0f);
        }

        private void CheckTeamElimination()
        {
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            int blueAlive = players.Count(p => p.Team == ETeam.Blue && !p.GetComponent<PlayerData>().IsDead);
            int redAlive = players.Count(p => p.Team == ETeam.Red && !p.GetComponent<PlayerData>().IsDead);

            if (blueAlive == 0 && redAlive == 0) DetermineRoundWinner(false, null);
            else if (blueAlive == 0) DetermineRoundWinner(false, ETeam.Red);
            else if (redAlive == 0) DetermineRoundWinner(false, ETeam.Blue);
        }

        private void DetermineRoundWinner(bool isTimeOut, ETeam? winner = null)
        {
            if (State != EGameState.Playing) return;
            State = EGameState.RoundEnd;

            if (isTimeOut)
            {
                var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                int blueHP = players.Where(p => p.Team == ETeam.Blue).Sum(p => p.GetComponent<PlayerData>().CurrentHP);
                int redHP = players.Where(p => p.Team == ETeam.Red).Sum(p => p.GetComponent<PlayerData>().CurrentHP);

                if (blueHP > redHP) winner = ETeam.Blue;
                else if (redHP > blueHP) winner = ETeam.Red;
                else winner = null; 
            }

            if (winner == ETeam.Blue)
            {
                BlueScore++;
                RPC_ShowMessage("BlueTeam! Win!");
            }
            else if (winner == ETeam.Red)
            {
                RedScore++;
                RPC_ShowMessage("RedTeam! Win!");
            }
            else
            {
                RPC_ShowMessage("Keep Fight!");
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ToggleGrounds(bool active)
        {
            GameObject blue = GameObject.Find("BlueTeamGround");
            GameObject red = GameObject.Find("RedTeamGround");
            if (blue) blue.SetActive(active);
            if (red) red.SetActive(active);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_StartRoundUI() => IngameUI.Instance.PlayStartSequence();

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ShowMessage(string msg) => IngameUI.Instance.ShowInformation(msg);
    }
}
