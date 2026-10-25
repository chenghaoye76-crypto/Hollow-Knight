using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
/// <summary>
/// 存储所有UI信息，并可以创建或者销毁UI
/// </summary>
public class UIManager
{
    /// <summary>
    /// 存储所有UI信息的字典，每一个UI信息都会对应一个GameObject
    /// </summary>
    private Dictionary<UIType, GameObject> dicUI;
    public string canvasPath;
    public UIManager()
    {
        dicUI = new Dictionary<UIType, GameObject>();
    }
    /// <summary>
    /// 获取一个UI对象
    /// </summary>
    /// <param name="type">UI信息</param>
    /// <returns></returns>
    public GameObject GetSingleUI(UIType type)
    {
        GameObject parent = GameObject.Find("Canvas");
        if(!parent)
        {
            Debug.LogError("Canvas不存在，请仔细查找有无这个对象");
            return null;
        }
        if (dicUI.ContainsKey(type)) 
        {
            return dicUI[type];
        }
        GameObject ui = GameObject.Instantiate(Resources.Load<GameObject>(type.Path), parent.transform);
        return ui;
    }
    /// <summary>
    /// 销毁一个UI对象
    /// </summary>
    /// <param name="type">UI信息</param>
    public void DestroyUI(UIType type)
    {
        if (dicUI.ContainsKey(type))
        {
            
            dicUI.Remove(type);
        }
    }
}
