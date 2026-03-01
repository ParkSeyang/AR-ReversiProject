using UnityEngine;
using Fusion;

/// <summary>
/// 로컬 플레이어의 입력을 감지하고 네트워크 동기화를 위한 데이터를 준비합니다.
/// </summary>
public class PlayerInput : NetworkBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject clickCursorPrefab;

    private Camera mainCamera;
    private GameObject currentCursorInstance;

    // 네트워크 전송을 위한 임시 캐시 변수
    private bool moveRequested;
    private bool attackRequested;
    private Vector3 lastClickPosition;

    public override void Spawned()
    {
        // 본인 캐릭터인 경우에만 카메라 할당
        if (Object.HasInputAuthority == true)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        // 1. 네트워크 객체가 유효하지 않으면 실행 중지
        if (Object == null || Object.IsValid == false) return;

        // 2. 로컬 입력 감지 (Input Authority가 있는 경우에만 실행)
        if (Object.HasInputAuthority == false) return;

        // 이동 요청: 마우스 우클릭
        if (Input.GetMouseButtonDown(1))
        {
            ProcessClickInput(NetworkInputData.BUTTON_MOVE);
        }

        // 공격 요청: Q 키
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ProcessClickInput(NetworkInputData.BUTTON_ATTACK);
        }
    }

    /// <summary>
    /// 마우스 클릭 지점의 월드 좌표를 계산하고 요청 플래그를 설정합니다.
    /// </summary>
    private void ProcessClickInput(int buttonType)
    {
        // 카메라가 없는 경우를 대비한 안전장치
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            lastClickPosition = hit.point;

            if (buttonType == NetworkInputData.BUTTON_MOVE)
            {
                moveRequested = true;
                ShowVisualFeedback(hit.point);
            }
            else if (buttonType == NetworkInputData.BUTTON_ATTACK)
            {
                attackRequested = true;
            }
        }
    }

    /// <summary>
    /// 로컬 플레이어에게만 이동 지점을 시각적으로 표시합니다.
    /// </summary>
    private void ShowVisualFeedback(Vector3 position)
    {
        if (clickCursorPrefab == null) return;

        if (currentCursorInstance != null)
        {
            Destroy(currentCursorInstance);
        }

        currentCursorInstance = Instantiate(clickCursorPrefab, position + Vector3.up * 0.05f, Quaternion.identity);
        Destroy(currentCursorInstance, 0.2f);
    }

    /// <summary>
    /// NetworkRunner가 입력을 수집할 때 캐싱된 데이터를 넘겨줍니다.
    /// </summary>
    public NetworkInputData GetNetworkInput()
    {
        NetworkInputData inputData = new NetworkInputData();

        inputData.Buttons.Set(NetworkInputData.BUTTON_MOVE, moveRequested);
        inputData.Buttons.Set(NetworkInputData.BUTTON_ATTACK, attackRequested);
        inputData.ClickPosition = lastClickPosition;

        return inputData;
    }

    /// <summary>
    /// 입력을 전송한 후 플래그를 초기화합니다. (OnInput에서 호출됨)
    /// </summary>
    public void ResetInputFlags()
    {
        moveRequested = false;
        attackRequested = false;
    }
}
