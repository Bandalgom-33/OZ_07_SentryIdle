using System;
using UnityEngine;

// <T>를 붙여서 자기 자신의 타입을 인스턴스로 반환하도록 변경
public abstract class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();

                // 씬에 인스턴스가 없을 경우 자동 게임오브젝트 생성하여 동적 할당
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    _instance = singletonObject.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}