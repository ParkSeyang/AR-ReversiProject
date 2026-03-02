using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.UI;


public class Study_UniTask : MonoBehaviour
{
    // 여러분이 UniTask로 코루틴을 완벽히 대체할 수 있다면 = 코루틴을 안쓰고 개발이 가능하면
    // UniTask에 익숙해진겁니다.

    // UniTask를 사용하는 방법은 대부분 Task => UniTask로 바꿔버리면 됩니다.
    // 내부 동작이 달라질 뿐이라서 그렇습니다. 기반은 C# Task 기반입니다.
    // 대신에 +@ 유니티 관련한 기능들이 추가되어 있습니다.
    
    // 연습하고 싶을때 팁
    // UniTask는 (.Net의 비동기 처리) + (유니티와 연관된 추가 기능)
    // PS : Task를 상속받아서 유니티의 특징을 추가했다! 로 이해하시면 좋습니다.
    // UniTask는 GC가 없다. 제로 얼로케이션이다. 라는것을 스터디하시면 됩니다.

    private void Start()
    {
        MouseClickLoop();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DownloadImage();
        }

        if (Input.GetMouseButtonDown(0))
        {
            
        }
    }
    
    // 비동기 로직들이 가장 많이 쓰이는 곳은 언제 작업이 끝날지 모를때 대기하는 용도로 사용됩니다 

    private const string IMAGE_URL = "https://picsum.photos/500";
    //요 도메인은 사진을 다운받는 도메인입니다. https://picsum.photos/{size}

    public RawImage rawImage;
    
    //요 함수를 실행하면 이미지를 다운로드 해서 보여줍니다.
    //다운로드가 완료되면 rawImage의 이미지가 갱신이 됩니다
    public async void DownloadImage()
    {
        Debug.Log($"다운로드를 시작합니다 {DateTime.Now:HH:mm:ss}");

        // await Task.Delay(2000); 
         await UniTask.Delay(2000);
        // try catch 구문 => 예외 발생시 함수를 통제하기 위한 기법
        // try 블록을 시도해보고, 예외가 throw되면 catch 부분을 실행합니다.
        
        try
        {
            Texture2D texture = await GetTextureAsync(IMAGE_URL);
            rawImage.texture = texture;
            Debug.Log($"이미지를 적용했습니다. {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception e) 
        {
            Debug.LogError(e);
        }
    }

    // Task<반환형> 으로 읽으시면 됩니다
    // 아래 함수는 비동기로 텍스쳐를 매개변수 url에서 다운받아 return하는 함수가 됩니다.
    
    private async UniTask<Texture2D> GetTextureAsync(string url)
    {
        using (UnityWebRequest rq = UnityWebRequestTexture.GetTexture(url))
        {
            // await rq.SendWebRequest(); UniTask를 사용하지 않는 일반 방법
            await rq.SendWebRequest().ToUniTask(); // UniTask를 사용하는 방법

            if (rq.result != UnityWebRequest.Result.Success)
            {
                throw new Exception(rq.error);
                // 프로그램이 실행중 throw 키워드를 만나면 해당 함수를 강제로 종료해버리고
                // 다음 작업을 이어 나갑니다.
                // 에러를 핸들링하는 방법중에 하나입니다.
            }

            return DownloadHandlerTexture.GetContent(rq);
        }
    }

    private async void MouseClickLoop()
    {
        CancellationToken token = this.destroyCancellationToken;
        
        // 부연설명 : try - catch가 호출비용이 조금 있는편입니다.
        try
        {
            // 만약 토큰이 살아있다면?
            while (token.IsCancellationRequested == false)
            {
                Func<bool> onMouseClick = () => Input.GetMouseButtonDown(0);
                await UniTask.WaitUntil(onMouseClick, cancellationToken: token);
                // OnClick?.Invoke({메서드 또는 변수명}) 이런식으로 이벤트를 발생시켜도 됩니다.
                Debug.Log($"Click!!");
                await UniTask.Yield(cancellationToken: token);

               // UniTask.Yield(PlayerLoopTiming.Update); => Update Frame만큼(다음 Update까지)
               // UniTask.Yield(PlayerLoopTiming.FixedUpdate); => 물리처리 Frame만큼 대기(다음 FixedUpdate까지)
            }
        }
        catch (Exception e)
        {
            return;
        }
       
        
        
       
    }


}
