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
    public void RemoveVoidAction(string eventName)
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


}
