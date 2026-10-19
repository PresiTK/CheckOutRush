using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCWander : MonoBehaviour
{
    [SerializeField, Min(0f)] float speed = 3.5f;

    [SerializeField, Min(0f)] float maxPointDistance = 10f;

    [SerializeField, Min(0f)] float detectRadius = 2f;

    [SerializeField, Min(0f)] float detectDistance = 6f;

    [SerializeField] LayerMask obstacleLayers = ~0;

    [SerializeField, Min(0f)] float blockedWait = 2f;

    readonly RaycastHit[] hits = new RaycastHit[16];

    NavMeshAgent agent;
    NavMeshPath path;
    bool waiting;
    float resumeTime;

    Transform Character => transform.parent != null ? transform.parent : transform;

    Vector3 CastOrigin => transform.position;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        path = new NavMeshPath();
    }

    void OnEnable() => waiting = false;

    void OnValidate()
    {
        if (agent != null) agent.speed = speed;
    }

    void Update()
    {
        if (!agent.isOnNavMesh || agent.pathPending) return;

        if (waiting)
        {
            if (Time.time < resumeTime) return;
            waiting = false;
            GoToRandomPoint();
            return;
        }

        if (agent.hasPath && SeesObstacle())
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            waiting = true;
            resumeTime = Time.time + blockedWait;
            return;
        }

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            GoToRandomPoint();
    }

    Vector3 CastDirection => Vector3.ProjectOnPlane(agent.velocity, Vector3.up).normalized;

    bool SeesObstacle()
    {
        Vector3 direction = CastDirection;
        if (direction == Vector3.zero) return false;

        Vector3 origin = CastOrigin;
        float feet = transform.position.y - agent.baseOffset;
        int count = Physics.SphereCastNonAlloc(origin, detectRadius, direction, hits, detectDistance, obstacleLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            var other = hit.collider;
            if (other.transform.IsChildOf(Character)) continue;
            if (other.bounds.max.y <= feet + 0.1f) continue;

            Vector3 point = hit.distance > 0f ? hit.point : other.ClosestPoint(origin);

            if (Vector3.Dot(Vector3.ProjectOnPlane(point - origin, Vector3.up), direction) > 0f) return true;
        }
        return false;
    }

    void GoToRandomPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 offset = Random.insideUnitCircle * maxPointDistance;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, agent.areaMask)
                && agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                return;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        Gizmos.color = waiting ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(CastOrigin, detectRadius);
        Vector3 direction = Application.isPlaying ? CastDirection : transform.forward;
        Gizmos.DrawWireSphere(CastOrigin + direction * detectDistance, detectRadius);
    }
}
