using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private bool isHurt = false;
    private bool isDead = false;
    public float moveSpeed = 1.0f;
    public Transform character;
    public Transform[] waypoints;
    private bool isMovingRoute = true;
    int m_CurrentWaypointIndex;
    Rigidbody2D rigidbody2D;
    private void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
    }
    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        if (isHurt)
        {
            StartCoroutine(Hurt());
            return;
        }
        if (isMovingRoute)
        {
            MoveRoute();
        }
    }

    public void StopMoving()
    {
        isDead = true;
        StopAllCoroutines();
        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.gravityScale = 0f;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
        {
            collider.enabled = false;
        }
    }
    
    public IEnumerator Hurt()
    {
        isHurt = true;
        rigidbody2D.linearVelocityX = 5f * (IsCharacterOnLeft(character) ? 1f : -1f);
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(DwellTime());
        isHurt = false;
    }
    IEnumerator DwellTime()
    {
        rigidbody2D.linearVelocityX = 0f;
        isMovingRoute = false;
        yield return new WaitForSeconds(0.5f);
        isMovingRoute = true;
    }
    public bool IsCharacterOnLeft(Transform character)
    {
        return character != null && character.position.x < transform.position.x;
    }

    private void MoveRoute()
    {
        if (waypoints == null || waypoints.Length == 0 || rigidbody2D == null) return;
        Transform currentWaypoint = waypoints[m_CurrentWaypointIndex];
        Vector2 currentToTarget = (Vector2)currentWaypoint.position - rigidbody2D.position;

        if (currentToTarget.magnitude < 0.1f)
        {
            m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length;
        }

        if (currentToTarget.x > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        rigidbody2D.MovePosition(rigidbody2D.position + currentToTarget.normalized * moveSpeed * Time.deltaTime);
    }
}
