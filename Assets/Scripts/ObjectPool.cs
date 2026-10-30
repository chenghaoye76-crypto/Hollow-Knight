using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    public Dictionary<string, List<GameObject>> objectPool;
    public Transform transform;

    public ObjectPool(Transform transform)
    {
        objectPool = new Dictionary<string, List<GameObject>>();
        this.transform = transform;
    }

    public GameObject GetGameObject(GameObject gameObject)
    {
        string name = gameObject.name;
        if (objectPool.ContainsKey(name))
        {
            if (objectPool[name].Count > 0)
            {
                GameObject obj = objectPool[name][0];
                obj.SetActive(true);
                objectPool[name].RemoveAt(0);
                return obj;
            }
            else
            {
                GameObject obj = CreateObject(gameObject);
                return obj;
            }
        }
        else
        {
            GameObject dic = new GameObject(gameObject.name);
            dic.transform.SetParent(transform);
            dic.transform.localPosition = Vector3.zero;
            dic.transform.localScale = Vector3.one;

            objectPool.Add(name, new List<GameObject>());

            GameObject obj = CreateObject(gameObject);
            return obj;
        }
    }

    public void ReturnGameObject(GameObject obj)
    {
        string name = obj.name.Replace("(Clone)", "");
        obj.SetActive(false);
        obj.transform.SetParent(transform.Find(name));
        objectPool[name].Add(obj);
    }

    public void CleanPool()
    {
        int count = transform.childCount;
        for (int i = count - 1; i >= 0; i--)
        {
            GameObject.Destroy(transform.GetChild(i).gameObject);
        }

        foreach (List<GameObject> list in objectPool.Values)
            list.Clear();

        objectPool.Clear();
    }

    private GameObject CreateObject(GameObject gameObject)
    {
        GameObject obj = GameObject.Instantiate(gameObject);
        obj.transform.SetParent(transform.Find(gameObject.name));
        obj.transform.localScale = new Vector3(Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
        obj.GetComponent<Poolbb>().pool = this;
        return obj;
    }
}
