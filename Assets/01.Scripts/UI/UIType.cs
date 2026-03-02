/// <summary>
/// 피구 게임(Dodgeball Multi-Game)에서 사용되는 UI 타입을 정의합니다.
/// </summary>
public enum UIType
{
    None,
    Title,        // 타이틀(게임 시작) 화면
    Lobby,        // 메인 로비 화면
    Matchmaking,  // [추가] PlayGame 씬의 대기방(세션/룸) UI
    Loading,      // 씬 로딩 화면
    HUD,          // 인게임 체력, 팀 점수, 남은 시간
    QuickSlot,    // 아이템 슬롯 (3칸)
    Menu,         // ESC 메뉴
    WarningPopup, // 경고/알림 팝업
    GameOver,     // 경기 결과 화면
}
