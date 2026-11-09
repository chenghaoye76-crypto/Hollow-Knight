using UnityEngine;

public class PlayerEffectManager : MonoBehaviour
{
    private ObjectPool pool;
    private PlayerController playerController;
    [Header("hurt")]
    [SerializeField] GameObject hurtSoul;
    [SerializeField] GameObject hurtLight;
    [SerializeField] GameObject hurtBlackGhost;
    [SerializeField] GameObject hurtBlackPoint;
    [SerializeField] GameObject deathEffect;
    [SerializeField] GameObject deadBlackGhost;
    [SerializeField] GameObject deadBlackPoint;
    [SerializeField] GameObject deadBlackPointEx;
    [SerializeField] GameObject head;
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
    [Header("jump")]
    [SerializeField] GameObject wallJumpPuff;
    [SerializeField] GameObject dashPuff;
    [SerializeField] GameObject doubleJumpEffect;
    [SerializeField] ParticleSystem doubleWings;
    [SerializeField] GameObject jumpPuff;
    [SerializeField] GameObject moveJumpPuff;
    [Header("focus")]
    [SerializeField] GameObject burstEffect;
    [SerializeField] GameObject focusEffect;
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
        EventManager.instance.RemoveVoidAction("SoulBrust", name);
    }
    #region Hurt
    public void Hurt()
    {
        if (hurtBlackGhost != null)
            HurtBlackGhost();

        HurtBlackPoint();
        HurtLight();
        HurtSoul();
    }

    private void HurtBlackGhost()
    {
        SlashGhostEffect effect1 = pool.GetGameObject(hurtBlackGhost).GetComponent<SlashGhostEffect>();
        effect1.transform.localPosition = Vector3.zero;
        effect1.AngleOffset = 0;

        SlashGhostEffect effect2 = pool.GetGameObject(hurtBlackGhost).GetComponent<SlashGhostEffect>();
        effect2.transform.localPosition = Vector3.zero;
        effect2.AngleOffset = 180;

        effect1.Play();
        effect2.Play();
    }
    private void HurtBlackPoint()
    {
        ParticleSystem particle1 = pool.GetGameObject(hurtBlackPoint).GetComponent<ParticleSystem>();
        particle1.transform.localPosition = Vector3.zero;
        particle1.transform.localScale = new Vector3(Mathf.Abs(particle1.transform.localScale.x), particle1.transform.localScale.y, particle1.transform.localScale.z);

        ParticleSystem particle2 = pool.GetGameObject(hurtBlackPoint).GetComponent<ParticleSystem>();
        particle2.transform.localPosition = Vector3.zero;
        particle2.transform.localScale = new Vector3(-Mathf.Abs(particle2.transform.localScale.x), particle2.transform.localScale.y, particle2.transform.localScale.z);

        particle1.Play();
        particle2.Play();
    }
    private void HurtLight()
    {
        GameObject obj = pool.GetGameObject(hurtLight);
        obj.transform.localPosition = new Vector3(0, 0, 0);
        obj.transform.localScale = new Vector3(Mathf.Abs(obj.transform.localScale.x), Mathf.Abs(obj.transform.localScale.y), Mathf.Abs(obj.transform.localScale.z));

        float rotationZ = UnityEngine.Random.Range(-30, 30);
        obj.transform.rotation = Quaternion.Euler(0, 0, rotationZ);
    }
    private void HurtSoul()
    {
        GameObject obj = pool.GetGameObject(hurtSoul);
        obj.transform.localPosition = new Vector3(0, 0, 0);
    }
    #endregion

    #region Dead
    private void DeadEffect()
    {
        GameObject obj = pool.GetGameObject(deathEffect);
        obj.transform.localPosition = new Vector3(0, 0, 0);
    }

    private void DeadBlackGhost()
    {
        SlashGhostEffect effect = pool.GetGameObject(deadBlackGhost).GetComponent<SlashGhostEffect>();
        effect.transform.localPosition = Vector3.zero;
        effect.Play();
    }

    private void DeadBlackPoint()
    {
        ParticleSystem particle = pool.GetGameObject(deadBlackPoint).GetComponent<ParticleSystem>();
        particle.transform.localPosition = Vector3.zero;
        particle.Play();
    }

    private void DeadBlackPointEx()
    {
        ParticleSystem particle1 = pool.GetGameObject(deadBlackPointEx).GetComponent<ParticleSystem>();
        particle1.transform.localPosition = Vector3.zero;
        particle1.transform.localScale = new Vector3(Mathf.Abs(particle1.transform.localScale.x), particle1.transform.localScale.y, particle1.transform.localScale.z);
        particle1.Play();

        ParticleSystem particle2 = pool.GetGameObject(deadBlackPointEx).GetComponent<ParticleSystem>();
        particle2.transform.localPosition = Vector3.zero;
        particle1.transform.localScale = new Vector3(-Mathf.Abs(particle1.transform.localScale.x), particle1.transform.localScale.y, particle1.transform.localScale.z);
        particle2.Play();
    }

    public void Head()
    {
        GameObject obj = GameObject.Instantiate(head);
        obj.transform.position = transform.position + new Vector3(0, 1.347f, 0);
    }

    public void Dead()
    {
        DeadBlackPoint();
        DeadEffect();
        DeadBlackGhost();
        HurtLight();
        DeadBlackPoint();
        DeadBlackPointEx();
    }
    #endregion

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

    #region Jump
    public void WallJumpPuff()
    {
        GameObject obj = pool.GetGameObject(wallJumpPuff);
        obj.transform.SetParent(null);
        obj.transform.position = transform.position;
        if (playerController.transform.localScale.x < 0)
        {
            obj.transform.localScale = new Vector3(-Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
            obj.transform.localPosition += new Vector3(0.5f, -0.5f, 0);
        }
        else
        {
            obj.transform.localScale = new Vector3(Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
            obj.transform.localPosition += new Vector3(-0.5f, -0.5f, 0);
        }
    }

    public void DoubleJumpEffect()
    {
        GameObject obj = pool.GetGameObject(doubleJumpEffect);
        obj.transform.localPosition = new Vector3(0, -1, 0);
    }

    public void JumpPuff()
    {
        GameObject obj = pool.GetGameObject(jumpPuff);
        obj.transform.SetParent(null);
        obj.transform.position = transform.position;
        if (playerController.transform.localScale.x < 0)
            obj.transform.localScale = new Vector3(-Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
        else
            obj.transform.localScale = new Vector3(Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
        obj.transform.localPosition += new Vector3(0f, -0.8f, 0);
    }

    public void MoveJumpPuff()
    {
        GameObject obj = pool.GetGameObject(moveJumpPuff);
        obj.transform.SetParent(null);
        obj.transform.position = transform.position;
        if (playerController.transform.localScale.x < 0)
            obj.transform.localScale = new Vector3(-Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
        else
            obj.transform.localScale = new Vector3(Mathf.Abs(obj.transform.localScale.x), obj.transform.localScale.y, obj.transform.localScale.z);
        obj.transform.localPosition += new Vector3(0f, -0.4f, 0);
    }

    public void DoubleWings()
    {
        doubleWings.Play();
    }
    #endregion

    #region Skill
    public void BrustEffect()
    {
        GameObject obj = pool.GetGameObject(burstEffect);
        obj.transform.localPosition = new Vector3(0, -0.5f, 0);
    }
    public void FocusEffect()
    {
        GameObject obj = pool.GetGameObject(focusEffect);
        obj.transform.localPosition = new Vector3(0, 0.7f, 0);
    }
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
    public void QuakeBlast2()
    {
        GameObject obj = pool.GetGameObject(quakeBlast2);
        obj.transform.localPosition = new Vector3(0, -0.4f, 0);
        quakePoint2L.Play();
        quakePoint2R.Play();
    }
    public void QuakePillar2()
    {
        GameObject obj = pool.GetGameObject(quakePillar2);
        obj.transform.localPosition = new Vector3(0, 3, 0);
    }
    public void QuakeMage2()
    {
        GameObject obj = pool.GetGameObject(quakeMega2);
        obj.transform.localPosition = new Vector3(0, -1.4f, 0);
    }
    public void QuakeCharge()
    {
        GameObject obj = pool.GetGameObject(quakeCharge);
        obj.transform.localPosition = new Vector3(-0.1f, -1.2f, 0);
    }







    #endregion
}
