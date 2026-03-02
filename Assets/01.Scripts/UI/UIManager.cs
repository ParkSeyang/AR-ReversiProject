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

    public void RegisterUI(BaseUI baseUI)
    {
        if (baseUI == null) return;

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
        OnInitialize();
    }

    protected override void OnInitialize()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isTitleScene = sceneName.Contains("Start") || sceneName.Contains("Title");
        bool isLobbyScene = sceneName.Contains("Loby") || sceneName.Contains("Lobby");
        
        ManageEventSystem();
        CleanupDuplicateUIs();
        InitializeUIStates(isTitleScene, isLobbyScene);
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

        foreach (var uiBase in allUIs)
        {
            if (uiBase == null) continue;
            Transform rootTransform = uiBase.transform.root;

            if (rootTransform.gameObject.scene.name == "DontDestroyOnLoad")
            {
                uiDictionary[uiBase.UIType] = uiBase;
            }
        }

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

    private void InitializeUIStates(bool isTitleScene, bool isLobbyScene)
    {
        foreach (var ui in uiDictionary.Values)
        {
            if (ui == null) continue;

            if (ui.UIType == UIType.Title)
            {
                if (isTitleScene == true) ui.Open(); else ui.Close();
            }
            else if (ui.UIType == UIType.Lobby)
            {
                if (isLobbyScene == true) ui.Open(); else ui.Close();
            }
            else if (ui.IsPopup == false)
            {
                // 인게임 전용 UI (HUD 등)는 타이틀도 로비도 아닐 때만 켬
                bool isGameScene = isTitleScene == false && isLobbyScene == false;
                if (isGameScene == true) ui.Open(); else ui.Close();
            }
            else
            {
                ui.Close();
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
        string sceneName = SceneManager.GetActiveScene().name;
        bool isLobbyScene = sceneName.Contains("Loby") || sceneName.Contains("Lobby");

        foreach (var ui in uiDictionary.Values)
        {
            if (ui == null) continue;

            if (isActive == true) 
            { 
                // 게임 시작 시: 타이틀과 로비는 닫고, HUD/퀵슬롯 등 활성화
                if (ui.UIType == UIType.Title || ui.UIType == UIType.Lobby) ui.Close();
                else if (ui.IsPopup == false) ui.Open(); 
            }
            else
            {
                // 게임 중이 아닐 때: 현재가 로비면 로비만 켬, 그 외(타이틀)면 타이틀만 켬
                if (ui.UIType == UIType.Lobby)
                {
                    if (isLobbyScene == true) ui.Open(); else ui.Close();
                }
                else if (ui.UIType == UIType.Title)
                {
                    if (isLobbyScene == false) ui.Open(); else ui.Close();
                }
                else
                {
                    ui.Close();
                }
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
        SetControlState(IsPopupOpen == false);
    }

    private void SetControlState(bool canControl)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowWarning(string message)
    {
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
