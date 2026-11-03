using UnityEngine;

public class PlayerEffectManager : MonoBehaviour
{
    private ObjectPool pool;
    private PlayerController playerController;
    [Header("attack")]
    [SerializeField] GameObject upAttack;
    [SerializeField] GameObject downAttack;
    [SerializeField] GameObject firstAttack;
    [SerializeField] GameObject secondAttack;
    [SerializeField] GameObject bigAttack;
    [SerializeField] GameObject cycloneAttack;
    [SerializeField] GameObject dashAttack;
    [SerializeField] GameObject NAcharge;
    [SerializeField] GameObject NAcharged;
    [Header("skill")]
    [SerializeField] GameObject fireBallEffect;
    [SerializeField] GameObject fireBall;
    [SerializeField] GameObject fireBallEffect2;
    [SerializeField] GameObject fireBall2;
    [SerializeField] ParticleSystem fireBallTrail2;
    [SerializeField] GameObject screamEffect1;
    [SerializeField] ParticleSystem screamPoint1;
    [SerializeField] GameObject screamBase1;
    [SerializeField] GameObject screamEffect2;
    [SerializeField] ParticleSystem screamPoint2;
    [SerializeField] GameObject screamBase2;
    [SerializeField] GameObject quakeCharge;
    [SerializeField] GameObject quakeBlast1;
    [SerializeField] GameObject quakePillar1;
    [SerializeField] ParticleSystem quakePoint1R;
    [SerializeField] ParticleSystem quakePoint1L;
    [SerializeField] GameObject quakeBlast2;
    [SerializeField] GameObject quakePillar2;
    [SerializeField] GameObject quakeMega2;
    [SerializeField] ParticleSystem quakePoint2R;
    [SerializeField] ParticleSystem quakePoint2L;
    [SerializeField] GameObject pickUpRing;
    [SerializeField] GameObject soulBrust;

    private void OnEnable()
    {
        playerController = GetComponentInParent<PlayerController>();
        pool = new ObjectPool(transform);
    }


    private void OnDisable()
    {
        
    }



    #region Attack



    //普通攻击第一段
    public void FisrtAttack()
    {
        GameObject obj = pool.GetGameObject(firstAttack);
        //if (playerControl.isClimbing)
        //    obj.transform.localScale = new Vector3(-Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
        //else
        obj.transform.localScale = new Vector3(Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
        obj.transform.localPosition = new Vector3(0.2f, 0.25f, 0);
    }


    //普通攻击第二段 
    public void SecondAttack()
    {
        GameObject obj = pool.GetGameObject(secondAttack);
        obj.transform.localPosition = new Vector3(0.4f, 0.15f, 0);
    }


    //向上攻击
    public void UpAttack()
    {
        GameObject obj = pool.GetGameObject(upAttack);
        obj.transform.parent = transform;
        obj.transform.localPosition = new Vector3(0, -2f, 0);
    }


    //向下攻击
    public void DownAttack()
    {
        GameObject obj = pool.GetGameObject(downAttack);
        obj.transform.localPosition = new Vector3(0, 0.6f, 0);
    }

    //超级攻击
    public void BigAttack()
    {
        GameObject obj = pool.GetGameObject(bigAttack);
        obj.transform.localPosition = new Vector3(-1.5f, 1f, 0);
    }

    //旋转攻击
    public void CycloneEffect()
    {
        GameObject obj = pool.GetGameObject(cycloneAttack);
        obj.transform.localPosition = new Vector3(0, -0.3f, 0);
    }


    //冲刺攻击
    public void DashAttack()
    {
        GameObject obj = pool.GetGameObject(dashAttack);
        obj.transform.localPosition = new Vector3(-2f, -0.8f, 0);
    }

    public void NACharge()
    {
        GameObject obj = pool.GetGameObject(NAcharge);
        obj.transform.localPosition = new Vector3(0, -0.2f, 0);
    }

    public void NACharged()
    {
        GameObject obj = pool.GetGameObject(NAcharged);
        obj.transform.localPosition = new Vector3(-0.1f, -0.9f, 0);
    }








    #endregion

    #region Skill
    public void FireBall2()
    {
        GameObject obj = GameObject.Instantiate(fireBall2);
        obj.transform.position = transform.position;
        obj.GetComponent<fireBall>().point = fireBallTrail2;
        if (playerController.transform.localScale.x < 0)
        {
            obj.transform.localScale = new Vector3(obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z);
            obj.transform.localPosition += new Vector3(3f, -0.5f, 0);
        }
        else
        {
            obj.transform.localScale = new Vector3(-obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z);
            obj.transform.localPosition += new Vector3(-3f, -0.5f, 0);
        }
    }
    public void ScreamEffect1()
    {
        GameObject obj = pool.GetGameObject(screamEffect1);
        obj.transform.localPosition = new Vector3(0, -2.5f, 0);
        screamPoint1.transform.localPosition = new Vector3(0, -2.5f, 0);
        screamPoint1.Play();
    }
    public void ScreamBase1()
    {
        GameObject obj = pool.GetGameObject(screamBase1);
        obj.transform.localPosition = new Vector3(0, -0.5f, 0);
    }
    public void ScreamEffect2()
    {
        GameObject obj = pool.GetGameObject(screamEffect2);
        obj.transform.localPosition = new Vector3(0, -4.5f, 0);
        screamPoint1.transform.localPosition = new Vector3(0, -2.5f, 0);
        screamPoint1.Play();
        screamPoint2.transform.localPosition = new Vector3(0, -4.5f, 0);
        screamPoint2.Play();
    }
    public void ScreamBase2()
    {
        GameObject obj = pool.GetGameObject(screamBase2);
        obj.transform.localPosition = new Vector3(0, -0.5f, 0);
    }







    #endregion
}
