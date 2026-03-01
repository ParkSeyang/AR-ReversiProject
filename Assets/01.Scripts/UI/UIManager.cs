using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 프로젝트의 전체 UI 생명주기와 상태를 관리하는 싱글톤 매니저입니다.
/// </summary>
public class UIManager : SingletonBase<UIManager>
{
    private Dictionary<UIType, BaseUI> uiDictionary = new Dictionary<UIType, BaseUI>();

    public bool IsPopupOpen { get; private set; }
    private bool isUIManagementEnabled = true;

    /// <summary>
    /// UI 요소를 매니저의 관리 장부에 등록합니다.
    /// </summary>
    public void RegisterUI(BaseUI baseUI)
    {
        if (baseUI == null) return;

        // 이미 DontDestroyOnLoad에서 관리 중인 UI가 있다면 중복 등록을 방지함
        if (uiDictionary.TryGetValue(baseUI.UIType, out var existingUI))
        {
            if (existingUI != null && existingUI.gameObject.scene.name == "DontDestroyOnLoad")
            {
                return;
            }
        }

        uiDictionary[baseUI.UIType] = baseUI;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로드 시마다 UI 장부를 갱신하고 상태를 초기화함
        OnInitialize();
    }

    protected override void OnInitialize()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isTitleScene = sceneName.Contains("Start") || sceneName.Contains("Title");
        
        // 1. EventSystem 중복 방지 및 단일 활성화 유지
        ManageEventSystem();

        // 2. UI 캔버스 중복 제거 및 DontDestroyOnLoad 관리
        CleanupDuplicateUIs();

        // 3. 초기 UI 상태 설정
        InitializeUIStates(isTitleScene);
    }

    private void ManageEventSystem()
    {
        var allEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        EventSystem primaryES = null;

        foreach (var es in allEventSystems)
        {
            if (es.gameObject.scene.name == "DontDestroyOnLoad")
            {
                if (primaryES == null) primaryES = es;
                else DestroyImmediate(es.gameObject);
            }
        }

        foreach (var es in allEventSystems)
        {
            if (es == null || es.gameObject.scene.name == "DontDestroyOnLoad") continue;

            if (primaryES == null)
            {
                primaryES = es;
                DontDestroyOnLoad(es.gameObject);
            }
            else DestroyImmediate(es.gameObject);
        }

        if (primaryES != null)
        {
            primaryES.gameObject.SetActive(true);
            primaryES.enabled = true;
        }
    }

    private void CleanupDuplicateUIs()
    {
        BaseUI[] allUIs = Object.FindObjectsByType<BaseUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        uiDictionary.Clear();

        // 단계 1: 이미 DDOL에 있는 UI들을 장부에 먼저 등록
        foreach (var uiBase in allUIs)
        {
            if (uiBase == null) continue;
            Transform rootTransform = uiBase.transform.root;

            if (rootTransform.gameObject.scene.name == "DontDestroyOnLoad")
            {
                uiDictionary[uiBase.UIType] = uiBase;
            }
        }

        // 단계 2: 새로 로드된 루트(Canvas)들을 검사하여 파괴하거나 DDOL로 이동
        var newRoots = allUIs
            .Where(ui => ui != null && ui.gameObject.scene.name != "DontDestroyOnLoad")
            .Select(ui => ui.transform.root.gameObject)
            .Distinct()
            .ToList();

        foreach (var root in newRoots)
        {
            var components = root.GetComponentsInChildren<BaseUI>(true);
            bool isDuplicate = components.Any(ui => uiDictionary.ContainsKey(ui.UIType));

            if (isDuplicate == true)
            {
                DestroyImmediate(root);
            }
            else
            {
                DontDestroyOnLoad(root);
                foreach (var ui in components)
                {
                    uiDictionary[ui.UIType] = ui;
                }
            }
        }
    }

    private void InitializeUIStates(bool isTitleScene)
    {
        foreach (var ui in uiDictionary.Values)
        {
            if (ui == null) continue;

            if (ui.UIType == UIType.Title)
            {
                if (isTitleScene == true) ui.Open(); else ui.Close();
            }
            else if (ui.IsPopup == false)
            {
                // HUD나 퀵슬롯 같은 비팝업 UI는 인게임에서만 오픈
                if (isTitleScene == false) ui.Open(); else ui.Close();
            }
            else
            {
                ui.Close(); // 팝업은 기본적으로 닫힘
            }
        }
        
        RefreshUIState();
    }

    private void Update()
    {
        if (isUIManagementEnabled == false) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPopupOpen == true) CloseAllPopup();
            else ToggleUI(UIType.Menu);
        }
    }

    public void ToggleUI(UIType uiType)
    {
        if (uiDictionary.TryGetValue(uiType, out BaseUI targetUI) == false) return;

        if (targetUI.gameObject.activeSelf == true) SetUIActive(uiType, false);
        else SetUIActive(uiType, true);
    }

    public void SetUIActive(UIType type, bool isActive)
    {
        if (uiDictionary.TryGetValue(type, out var targetUI))
        {
            if (isActive == true) targetUI.Open();
            else targetUI.Close();
            RefreshUIState();
        }
    }

    public void SetAllInGameUIActive(bool isActive)
    {
        isUIManagementEnabled = isActive;

        foreach (var ui in uiDictionary.Values)
        {
            if (ui == null) continue;

            if (isActive == true) 
            { 
                // 게임 시작 시: 타이틀 UI는 닫고, HUD/퀵슬롯 등은 활성화
                if (ui.UIType == UIType.Title) ui.Close();
                else if (ui.IsPopup == false) ui.Open(); 
            }
            else
            {
                // 로비 상태 시: 타이틀 UI만 노출
                if (ui.UIType == UIType.Title) ui.Open();
                else ui.Close();
            }
        }

        RefreshUIState();
    }

    public void CloseAllPopup()
    {
        var activePopups = uiDictionary.Values.Where(ui => ui != null && ui.IsPopup && ui.gameObject.activeSelf).ToList();
        foreach (var popup in activePopups) popup.Close();
        RefreshUIState();
    }

    public void RefreshUIState()
    {
        IsPopupOpen = uiDictionary.Values.Any(ui => ui != null && ui.IsPopup && ui.gameObject.activeSelf);
        
        // 멀티플레이어 환경에서는 Time.timeScale 조절 대신 커서 제어만 수행함
        SetControlState(IsPopupOpen == false);
    }

    private void SetControlState(bool canControl)
    {
        // 피구 게임(탑다운) 특성상 커서가 항상 보여야 조준과 이동이 가능함
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowWarning(string message)
    {
        // 경고 팝업 로직 (구현된 UI가 있을 경우 활성화)
        Debug.Log($"[UI Warning] {message}");
    }

    public void ForceRefreshAll()
    {
        foreach (var ui in uiDictionary.Values)
        {
            if (ui != null) ui.Refresh();
        }
    }
}
