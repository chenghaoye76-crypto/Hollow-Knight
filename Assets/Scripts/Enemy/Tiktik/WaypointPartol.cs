using UnityEngine;

public class WaypointPatrol : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public Transform[] waypoints;

    private Rigidbody2D m_RigidBody;
    int m_CurrentWaypointIndex;

    void Start()
    {
        m_RigidBody = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        Transform currentWaypoint = waypoints[m_CurrentWaypointIndex];
        Vector2 currentToTarget = (Vector2)currentWaypoint.position - m_RigidBody.position;

        if (currentToTarget.magnitude < 0.1f)
        {
            m_CurrentWaypointIndex = (m_CurrentWaypointIndex + 1) % waypoints.Length;
        }
        //float angle = Mathf.Atan2(currentToTarget.y, currentToTarget.x)
        //              * Mathf.Rad2Deg;

        //m_RigidBody.MoveRotation(angle);

        if (currentToTarget.x > 0) 
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        m_RigidBody.MovePosition(m_RigidBody.position + currentToTarget.normalized * moveSpeed * Time.deltaTime);
    }
}
