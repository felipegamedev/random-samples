using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventsManager : Singleton<EventsManager>
{
    protected Dictionary<string, UnityEvent<object>> eventDictionary = new Dictionary<string, UnityEvent<object>>();

    public void AddListener(string p_eventName, UnityAction<object> p_listener)
    {
        if (!eventDictionary.ContainsKey(p_eventName))
        {
            eventDictionary.Add(p_eventName, new UnityEvent<object>());
        }

        eventDictionary[p_eventName].AddListener(p_listener);
    }

    public void RemoveListener(string p_eventName, UnityAction<object> p_listener)
    {
        if (eventDictionary.ContainsKey(p_eventName))
        {
            eventDictionary[p_eventName].RemoveListener(p_listener);
        }
    }

    public void ClearEvent(string p_eventName)
    {
        if (eventDictionary.ContainsKey(p_eventName))
        {
            eventDictionary.Remove(p_eventName);
        }
    }

    public void TriggerEvent(string p_eventName, object p_param = null)
    {
        if (eventDictionary.ContainsKey(p_eventName))
        {
            eventDictionary[p_eventName].Invoke(p_param);
        }
    }

    public Coroutine TriggerEventDelayed(string p_eventName, float p_delay, object p_param = null)
    {
        return WaitForSeconds(p_delay, () =>
        {
            TriggerEvent(p_eventName, p_param);
        });
    }

    protected override void OnAwake()
    {
        base.OnAwake();
        // Initialization here.
        Instance.gameObject.name = "EventsManager";
    }

    private Coroutine WaitForSeconds(float p_seconds, Action p_onComplete)
    {
        return StartCoroutine(WaitForSecondsCoroutine(p_seconds, p_onComplete));
    }

    private IEnumerator WaitForSecondsCoroutine(float p_seconds, Action p_onComplete)
    {
        yield return new WaitForSeconds(p_seconds);

        p_onComplete?.Invoke();
    }
}