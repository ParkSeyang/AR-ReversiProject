using UnityEngine;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;
using System;

public class AuthManager : SingletonBase<AuthManager>
{
    private FirebaseAuth firebaseAuth;
    private FirebaseUser currentUser;

    public string DisplayName => (currentUser != null && string.IsNullOrEmpty(currentUser.DisplayName) == false) 
                                 ? currentUser.DisplayName : "Guest";
    
    // [수정] 인스턴스를 통해 접근 가능하도록 복구
    public bool IsFirebaseInitialized { get; private set; } = false;

    protected override void OnInitialize()
    {
        InitializeFirebase();
    }

    private async void InitializeFirebase()
    {
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        
        if (dependencyStatus == DependencyStatus.Available)
        {
            // [수정] google-services.json 파일의 Database URL 누락 에러를 방지하기 위해 수동 초기화(AppOptions)를 수행합니다.
            // ZeroDarkMos 님, 아래 "" 안의 값들을 Firebase 콘솔 프로젝트 설정에서 확인하여 직접 입력해 주세요.
            AppOptions options = new AppOptions
            {
                DatabaseUrl = new Uri("https://studynetwork-42c19-default-rtdb.firebaseio.com/"),
                ApiKey = "AIzaSyBzo_zDCkQL-NhucyIAstv3RziWGjseCEI",
                AppId = "1:7557307168:android:aff2cd10f624353abfd545",
                ProjectId = "studynetwork-42c19"
            };

            // 설정된 옵션으로 Firebase App 생성 및 인증 인스턴스 할당
            FirebaseApp app = FirebaseApp.Create(options);
            firebaseAuth = FirebaseAuth.GetAuth(app);
            
            firebaseAuth.StateChanged += OnAuthStateChanged;
            IsFirebaseInitialized = true;
            Debug.Log("[Auth] Firebase Manual Initialization Success.");
        }
        else
        {
            Debug.LogError($"[Auth] Could not resolve all Firebase dependencies: {dependencyStatus}");
        }
    }

    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (firebaseAuth.CurrentUser != currentUser)
        {
            currentUser = firebaseAuth.CurrentUser;
        }
    }

    public async Task<bool> SignInAnonymously()
    {
        if (firebaseAuth == null) return false;
        try
        {
            var authResult = await firebaseAuth.SignInAnonymouslyAsync();
            currentUser = authResult.User;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> UpdateDisplayName(string newName)
    {
        if (currentUser == null) return false;
        UserProfile profile = new UserProfile { DisplayName = newName };
        try
        {
            await currentUser.UpdateUserProfileAsync(profile);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
