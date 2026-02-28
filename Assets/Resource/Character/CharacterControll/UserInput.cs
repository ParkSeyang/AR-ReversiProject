using UnityEngine;
using Fusion;

namespace Youstianus
{
    public class UserInput : NetworkBehaviour
    {
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private GameObject clickCursorPrefab; // 클릭 시 생성할 프리팹
        
        private PlayerController playerController;
        private Camera mainCamera;
        private GameObject currentCursor; // 현재 화면에 표시 중인 커서

        private float lastMoveRequestTime = -100f;
        private const float moveInputInterval = 0.05f; // 0.1s -> 0.05s 단축
        private const float cursorLifeTime = 0.1f;

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                playerController = GetComponent<PlayerController>();
                mainCamera = Camera.main;
            }
        }

        public override void Render()
        {
            if (Object == null || !Object.IsValid) return;
            if (!Object.HasInputAuthority) return;

            // Move: Right Click (Hold)
            if (Input.GetMouseButton(1))
            {
                bool isInitialClick = Input.GetMouseButtonDown(1);
                
                if (isInitialClick || Time.time - lastMoveRequestTime >= moveInputInterval)
                {
                    HandleMovementInput(isInitialClick);
                    lastMoveRequestTime = Time.time;
                }
            }

            // Attack: Q
            if (Input.GetKeyDown(KeyCode.Q))
            {
                playerController.Attack();
            }
        }

        private void HandleMovementInput(bool showCursor)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f)) 
            {
                // 보정된 위치를 받아서 캐릭터를 이동시키고, 필요 시 그 위치에 커서를 표시합니다.
                Vector3 validDest = playerController.MoveTo(hit.point);
                
                if (showCursor)
                {
                    ShowClickCursor(validDest);
                }
            }
        }

        private void ShowClickCursor(Vector3 position)
        {
            if (clickCursorPrefab == null) return;

            if (currentCursor != null)
            {
                Destroy(currentCursor);
            }

            // 새로운 위치(보정된 NavMesh 위치)에 커서 생성
            currentCursor = Instantiate(clickCursorPrefab, position + Vector3.up * 0.05f, Quaternion.identity);
            
            // 0.1초 라이프타임 적용
            Destroy(currentCursor, 0.1f);
        }
    }
}
