using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Newtonsoft.Json.Linq;
using UnityEngine;

/* 원래는 지금 내용부터 FireStore(비 관계형 DB)
 * 강의장 네트워크 문제로 해당 부분은 문제가 해결 된 뒤에 진행하겠습니다.
 */

// 네트워크는 절차 및 진행 확인을 위해서
// 기존의 함수 선언 순서 (public -> private) 규칙을 무시하고
// 네트워크 절차대로 선언하여 관리합니다.
// 함수의 실행 순서가 위에서부터 아래로 차례대로 호출된다고 생각하시면서 보세요


public class Study_FireBaseAuth : MonoBehaviour
{
   // Firebae SDK(Software Development Kit)가 어떤 형식으로 이루어져있는지
   // 확인을 하는게 중요합니다.

   private FirebaseApp app; 
   
   public async void Start()
   {
      // Firebase의 초기화 절차
      // 1. App의 종속성을 확인 합니다.
      // 2. 확인이 끝나면 FirebaseApp의 인스턴스를 생성 요청 합니다.(OS에게)
      // (프로그램 하나가 더 뜬다라고 생각하면됨)
      // 3. auth 기능을 수행할 인스턴스를 생성 요청 합니다
      // 4. 초기화 완료가 된겁니다.


      await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
      {
         var result = task.Result;

         // 이용 가능한 상태가 아니라면
         if (result != DependencyStatus.Available)
         {
            // 예외 처리를 해줍니다.
            Debug.LogError($"Firebase Error : {result.ToString()}");
            return;
         }
         
         // 이 시점에서는 Firebase를 이용 가능한 상태가 됩니다.
         try
         {
             app = await LoadFirebaseAppFromStreamingAssetsAsync(Guid.NewGuid().ToString());
         }
         catch (Exception e)
         {
             throw new Exception($"Firebase Error : {e.Message}");
         }
         
         // app자체가 생성 완료된 상황
         Debug.Log($"<color=green> Firebase App loaded </color=green>");

      });
   }

   // appName은 유저의 디바이스 환경에서는 실제 AppName이 되어도 상관은 없으나,
   // Appname을 사용할 경우 여러분들이 Firebase를 이용해서 데스크톱 환경에서
   // 멀티 플레이 게임을 테스트할때 에러가 발생할 수 있습니다.
      public async Task<FirebaseApp> LoadFirebaseAppFromStreamingAssetsAsync(string appName = "CustomApp")
    {
        const string jsonFileNameForMobile = "google-services.json";
        const string jsonFileNameForDesktop = "google-services-desktop.json";

        string jsonFileNameTarget = jsonFileNameForDesktop;
        
        
#if UNITY_ANDROID && !UNITY_EDITOR
// #if UNITY_ANDROID && !UNITY_EDITOR = Android 디바이스에서 실행이 된다는 말입니다.
        jsonFileNameTarget = jsonFileNameForMobile;
#endif
        // 위 내용이 아니라면 데스크톱의 환경이라는 말
        string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileNameTarget);
        
#if UNITY_ANDROID && !UNITY_EDITOR
        var www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
        await SendRequestAsync(www);

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"{jsonFileNameTarget} 읽기 실패: {www.error}");
            return null;
        }

        string jsonText = www.downloadHandler.text;
#else

        if (File.Exists(filePath) == false) 
        {
            Debug.LogError($"{jsonFileNameTarget} 없음.");
            return null;
        }

        string jsonText = await File.ReadAllTextAsync(filePath);
#endif

        JObject root = JObject.Parse(jsonText);

        string projectId = root["project_info"]?["project_id"]?.ToString();
        string storageBucket = root["project_info"]?["storage_bucket"]?.ToString();
        string projectNumber = root["project_info"]?["project_number"]?.ToString();

        var client = root["client"]?[0];
        string appId = client?["client_info"]?["mobilesdk_app_id"]?.ToString();
        string apiKey = client?["api_key"]?[0]?["current_key"]?.ToString();

        if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError($"{jsonFileNameTarget} 파일 확인 필요");
            return null;
        }
        
        // 모든 정보가 유효하다면
        // 해당 내용을 토대로 AppOptions 파일을 생성합니다.
        
        AppOptions options = new AppOptions
        {
            ProjectId = projectId,
            StorageBucket = storageBucket,
            AppId = appId,
            ApiKey = apiKey,
            MessageSenderId = projectNumber
        };
        
        // 만들어낸 Option파일(google-services 파일을 기바능로 FirebaseApp을 생성합니다.)
        FirebaseApp app = FirebaseApp.Create(options, appName);
        return app;
    }



}
