using UnityEngine;

public class AttackEffect : MonoBehaviour
{
    private Transform owner;
    private Transform effectRoot;
    private Vector3 offset;
    private bool following;

    public void Follow(Transform target)
    {
        owner = target;
        effectRoot = transform.parent != null ? transform.parent : transform;
        following = owner != null;
        if (following) offset = effectRoot.position - owner.position;
    }

    // Move the root so both the animated slash and its trigger follow the Knight.
    // Keep world rotation/scale intact when the Knight turns during a swing.
    private void FixedUpdate() => FollowPosition();
    private void LateUpdate() => FollowPosition();
    private void FollowPosition()
    {
        if (!following) return;
        if (owner == null) { DestroyEffect(); return; }
        effectRoot.position = owner.position + offset;
    }

    public void DestroyEffect()
    {
        var root = effectRoot != null ? effectRoot : transform.parent != null ? transform.parent : transform;
        Destroy(root.gameObject);
    }
}
