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
            firebaseAuth = FirebaseAuth.DefaultInstance;
            firebaseAuth.StateChanged += OnAuthStateChanged;
            IsFirebaseInitialized = true;
            Debug.Log("[Auth] Firebase Initialized.");
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
