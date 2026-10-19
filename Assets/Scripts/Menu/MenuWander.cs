using UnityEngine;

public class MenuWander : MonoBehaviour
{
    [SerializeField] Vector2 areaMin = new Vector2(-4f, 0f);
    [SerializeField] Vector2 areaMax = new Vector2(4f, 13f);

    [SerializeField, Min(0f)] float speed = 1.6f;
    [SerializeField, Min(0f)] float turnSpeed = 90f;

    [SerializeField] Vector2 pause = new Vector2(0.5f, 2.5f);

    [SerializeField] LayerMask obstacleLayers = ~0;
    [SerializeField, Min(0f)] float margin = 0.3f;

    [Header("Bamboleo")]
    [SerializeField] float swayAngle = 3f;
    [SerializeField] float stepsPerSecond = 1.5f;

    Vector3 target;
    float waitUntil;
    float stepPhase;
    float yaw;
    Vector3 boxCenter;
    Vector3 boxHalf;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        MeasureFootprint();
        PickTarget();
    }

    void Update()
    {
        Vector3 pos = transform.position;
        Vector3 toTarget = target - pos;
        toTarget.y = 0f;
        bool walking = Time.time >= waitUntil && toTarget.magnitude > 0.3f;

        if (walking)
        {
            float wanted = Quaternion.LookRotation(toTarget).eulerAngles.y;
            float nextYaw = Mathf.MoveTowardsAngle(yaw, wanted, turnSpeed * Time.deltaTime);
            float facing = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(nextYaw, wanted)) / 90f);
            Vector3 nextPos = pos + Quaternion.Euler(0f, nextYaw, 0f) * Vector3.forward * (speed * facing * Time.deltaTime);

            if (Blocked(nextPos, nextYaw))
            {
                waitUntil = Time.time + Random.Range(pause.x, pause.y);
                PickTarget();
            }
            else
            {
                pos = nextPos;
                yaw = nextYaw;
                stepPhase += Time.deltaTime * stepsPerSecond * Mathf.PI * 2f;
            }
        }
        else if (Time.time >= waitUntil)
        {
            waitUntil = Time.time + Random.Range(pause.x, pause.y);
            PickTarget();
        }
        else
        {
            stepPhase = Mathf.Lerp(stepPhase, Mathf.Round(stepPhase / Mathf.PI) * Mathf.PI, Time.deltaTime * 4f);
        }

        transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, Mathf.Sin(stepPhase) * swayAngle));
    }

    void PickTarget()
    {
        Vector3 pos = transform.position;
        for (int i = 0; i < 30; i++)
        {
            var candidate = new Vector3(Random.Range(areaMin.x, areaMax.x), pos.y, Random.Range(areaMin.y, areaMax.y));
            Vector3 dir = candidate - pos;
            if (dir.sqrMagnitude < 1f) continue;
            float dirYaw = Quaternion.LookRotation(dir).eulerAngles.y;
            if (Blocked(candidate, dirYaw) || Blocked(pos, dirYaw)) continue;
            target = candidate;
            return;
        }
        target = pos;
    }

    void MeasureFootprint()
    {
        Quaternion saved = transform.rotation;
        transform.rotation = Quaternion.identity;
        var local = new Bounds();
        bool first = true;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i < 4 ? b.min.z : b.max.z);
                Vector3 p = corner - transform.position;
                if (first) local = new Bounds(p, Vector3.zero);
                else local.Encapsulate(p);
                first = false;
            }
        }
        transform.rotation = saved;

        float bottom = -transform.position.y + 0.3f;
        if (local.min.y < bottom) local.SetMinMax(new Vector3(local.min.x, bottom, local.min.z), local.max);
        boxCenter = local.center;
        boxHalf = local.extents + new Vector3(margin, 0f, margin);
    }

    bool Blocked(Vector3 pos, float atYaw)
    {
        Quaternion rot = Quaternion.Euler(0f, atYaw, 0f);
        return Physics.CheckBox(pos + rot * boxCenter, boxHalf, rot, obstacleLayers, QueryTriggerInteraction.Ignore);
    }
}
