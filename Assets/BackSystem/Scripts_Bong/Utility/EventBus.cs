using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> EventListeners = new Dictionary<Type, Delegate>();

    // 이벤트 발행 연산
    public static void Publish<T>(T eventMessage) where T : struct
    {
        Type eventType = typeof(T);

        if (EventListeners.TryGetValue(eventType, out Delegate listenerDelegate))
        {
            Action<T> callback = listenerDelegate as Action<T>;
            callback?.Invoke(eventMessage);
        }
    }

    // 이벤트 구독 등록 처리
    public static void Subscribe<T>(Action<T> listener) where T : struct
    {
        Type eventType = typeof(T);

        if (EventListeners.TryGetValue(eventType, out Delegate existingDelegate))
        {
            EventListeners[eventType] = Delegate.Combine(existingDelegate, listener);
        }
        else
        {
            EventListeners[eventType] = listener;
        }
    }

    // 이벤트 구독 해제 처리
    public static void Unsubscribe<T>(Action<T> listener) where T : struct
    {
        Type eventType = typeof(T);

        if (EventListeners.TryGetValue(eventType, out Delegate existingDelegate))
        {
            Delegate currentDelegate = Delegate.Remove(existingDelegate, listener);

            if (currentDelegate == null)
            {
                EventListeners.Remove(eventType);
            }
            else
            {
                EventListeners[eventType] = currentDelegate;
            }
        }
    }

    // 전체 이벤트 리스너 초기화
    public static void ClearAllListeners()
    {
        EventListeners.Clear();
    }
}
