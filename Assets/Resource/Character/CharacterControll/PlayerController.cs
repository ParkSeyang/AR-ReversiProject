using UnityEngine;
using UnityEngine.AI;
using Fusion;
using System.Linq;

namespace Youstianus
{
    public enum ETeam { Blue, Red }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : NetworkBehaviour
    {
        public static PlayerController Local { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 3000f;
        [SerializeField] private float acceleration = 200f;

        [Header("Attack Settings")]
        [SerializeField] private NetworkPrefabRef[] weaponPrefabs; 
        [SerializeField] private Transform firePoint;             

        [Header("UI Settings")]
        [SerializeField] private GameObject hpBarPrefab; 
        private UI_HpBar hpBarInstance;

        [Networked] public ETeam Team { get; set; }
        [Networked] public bool IsAttacking { get; set; }
        [Networked] public bool IsInHitStun { get; set; } 
        [Networked] public NetworkString<_32> NickName { get; set; } 
        [Networked] public int SpawnPointIndex { get; set; } 
        [Networked] public int CharacterTypeIndex { get; set; } 
        [Networked] private TickTimer attackCooldownTimer { get; set; } 
        [Networked] private TickTimer inputBlockTimer { get; set; } 

        private NavMeshAgent navAgent;
        private Animator animator;
        private Rigidbody rb;
        private PlayerData playerData; 
        private ChangeDetector changeDetector; 
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private bool hasShotInCurrentAttack = false;

        public override void Spawned()
        {
            navAgent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            rb = GetComponent<Rigidbody>();
            playerData = GetComponent<PlayerData>();

            if (Object.HasInputAuthority)
            {
                Local = this;
                string savedNick = PlayerPrefs.GetString("PlayerNickName", "Guest");
                RPC_SetInitialData(savedNick, NetworkRunnerHandler.Instance != null ? NetworkRunnerHandler.Instance.SelectedCharacterIndex : 0);
            }

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (navAgent != null) navAgent.enabled = false;
            changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            ApplyTeamAreaMask();

            if (navAgent != null)
            {
                if (transform.parent != null) transform.localPosition = Vector3.zero;
                navAgent.enabled = true; 
                navAgent.Warp(transform.position);
                navAgent.ResetPath();
                if (navAgent.isOnNavMesh) navAgent.SetDestination(transform.position);
            }

            ConfigureNavMeshAgent();
            UpdateInGameUI();
            InitializeHpBar();
        }

        private void InitializeHpBar()
        {
            if (hpBarPrefab == null) return;
            RectTransform container = (IngameUI.Instance != null) ? IngameUI.Instance.HPBarContainer : null;
            GameObject hpBarObj = Instantiate(hpBarPrefab, container);
            hpBarObj.name = $"HPBar_{NickName}";
            RectTransform rt = hpBarObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.SetParent(container, false);
                rt.localScale = Vector3.one;
                rt.localPosition = Vector3.zero;
            }
            hpBarInstance = hpBarObj.GetComponent<UI_HpBar>();
            UpdateHpBarVisuals();
        }

        private void UpdateHpBarVisuals()
        {
            if (hpBarInstance == null) return;
            bool isEnemy = false;
            if (Local != null && Local != this) isEnemy = (Local.Team != this.Team);
            hpBarInstance.Initialize(this.transform, isEnemy);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RPC_SetInitialData(string nick, int charIndex)
        {
            NickName = nick;
            CharacterTypeIndex = charIndex;
        }

        public override void Render()
        {
            if (Object == null || !Object.IsValid) return;

            if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.W)) PlayHit();

            if (hpBarInstance != null && playerData != null)
            {
                hpBarInstance.UpdateHP(playerData.CurrentHP, playerData.MaxHP, playerData.IsDead);
                if (Local != null && Local != this) hpBarInstance.SetColor(Local.Team != this.Team);
            }

