using UnityEngine;

namespace Youstianus
{
    /// <summary>
    /// 인게임 씬의 스폰 포인트들을 관리하고 싱글톤 핸들러에 등록합니다.
    /// </summary>
    public class SpawnPointContainer : MonoBehaviour
    {
        [Header("Spawn Points")]
        [Tooltip("SpawnPoint 1, 2, 3, 4를 순서대로 넣어주세요.")]
        [SerializeField] private Transform[] sceneSpawnPoints;

        private void Awake()
        {
            // 싱글톤 인스턴스에 현재 씬의 스폰 포인트들 전달
            if (NetworkRunnerHandler.Instance != null)
            {
                NetworkRunnerHandler.Instance.SetSpawnPoints(sceneSpawnPoints);
                Debug.Log("[SpawnPointContainer] 스폰 포인트 등록 완료!");
            }
            else
            {
                Debug.LogError("[SpawnPointContainer] NetworkRunnerHandler 인스턴스를 찾을 수 없습니다!");
            }
        }
    }
}
