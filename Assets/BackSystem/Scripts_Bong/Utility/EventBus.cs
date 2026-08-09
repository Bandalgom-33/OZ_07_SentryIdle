using System;
using System.Collections.Generic;
using UnityEngine;


public static class EventBus
{
    
    private static readonly Dictionary<Type, Delegate> EventTable = new Dictionary<Type, Delegate>();

    // 이벤트 구독
    public static void Subscribe<T>(Action<T> listener)
    {
        if (listener == null) return;
        
        // 타입은 EventTypes에 저장한 타입을 사용
        Type eventType = typeof(T);
        
        // 해당 타입의 델리게이트가 이미 있는지 확인하고 연결 또는 초기 생성
        if (EventTable.TryGetValue(eventType, out Delegate existingDelegate))
        {
            EventTable[eventType] = Delegate.Combine(existingDelegate, listener);
        }
        else
        {
            EventTable[eventType] = listener;
        }
    }
    
    // 이벤트 구독취소 
    public static void Unsubscribe<T>(Action<T> listener)
    {
        if (listener == null) return;
    
        // 타입은 EventTypes에 저장한 타입을 사용
        Type eventType = typeof(T);

        // 델리게이트에서 열결 해제 이후 남아있는 연결 확인후 없다면 이벤트 테이블에서 삭제
        if (EventTable.TryGetValue(eventType, out Delegate existingDelegate))
        {
            
            Delegate currentDelegate = Delegate.Remove(existingDelegate, listener);
            
            if (currentDelegate == null)
            {
                EventTable.Remove(eventType);
            }
            else
            {
                EventTable[eventType] = currentDelegate;
            }
        }
    }
    
    // 이벤트 발생 
    public static void Publish<T>(T eventData)
    {
        Type eventType = typeof(T);

        // 이벤트 발행하기전 이벤트가 이벤트 테이블에 있는지 확인 후 다운캐스팅을 시도 하여 이벤트 발생 및 실패
        if (EventTable.TryGetValue(eventType, out Delegate existingDelegate))
        {
            if (existingDelegate is Action<T> action)
            {
                action.Invoke(eventData);
            }
            else
            {
                Debug.LogError($"[EventBus] 이벤트 타입 {eventType}의 델리게이트 형변환에 실패했습니다.");
            }
        }
    }
    
    public static void ClearAll()
    {
        EventTable.Clear();
    }
}

