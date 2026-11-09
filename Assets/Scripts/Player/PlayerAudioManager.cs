using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField] AudioClip run;
    [SerializeField] AudioClip jump;
    [SerializeField] AudioClip walljump;
    [SerializeField] AudioClip dash;
    [SerializeField] AudioClip shadeDash;
    [SerializeField] AudioClip swing;
    [SerializeField] AudioClip fireball;
    [SerializeField] AudioClip scream1;
    [SerializeField] AudioClip scream2;
    [SerializeField] AudioClip softland;
    [SerializeField] AudioClip hardland;
    [SerializeField] AudioClip wallslide;
    [SerializeField] AudioClip hurt;
    [SerializeField] AudioClip death;
    [SerializeField] AudioClip falling;
    [SerializeField] AudioClip sword1;
    [SerializeField] AudioClip sword2;
    [SerializeField] AudioClip sword3;
    [SerializeField] AudioClip bigSword;
    [SerializeField] AudioClip cycloneSword;
    [SerializeField] AudioClip dashSword;
    [SerializeField] AudioClip foucus;
    [SerializeField] AudioClip foucusGet;
    [SerializeField] AudioClip quakeDash;
    [SerializeField] AudioClip quakePrepare;
    [SerializeField] AudioClip quake1;
    [SerializeField] AudioClip quake2;
    [SerializeField] AudioClip naChargeComplete;
    [SerializeField] AudioClip sdReady;
    [SerializeField] AudioClip sdAirBreak;
    [SerializeField] AudioClip sdDashing;
    [SerializeField] AudioClip sdHitWall;
    [SerializeField] AudioClip sdBrust;
    [SerializeField] AudioClip pickUpAudio;
    [SerializeField] AudioClip pickUpBoomAudio;
    [Header("attack")]
    [SerializeField] AudioClip sword_hit_reject;
    [SerializeField] AudioClip hit_enemy;
    [SerializeField] AudioClip mage_sword;
    AudioSource audioSource;

    private void OnEnable()
    {
        audioSource = GetComponent<AudioSource>();

        EventManager.instance.AddVoidAction("sword_hit_reject", name, SwordHitReject);
        EventManager.instance.AddVoidAction("hit_enemy", name, HitEnemy);
    }

    private void OnDisable()
    {
        EventManager.instance.RemoveVoidAction("hit_enemy", name);
        EventManager.instance.RemoveVoidAction("sword_hit_reject", name);
    }
    //瞬时 jump,walljump,softland,hardland,hurt,death
    //持续 run,wallslide,falling

    public void StopSound()
    {
        audioSource.clip = null;
    }

    public void StopAllSound()
    {
        audioSource.Stop();
    }

    public void RunSound()
    {
        audioSource.clip = run;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void FallingSound()
    {
        audioSource.clip = falling;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void WallSlideSound()
    {
        audioSource.clip = wallslide;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void Foucus()
    {
        audioSource.clip = foucus;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void QuakeDash()
    {
        audioSource.clip = quakeDash;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void SuperDashing()
    {
        audioSource.clip = sdDashing;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void JumpSound()
    {
        StopSound();
        audioSource.PlayOneShot(jump);
    }

    public void SoftLandSound()
    {
        StopSound();
        audioSource.PlayOneShot(softland);
    }

    public void HardLandSound()
    {
        StopSound();
        audioSource.PlayOneShot(hardland);
    }

    public void WallJumpSound()
    {
        StopSound();
        audioSource.PlayOneShot(walljump);
    }

    public void HurtSound()
    {
        StopSound();
        audioSource.PlayOneShot(hurt);
    }

    public void DeathSound()
    {
        StopSound();
        audioSource.PlayOneShot(death);
    }

    public void Sword1()
    {
        audioSource.PlayOneShot(sword1);
    }

    public void Sword2()
    {
        audioSource.PlayOneShot(sword2);
    }

    public void Sword3()
    {
        audioSource.PlayOneShot(sword3);
    }

    public void BigSword()
    {
        audioSource.PlayOneShot(bigSword);
    }

    public void CycloneSword()
    {
        StopSound();
        audioSource.PlayOneShot(cycloneSword);
    }

    public void DashSword()
    {
        audioSource.PlayOneShot(dashSword);
    }

    public void Swing()
    {
        StopSound();
        audioSource.PlayOneShot(swing);
    }

    public void Dash()
    {
        StopSound();
        audioSource.PlayOneShot(dash);
    }

    public void ShadeDash()
    {
        StopSound();
        audioSource.PlayOneShot(shadeDash);
    }

    public void FireBall()
    {
        StopSound();
        audioSource.PlayOneShot(fireball);
    }

    public void Scream1()
    {
        StopSound();
        audioSource.PlayOneShot(scream1);
    }

    public void Scream2()
    {
        StopSound();
        audioSource.PlayOneShot(scream2);
    }

    public void FoucusGet()
    {
        StopSound();
        audioSource.PlayOneShot(foucusGet);
    }

    public void Quake1()
    {
        StopSound();
        audioSource.PlayOneShot(quake1);
    }

    public void Quake2()
    {
        StopSound();
        audioSource.PlayOneShot(quake2);
    }
    public void QuakePrepare()
    {
        StopSound();
        audioSource.PlayOneShot(quakePrepare);
    }

    public void NAChargeComplete()
    {
        audioSource.PlayOneShot(naChargeComplete);
    }

    public void SwordHitReject()
    {
        audioSource.PlayOneShot(sword_hit_reject);
    }
    private void HitEnemy()
    {
        audioSource.PlayOneShot(hit_enemy);
    }

    public void SDBrust()
    {
        audioSource.PlayOneShot(sdBrust);
    }

    public void SDReady()
    {
        audioSource.PlayOneShot(sdReady);
    }

    public void SDAirBreak()
    {
        StopSound();
        audioSource.PlayOneShot(sdAirBreak);
    }

    public void SDHitWall()
    {
        StopSound();
        audioSource.PlayOneShot(sdHitWall);
    }

    public void PickUpAudio()
    {
        StopSound();
        audioSource.PlayOneShot(pickUpAudio);
    }

    public void PickUpBoomAudio()
    {
        StopSound();
        audioSource.PlayOneShot(pickUpBoomAudio);
    }

    public void MageSword()
    {
        audioSource.PlayOneShot(mage_sword);
    }
}
