using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class EventSystemManager : MonoBehaviour
{
    public static EventSystemManager instance;

    private EventSystem eventSystem;
    private InputSystemUIInputModule inputModule;

    public GameObject lastGameObject;
    [HideInInspector] public GameObject lockGameObject;
    [HideInInspector] public bool locked;
    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else
            Destroy(gameObject);
        eventSystem = GetComponent<EventSystem>();
        inputModule = GetComponent<InputSystemUIInputModule>();
    }
    private void Update()
    {
        if (lastGameObject && !locked)
        {
            Currentgame();
        }
        else if (lastGameObject && locked)
        {
            LockGameObject();
        }
    }

    private void Start()
    {
        //inputModule.actionsAsset = BindingController.instance.playerInputAction.asset;
    }

    // 限制鼠标点击
    private void Currentgame()
    {
        if (eventSystem.currentSelectedGameObject)
        {
            lastGameObject = eventSystem.currentSelectedGameObject;
        }

        if (!eventSystem.currentSelectedGameObject)
        {
            eventSystem.SetSelectedGameObject(lastGameObject);
        }
    }
    private void LockGameObject()
    {
        if (eventSystem.currentSelectedGameObject != lockGameObject)
        {
            eventSystem.SetSelectedGameObject(lockGameObject);
            lastGameObject = lockGameObject;
        }
    }

}