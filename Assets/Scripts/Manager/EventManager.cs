using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{
    public static EventManager instance;

    private Dictionary<string, Dictionary<string, UnityAction>> voidEventDic = new Dictionary<string, Dictionary<string, UnityAction>>();
    private Dictionary<string, Dictionary<string, UnityAction<Transform>>> transformEventDic = new Dictionary<string, Dictionary<string, UnityAction<Transform>>>();
    private Dictionary<string, Dictionary<string, UnityAction<UnityAction>>> unityActionEventDic = new Dictionary<string, Dictionary<string, UnityAction<UnityAction>>>();
    private Dictionary<string, Dictionary<string, UnityAction<int>>> intEventDic = new Dictionary<string, Dictionary<string, UnityAction<int>>>();
    private Dictionary<string, Dictionary<string, UnityAction<Vector3>>> vector3EventDic = new Dictionary<string, Dictionary<string, UnityAction<Vector3>>>();
    private Dictionary<string, Dictionary<string, UnityAction<float>>> floatEventDic = new Dictionary<string, Dictionary<string, UnityAction<float>>>();

    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else
            Destroy(gameObject);
    }
    #region voidEvent
    /// <summary>
    /// 添加事件
    /// </summary>
    /// <param name="eventName">
    /// 事件名
    /// </param>
    /// <param name="name">
    /// 相应事件的物体的名称
    /// </param>
    /// <param name="action">
    /// 要添加的事件
    /// </param>
    public void AddVoidAction(string eventName, string name, UnityAction action)
    {
        if (voidEventDic.ContainsKey(eventName))
        {
            if(voidEventDic[eventName].ContainsKey(name))
                voidEventDic[eventName][name] = action;
            else
                voidEventDic[eventName].Add(name, action);
        }
        else
        {
            voidEventDic.Add(eventName, new Dictionary<string, UnityAction>());
            voidEventDic[eventName].Add(name, action);
        }
    }
    public void RemoveVoidAction(string eventName, string name)
    {
        if (!voidEventDic.ContainsKey(eventName))
            return;
        voidEventDic.Remove(eventName);
    }
    public void RemoveVoidEvent(string eventName)
    {
        if (!voidEventDic.ContainsKey(eventName))
            return;
        voidEventDic.Remove(eventName);
    }
    public void VoidEventInvoke(string eventName)
    {
        if (!voidEventDic.ContainsKey(eventName))
            return;
        foreach (UnityAction action in voidEventDic[eventName].Values)
        {
            action?.Invoke();
        }
    }
    #endregion 

    #region TransformEvent
    public void AddTransformAction(string eventName, string name, UnityAction<Transform> action)
    {
        if (transformEventDic.ContainsKey(eventName))
        {
            transformEventDic[eventName].Add(name, action);
        }
        else
        {
            transformEventDic.Add(eventName, new Dictionary<string, UnityAction<Transform>>());
            transformEventDic[eventName].Add(name, action);
        }
    }

    public void RemoveTransformAction(string eventName, string name)
    {
        if (!transformEventDic.ContainsKey(eventName))
            return;
        transformEventDic[eventName].Remove(name);
        if (transformEventDic[eventName].Count == 0)
            RemoveTransformEvent(eventName);
    }
    public void RemoveTransformEvent(string eventName)
    {
        if (!transformEventDic.ContainsKey(eventName))
            return;
        transformEventDic.Remove(eventName);
    }
    public void TransformEventInvoke(string eventName, Transform trans)
    {
        if (!transformEventDic.ContainsKey(eventName))
            return;
        foreach (UnityAction<Transform> action in transformEventDic[eventName].Values)
        {
            action.Invoke(trans);
        }
    }
#endregion

    #region IntEvent
    public void AddIntAction(string eventName, string name, UnityAction<int> action)
    {
        if (intEventDic.ContainsKey(eventName))
        {
            intEventDic[eventName].Add(name, action);
        }
        else
        {
            intEventDic.Add(eventName, new Dictionary<string, UnityAction<int>>());
            intEventDic[eventName].Add(name, action);
        }
    }
    public void RemoveIntAction(string eventName, string name)
    {
        if (!intEventDic.ContainsKey(eventName))
            return;
        intEventDic[eventName].Remove(name);
        if (intEventDic[eventName].Count == 0)
            RemoveIntEvent(eventName);
    }
    public void RemoveIntEvent(string eventName)
    {
        if (!intEventDic.ContainsKey(eventName))
            return;
        intEventDic.Remove(eventName);
    }
    public void IntEventInvoke(string eventName, int n)
    {
        if (!intEventDic.ContainsKey(eventName))
            return;
        foreach (UnityAction<int> action in intEventDic[eventName].Values)
        {
            if (action != null)
                action.Invoke(n);
        }
    }
    #endregion


}
