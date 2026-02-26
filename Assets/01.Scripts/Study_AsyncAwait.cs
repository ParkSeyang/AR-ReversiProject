using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class Study_AsyncAwait : MonoBehaviour
{
    /* 비동기 프로그래밍
     * 로직을 진행중에 잠시 멈췄다가,
     * 어떤 특정 조건을 만족 했을때 다시 실행되도록 프로그래밍 하는 방법
     * Ex) 여기서 트리거는 { n초 뒤에, ~작업이 완료되면, bool값이 false가 되면 등등 
     * 비동기 프로그래밍 방식중에 가장 간단한 방식이 Coroutine입니다.
     * 이제부터는 C#의 대표적인 방식(Async/await/Task)과, Unity(UniTask)에서의 방식을 알게될겁니다
     * 
     * 유니티와 C#에서 사용되는 비동기 프로그래밍 방법 3가지
     * 1. 코루틴 기반 => 정확히 말하면 비동기같은 동기임
     * 2. .Net의 async/ await / Task 기반
     * 3. UniTask 기반
     *
     * 1번 코루틴은 결국 게임로직내에서 구동되는것 라이프 사이클을 가지고 있어서 비동기라고 하기에는 표현이 부정확함
     * 2번과 3번 방법이 진짜 비동기 프로그래밍이라고 봐도 좋습니다.
     *
     * 비동기의 메모리 작동 방식
     * 비동기 함수가 실행될때는 Stack 메모리에서 실행되었다가 Heap메모리로 옮겨집니다.
     * 그후 가비지 콜렉터가 비동기 함수가 끝날때까지 계속 로직을 감시합니다
     * 비동기 함수가 끝나게되면 다시 Stack으로 다시 넣어주고 비동기함수를 종료시킵니다.
     *
     * 네트워크와 관련된 거의 대부분의 API들은 둘중 하나를 사용해서 연동이 됩니다.
     * 유니티 코루틴을 기반으로 웹 요청(UnityWebRequest)을 기다리거나,
     * Task Or UniTask 라는것을 이용해서 웹 요청(UnityWebRequest)을 기다립니다.
     *
     * C# Task를 알아야 하는 이유는 플랫폼사에서 제공하는 SDK들이 대부분 C# 기반으로 되어있습니다.
     * UniTask가 유니티에 최적화 되어있지만, 다른 플랫폼과 연동성이 떨어집니다.
     * 여러분은 두가지 방법을 모두 공부하시는걸 추천드립니다. (Task / UniTask)
     */ 
    private bool check = false;
    
    public void Start()
    {
        // StudyAsyncAwait();
        int randNum = Random.Range (1000, 3000);
        
        Debug.Log($"randNum: {randNum}");
        
        StudyAsyncAwait2(randNum);
        
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Study_asyncWithTask();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            check = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DownloadImage();
        }

    }
    
    private async void StudyAsyncAwait()
    {
        /* C#(= .Net)에서의 비동기 처리는 Task(작업의 한 단위)를 기반으로 이루어 집니다.
         * Task는 프로그래머가 구성한 행동(함수)들의 묶음 단위를 나타냅니다.
         * 가장 흔한 방식은 Delay입니다.
         */
        
        // async : 비동기임을 나타내는 키워드 입니다.
        // await : 기다려야 한다를 나타내는 키워드
        
        
        Debug.Log($"Study Async Await 함수 대기 시작 : {DateTime.Now:HH:mm:ss}");
        
        await Task.Delay(1000); // ms = 밀리세컨드 단위 = 1초(1000ms)
        
        Debug.Log($"Study Async Await 함수 대기 끝 : {DateTime.Now:HH:mm:ss}");
        
    }

    private async void StudyAsyncAwait2(int delayTime)
    {
        
        Debug.Log($"Study Async Await2 함수 대기 시작 : {DateTime.Now:HH:mm:ss}");
        
        await Task.Delay(delayTime); // ms = 밀리세컨드 단위 = 1초(1000ms)
        
        Debug.Log($"Study Async Await2 함수 대기 끝 : {DateTime.Now:HH:mm:ss}");
        
    }

    

    private async Task WaitBool()
    {
        check = false;
        while (check == false)
        {
            await Task.Delay(100);
        }
        
    }

    private async void Study_asyncWithTask()
    {
        check = false;
        Debug.Log($"현재 check {check} : 대기 시작 {DateTime.Now:HH:mm:ss}");
        await WaitBool();
        Debug.Log($"현재 check {check} : 대기 종료 {DateTime.Now:HH:mm:ss}");
    }

    // 비동기 로직들이 가장 많이 쓰이는 곳은
    // 언제 작업이 끝날지 모를때 
    
    private const string IMAGEURL = "https://picsum.photos/500";
    // 요 도메인은 사진을 다운받는 도메인 입니다. "https://picsum.photos/{size}";

    public RawImage rawImage;
    
    // DownloadImage() 함수를 실행하면 이미지를 다운로드 해서 보여줍니다.
    // 다운로드가 완료되면 rawImage의 이미지가 갱신이 됩니다
    public async void DownloadImage()
    {
        Debug.Log($"다운로드를 시작합니다. {DateTime.Now:HH:mm:ss}");

        await Task.Delay(2000); // 딜레이를 추가함.
        
        // try catch 구문 => 예외 발생시 함수를 통제하기 위한 기법
        // try 블록을 시대호보고, 예외가 throw되면 catch부분을 실행합니다.
        
        try
        {
            Texture2D texture = await GetTextureAsync(IMAGEURL);
            rawImage.texture = texture;
            Debug.Log($"이미지를 적용했습니다. {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception error)
        {
            Debug.LogError(error);
        }
        
        
    }

    // Task<반환형> 으로 읽으시면 됩니다.
    // 아래 함수는 비동기로 텍스쳐를 매개변수 url에서 다운받아 return하는 함수가 됩니다.
    
    private async Task<Texture2D> GetTextureAsync(string url)
    {
        using (UnityWebRequest site = UnityWebRequestTexture.GetTexture(url))
        {
            await site.SendWebRequest();
            if (site.result != UnityWebRequest.Result.Success)
            {
                throw new Exception(site.error);
                // 프로그램이 실행중에 throw 키워드를 만나면 해당 함수를 강제로 종료해버리고
                // 다음 작업을 이어 나갑니다.
                
                // 에러를 핸들링하는 방법중에 하나입니다.
            }
            
            return DownloadHandlerTexture.GetContent(site);
        }
    }

    void StudyNote()
    {
        Debug.Log("test AyncOne ");
        Debug.Log("Hello World!"); // 첫번째 라인이 출력이 된 직후에
        Debug.Log("Hello World!"); // 두번째 라인을 바로 작업을 함.

        
        int number;
        // 해당 시점에서 잠깐 멈췃다가 number에 뭔가가 할당이 되면 number를 출력
        // 하지만 StudyAsync()는 진행이 되어야함.
        number = 123;
        Debug.Log($"{number}");  
        
        // 특정 라인을 컴퓨터가 작업을 하고 있을때 다른작업을 할 수 가 없다.
        // 하지만 여러분들도 비동기 함수들을 써봤습니다.
        
        // ex) Console.ReadLine() => 사용자가 문자열을 입력하고 Enter를 입력해야 다시 코드가 실행이 됨.

        string inputStr = Console.ReadLine();
        Debug.Log($"입력한 문자열은 {inputStr} 입니다");

    }


   
}