            if (playerData != null && playerData.IsDead)
            {
                if (animator != null)
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (!stateInfo.IsName("Die")) animator.Play("Die");
                    else if (stateInfo.normalizedTime >= 0.95f) animator.speed = 0;
                }
                return;
            }

            foreach (var change in changeDetector.DetectChanges(this))
            {
                if (change == nameof(NickName) || change == nameof(SpawnPointIndex)) UpdateInGameUI();
                if (change == nameof(Team))
                {
                    ApplyTeamAreaMask();
                    UpdateHpBarVisuals();
                }
            }

            if (animator != null && navAgent != null && navAgent.isActiveAndEnabled)
            {
                float currentSpeed = navAgent.velocity.magnitude;
                animator.SetFloat(SpeedHash, currentSpeed > 0.1f ? currentSpeed : 0f);
            }

            CheckAttackProgress();
            CheckHitProgress();

            // [추가] 로컬 플레이어 본인의 쿨타임 UI 갱신
            if (Object.HasInputAuthority && IngameUI.Instance != null && playerData != null)
            {
                float remaining = attackCooldownTimer.RemainingTime(Runner) ?? 0f;
                IngameUI.Instance.UpdateCooltime(remaining, playerData.AttackCooldown);
            }
        }

        private void UpdateInGameUI()
        {
            if (IngameUI.Instance != null && SpawnPointIndex > 0)
            {
                int slotIndex = SpawnPointIndex - 1;
                IngameUI.Instance.UpdatePlayerNameUI(slotIndex, NickName.ToString());
            }
        }

        private void CheckAttackProgress()
        {
            if (!IsAttacking || animator == null) return;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Attack"))
            {
                if (!hasShotInCurrentAttack && stateInfo.normalizedTime >= 0.4f)
                {
                    Shoot();
                    hasShotInCurrentAttack = true;
                }
                if (stateInfo.normalizedTime >= 0.95f) OnAttackEnd();
            }
            else if (!animator.IsInTransition(0)) OnAttackEnd();
        }

        private void CheckHitProgress()
        {
            if (!IsInHitStun || animator == null) return;
            if (!HasStateAuthority) return;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Hit"))
            {
                if (stateInfo.normalizedTime >= 0.9f) OnHitEnd();
            }
            else if (!animator.IsInTransition(0)) OnHitEnd();
        }

        private void ConfigureNavMeshAgent()
        {
            if (navAgent == null) return;
            float speed = (playerData != null) ? playerData.MoveSpeed : moveSpeed;
            navAgent.speed = speed;
            navAgent.angularSpeed = rotationSpeed;
            navAgent.acceleration = acceleration;
            navAgent.stoppingDistance = 0.1f;
            navAgent.updateRotation = true;
            navAgent.updatePosition = true;
        }

        public Vector3 MoveTo(Vector3 targetPosition)
        {
            if (GameManager.Instance != null && GameManager.Instance.State != EGameState.Playing)
            {
                StopNavAgent();
                return transform.position;
            }

            if (playerData != null && playerData.IsDead || IsAttacking || IsInHitStun || !inputBlockTimer.ExpiredOrNotRunning(Runner))
            {
                StopNavAgent();
                return transform.position;
            }
            
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = false; 
                if (Vector3.Distance(transform.position, targetPosition) < 0.1f) return transform.position;
                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 100f, navAgent.areaMask))
                {
                    navAgent.SetDestination(hit.position);
                    return hit.position;
                }
            }
            return targetPosition;
        }

        private void StopNavAgent()
        {
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
                navAgent.ResetPath();
            }
        }

        public void Attack()
        {
            if (Object.HasInputAuthority)
            {
                if (GameManager.Instance != null && GameManager.Instance.State != EGameState.Playing) return;
                if (playerData != null && (playerData.IsDead || IsInHitStun)) return;
                if (IsAttacking || !attackCooldownTimer.ExpiredOrNotRunning(Runner)) return;

                inputBlockTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
                IsAttacking = true;
                hasShotInCurrentAttack = false; 

                StopNavAgent();
                FaceMouseDirection();
                RPC_PlayAttack();

                if (playerData != null)
                    attackCooldownTimer = TickTimer.CreateFromSeconds(Runner, playerData.AttackCooldown);
            }
        }

        private void FaceMouseDirection()
        {
            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 lookDir = (hitPoint - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        public void Shoot()
        {
            if (hasShotInCurrentAttack) return;
            if (!HasStateAuthority) return;
            int weaponIndex = CharacterTypeIndex;
            if (weaponIndex < 0 || weaponIndex >= weaponPrefabs.Length || !weaponPrefabs[weaponIndex].IsValid) return;
            
            hasShotInCurrentAttack = true;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward + Vector3.up;
            Runner.Spawn(weaponPrefabs[weaponIndex], spawnPos, transform.rotation, Object.InputAuthority, (runner, obj) => 
            {
                obj.transform.localScale = Vector3.one;
                if (obj.TryGetComponent<AxeProjectile>(out var axe)) axe.OwnerTeam = this.Team;
                if (obj.TryGetComponent<SwordProjectile>(out var sword)) sword.OwnerTeam = this.Team;
            });
        }

        public void OnAttackEnd()
        {
            if (!HasStateAuthority) return;
            IsAttacking = false;
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = false; 
                navAgent.updateRotation = true;
            }
        }

        public void OnHitEnd()
        {
            if (!HasStateAuthority) return;
            IsInHitStun = false;
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = false;
                navAgent.updateRotation = true;
            }
        }

        public void ResetPlayer()
        {
            if (playerData != null)
            {
                playerData.CurrentHP = playerData.MaxHP;
                playerData.IsDead = false;
            }
            IsAttacking = false;
            IsInHitStun = false;
            
            if (animator != null)
            {
                animator.speed = 1;
                animator.Play("Idle", 0, 0f);
            }

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // [수정] 스폰 포인트로 명시적 순간이동 (Warp)
            if (navAgent != null)
            {
                navAgent.enabled = false; // 좌표 수정을 위해 잠시 끔

                // NetworkRunnerHandler에 등록된 실제 스폰 포인트 좌표 가져오기
                if (NetworkRunnerHandler.Instance != null && SpawnPointIndex > 0)
                {
                    int pointIndex = SpawnPointIndex - 1;
                    if (NetworkRunnerHandler.Instance.spawnPoints != null && 
                        pointIndex < NetworkRunnerHandler.Instance.spawnPoints.Length)
                    {
                        Transform targetPoint = NetworkRunnerHandler.Instance.spawnPoints[pointIndex];
                        if (targetPoint != null)
                        {
                            transform.position = targetPoint.position;
                            transform.rotation = targetPoint.rotation;
                        }
                    }
                }

                navAgent.enabled = true;
                navAgent.Warp(transform.position); // 네브메쉬 위치 강제 동기화
                StopNavAgent();
            }
        }

        public void PlayHit() => RPC_PlayHit();
        public void PlayDie() => RPC_PlayDie();

        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        private void RPC_PlayAttack()
        {
            StopNavAgent();
            if (animator != null) animator.SetTrigger("Attack");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayHit()
        {
            if (playerData != null && playerData.IsDead) return;
            inputBlockTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
            StopNavAgent();
            IsInHitStun = true; 
            if (animator != null) animator.SetTrigger("Hit");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayDie()
        {
            IsAttacking = false;
            IsInHitStun = false;
            if (animator != null)
            {
                animator.speed = 1;
                animator.Play("Die", 0, 0f);
            }
            if (navAgent != null) navAgent.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private void ApplyTeamAreaMask()
        {
            if (navAgent == null) return;
            int walkableMask = 1 << 0;
            int teamMask = (Team == ETeam.Blue) ? (1 << 3) : (1 << 4); 
            navAgent.areaMask = walkableMask | teamMask;
        }

        public void FootL() { }
        public void FootR() { }
    }
}
