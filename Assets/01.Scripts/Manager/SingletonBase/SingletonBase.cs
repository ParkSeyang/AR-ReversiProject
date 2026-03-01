using UnityEngine;

public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;
    private static readonly object lockObj = new object();
    private static bool applicationIsQuitting = false;
    private static bool isInitialized = false;

    public static bool IsInitialized => isInitialized && instance != null;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                return null;
            }

            lock (lockObj)
            {
                if (instance == null)
                {
                    instance = UnityEngine.Object.FindAnyObjectByType<T>();

                    if (instance == null)
                    {
                        var singletonObject = new GameObject(typeof(T).Name);
                        instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject);
                    }
                    else
                    {
                        if (instance.transform.parent == null)
                        {
                            DontDestroyOnLoad(instance.gameObject);
                        }
                    }

                    if (isInitialized == false)
                    {
                        (instance as SingletonBase<T>)?.OnInitialize();
                        isInitialized = true;
                    }
                }

                return instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnInitialize();
            isInitialized = true;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit() => applicationIsQuitting = true;

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            OnDispose();
            instance = null;
            isInitialized = false;
        }
    }

    protected virtual void OnInitialize() { }
    protected virtual void OnDispose() { }

    public static void ResetInstance()
    {
        if (instance != null)
        {
            (instance as SingletonBase<T>)?.OnDispose();

#if UNITY_EDITOR
            DestroyImmediate((instance as MonoBehaviour)?.gameObject);
#else
            Destroy((instance as MonoBehaviour)?.gameObject);
#endif
            instance = null;
            isInitialized = false;
            applicationIsQuitting = false;
        }
    }
}