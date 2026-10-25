using UnityEngine;

// Actual action callbacks play transients; movement states drive separate looping sources.
[DisallowMultipleComponent]
public sealed class KnightAudio : MonoBehaviour
{
    [Range(0, 1)] public float actionVolume = 0.75f;
    [Range(0, 1)] public float footstepsVolume = 0.32f;
    [Range(0, 1)] public float wallSlideVolume = 0.30f;
    private PlayerController player;
    private Rigidbody2D body;
    private AudioSource actions, impacts, footsteps, slide, falling;
    private AudioClip jump, wings, wallJump, dash, softLand, hardLand, hurt, death, hit, terrainHit, rest;
    private AudioClip[] slashes;
    private int previousSlash = -1;
    private float peakFallSpeed, nextImpact;
    private bool airborne, fallPlayed, paused, dying;

    private static AudioClip Clip(string name) => Resources.Load<AudioClip>("Audio/" + name);
    private void Awake()
    {
        player = GetComponent<PlayerController>(); body = GetComponent<Rigidbody2D>();
        jump=Clip("Player/PlayerJump"); wings=Clip("Player/hero_wings"); wallJump=Clip("Player/PlayerWallJump"); dash=Clip("Player/hero_dash");
        softLand=Clip("Player/PlayerSoftLand"); hardLand=Clip("Player/PlayerHardLand"); hurt=Clip("Player/PlayerInjured"); death=Clip("Player/PlayerDeath");
        hit=Clip("Enemy/EnermyDamageAudio"); terrainHit=Clip("object/sword_hit_reject"); rest=Clip("UI/UIHint_Save");
        slashes=new[]{Clip("Player/sword_2"),Clip("Player/sword_3"),Clip("Player/sword_4")};
        actions=Source("Knight actions", actionVolume);
        impacts=Source("Knight impacts", actionVolume);
        footsteps=Source("Knight footsteps", footstepsVolume, Clip("Player/PlayerRun"));
        slide=Source("Knight wall slide", wallSlideVolume, Clip("Player/PlayerWallSlide"));
        falling=Source("Knight falling", 0.25f); falling.clip=Clip("Player/PlayerFalling");
    }
    private AudioSource Source(string label, float volume, AudioClip loop=null)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);
        var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
        source.volume=volume;source.loop=loop!=null;source.clip=loop;source.priority=80;
        // The channel applies Sound Volume and keeps OptionsRuntime from scaling this source twice.
        go.AddComponent<OptionsAudioChannel>().music=false;
        return source;
    }
    private static void OneShot(AudioSource source,AudioClip clip,float volume=1)
    {
        if(source!=null && clip!=null && Time.timeScale>0)source.PlayOneShot(clip,volume);
    }
    public void Jump(string animation)
    {
        airborne=true;peakFallSpeed=0;fallPlayed=false;falling.Stop();
        footsteps.Stop();slide.Stop();
        OneShot(actions,animation=="SlideJump"?wallJump:animation=="DoubleJump"?wings:jump);
    }
    public void Dash()
    {
        footsteps.Stop();slide.Stop();falling.Stop();OneShot(actions,dash);
    }
    public void Land()
    {
        if(airborne && !dying)OneShot(impacts,peakFallSpeed>=14?hardLand:softLand,peakFallSpeed>=14?0.85f:0.7f);
        airborne=fallPlayed=false;peakFallSpeed=0;falling.Stop();
    }
    public void Slash()
    {
        int index=Random.Range(0,slashes.Length-1);
        if(index>=previousSlash)index++;
        previousSlash=index;OneShot(actions,slashes[index]);
    }
    public void Hit(bool terrain=false)
    {
        if(Time.time<nextImpact)return;
        nextImpact=Time.time+0.06f;OneShot(impacts,terrain?terrainHit:hit,terrain?0.65f:0.85f);
    }
    public void Hurt(bool fatal=false)
    {
        StopMovement();
        if(fatal){dying=true;actions.Stop();impacts.pitch=0.82f;}
        else impacts.pitch=1;
        OneShot(impacts,fatal?death:hurt);
    }
    public void Rest() { StopMovement();OneShot(actions,rest,0.55f); }
    private void Loop(AudioSource source,bool active)
    {
        if(active && source.clip!=null){if(!source.isPlaying)source.Play();}
        else if(source.isPlaying)source.Stop();
    }
    private void Update()
    {
        bool pauseNow=Time.timeScale<=0;
        if(pauseNow!=paused)
        {
            paused=pauseNow;
            foreach(var source in new[]{actions,impacts,footsteps,slide,falling})
                if(paused)source.Pause();else source.UnPause();
        }
        if(paused)return;
        if(player==null || body==null || !player.enabled || dying){StopMovement();return;}
        float speed=Mathf.Abs(body.linearVelocity.x);
        bool canMove=body.simulated && !player.IsDashing && !player.IsHurt && !player.IsCastingSpell;
        Loop(footsteps,canMove && player.IsGrounded && speed>0.5f);
        Loop(slide,canMove && player.IsWallSliding);
        if(!player.IsGrounded && body.linearVelocity.y<-0.5f)
        {airborne=true;peakFallSpeed=Mathf.Max(peakFallSpeed,-body.linearVelocity.y);}
        if(canMove && !player.IsGrounded && !player.IsWallSliding && body.linearVelocity.y<-13 && !fallPlayed)
        {fallPlayed=true;if(falling.clip!=null)falling.Play();}
        if(player.IsGrounded || player.IsWallSliding || player.IsDashing)falling.Stop();
    }
    private void StopMovement() { footsteps?.Stop();slide?.Stop();falling?.Stop(); }
    private void OnDisable()
    {
        StopMovement();actions?.Stop();impacts?.Stop();paused=false;
    }
}
