using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    private ObjectPool pool;

    public void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        pool = new ObjectPool(transform);
    }

    [SerializeField] GameObject blackGrass;
    [SerializeField] GameObject windGrass;

    [Header("strike")]
    [SerializeField] GameObject block;
    [SerializeField] GameObject enemyShot;
    [SerializeField] GameObject healthGet;
    [SerializeField] GameObject impactLine;
    [SerializeField] GameObject nailClash;
    [SerializeField] GameObject pierceSpike;
    [SerializeField] GameObject slam;
    [SerializeField] GameObject slamFast;
    [SerializeField] GameObject slahGhost1;
    [SerializeField] GameObject slahGhost2;
    [SerializeField] GameObject slashImpact1;
    [SerializeField] GameObject slashImpact2;
    [SerializeField] GameObject slashShade;
    [SerializeField] GameObject splat;
    [SerializeField] GameObject splat2;
    [SerializeField] GameObject sword;
    [SerializeField] GameObject thunk;
    [SerializeField] GameObject thunkfast;
    [SerializeField] GameObject hitNormalGround;

    [Header("patical")]
    [SerializeField] GameObject impact;


    private void OnEnable()
    {
        EventManager.instance.AddTransformAction("blackGrassEffect", "blackGrass", BlackGrass);
        EventManager.instance.AddTransformAction("windGrassEffect", "windGrass", WindGrass);
    }

    private void OnDisable()
    {
        EventManager.instance.RemoveTransformAction("blackGrassEffect", "blackGrass");
        EventManager.instance.RemoveTransformAction("windGrassEffect", "windGrass");
    }
    public void BlackGrass(Transform trans)
    {
        GameObject gameObject = pool.GetGameObject(blackGrass);
        gameObject.transform.position = trans.position;
        gameObject.GetComponent<ParticleSystem>().Play();
    }

    public void WindGrass(Transform trans)
    {
        GameObject gameObject = pool.GetGameObject(windGrass);
        gameObject.transform.position = trans.position;
        gameObject.GetComponent<ParticleSystem>().Play();
    }

    #region Attack
    public void NailClash(Vector3 position, int faceDir)
    {
        GameObject obj = pool.GetGameObject(nailClash);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(faceDir, 1, 1);
    }

    public void SlamImpact1(Vector3 position, int faceDir, float size, float angle)
    {
        GameObject obj = pool.GetGameObject(slashImpact1);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        obj.transform.localScale = new Vector3(faceDir, 1, 1) * size;
    }

    public void SlamImpact2(Vector3 position, int faceDir, float size, float angle)
    {
        GameObject obj = pool.GetGameObject(slashImpact2);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        obj.transform.localScale = new Vector3(faceDir, 1, 1) * size;
    }

    public void Sword(Vector3 position, int faceDir, float size, float angle)
    {
        GameObject obj = pool.GetGameObject(sword);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        obj.transform.localScale = new Vector3(faceDir, 1, 1) * size;
    }

    public void Block(Vector3 position, float Rotation = -90)
    {
        GameObject obj = pool.GetGameObject(block);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(0, 0, Rotation);
    }

    public void HitNormalGround(Vector3 position, float angle)
    {
        GameObject obj = pool.GetGameObject(hitNormalGround);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void Splat(Vector3 postion, int dir, float size)
    {
        GameObject obj = pool.GetGameObject(splat);
        obj.transform.position = postion;
        obj.transform.localScale = new Vector3(dir, 1, 1) * size;
    }

    public void EnemyShot(Vector3 position, int dir, float size)
    {
        GameObject obj = pool.GetGameObject(enemyShot);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(dir, 1, 1) * size;
    }

    public void Slam(Vector3 position, float size)
    {
        GameObject obj = pool.GetGameObject(slam);
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * size;
    }
    #endregion

    public GameObject CreateEffect(GameObject obj)
    {
        GameObject gameObj = pool.GetGameObject(obj);
        return gameObj;
    }

    public void ClearPool()
    {
        pool.CleanPool();
    }
}
