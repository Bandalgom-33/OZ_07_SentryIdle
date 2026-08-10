using System;
using System.Collections.Generic;

// 시스템 간 직접 참조(의존성) 방지를 위한 제네릭 기반 중앙 이벤트 발행/구독 버스
public static class EventBus
{
    // 이벤트 타입별 구독자 대리자(Delegate) 사전 매핑 레지스터
    private static readonly Dictionary<Type, Delegate> EventListeners = new Dictionary<Type, Delegate>();

    // 특정 이벤트 메시지 중앙 발행 연산 (구독 중인 수신자들에게 일괄 메시지 전달)
    public static void Publish<T>(T eventMessage) where T : struct
    {
        Type eventType = typeof(T);

        if (EventListeners.TryGetValue(eventType, out Delegate listenerDelegate))
        {
            Action<T> callback = listenerDelegate as Action<T>;
            callback?.Invoke(eventMessage);
        }
    }

    // 특정 이벤트 타입의 메시지 수신 구독 등록 처리
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

    // 등록된 이벤트 수신 레지스터 구독 해제 처리 (메모리 누수 방지)
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

    // 전체 등록 이벤트 리스너 레지스터 클리어 처리
    public static void ClearAllListeners()
    {
        EventListeners.Clear();
    }
}
