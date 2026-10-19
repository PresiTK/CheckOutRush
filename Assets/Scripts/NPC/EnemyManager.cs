using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] GameObject momPrefab;
    [SerializeField] GameObject kidPrefab;

    [Header("Cantidad (mínimo y máximo, ambos incluidos)")]
    [SerializeField] Vector2Int momCount = new Vector2Int(7, 10);
    [SerializeField] Vector2Int kidCount = new Vector2Int(5, 7);

    [Header("Zona")]
    [SerializeField] Vector2 areaMin = new Vector2(-66f, -77f);
    [SerializeField] Vector2 areaMax = new Vector2(86f, 95f);
    [SerializeField] float floorHeight = 1f;

    [SerializeField, Min(0f)] float minPlayerDistance = 15f;
    [SerializeField, Min(0f)] float minSpacing = 6f;
    [SerializeField, Min(0f)] float clearRadius = 2f;
    [SerializeField] LayerMask blockingLayers = ~0;

    [SerializeField, Min(1)] int maxTries = 50;

    [SerializeField] Transform player;

    readonly List<GameObject> spawned = new List<GameObject>();
    public IReadOnlyList<GameObject> Spawned => spawned;

    void Reset() => blockingLayers = ~LayerMask.GetMask("Floor");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (player == null)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start() => SpawnAll();

    public void SpawnAll()
    {
        var order = new List<GameObject>();
        AddCopies(order, momPrefab, momCount);
        AddCopies(order, kidPrefab, kidCount);
        Shuffle(order);

        var used = new List<Vector3>();
        foreach (var prefab in order)
        {
            if (!TryFindSpot(used, out Vector3 spot))
            {
                Debug.LogWarning($"No se ha encontrado sitio para {prefab.name}", this);
                continue;
            }

            used.Add(spot);
            spawned.Add(Spawn(prefab, spot));
        }
    }

    static void AddCopies(List<GameObject> list, GameObject prefab, Vector2Int range)
    {
        if (prefab == null) return;
        int count = Random.Range(range.x, range.y + 1);
        for (int i = 0; i < count; i++) list.Add(prefab);
    }

    bool TryFindSpot(List<Vector3> used, out Vector3 spot)
    {
        for (int i = 0; i < maxTries; i++)
        {
            var guess = new Vector3(Random.Range(areaMin.x, areaMax.x), floorHeight, Random.Range(areaMin.y, areaMax.y));
            if (!NavMesh.SamplePosition(guess, out NavMeshHit hit, 3f, NavMesh.AllAreas)) continue;

            spot = hit.position;
            if (player != null && FlatDistance(spot, player.position) < minPlayerDistance) continue;
            if (TooClose(spot, used)) continue;
            if (Physics.CheckCapsule(spot + Vector3.up * (clearRadius + 0.1f), spot + Vector3.up * 3f, clearRadius,
                    blockingLayers, QueryTriggerInteraction.Ignore)) continue;
            return true;
        }

        spot = default;
        return false;
    }

    bool TooClose(Vector3 spot, List<Vector3> used)
    {
        foreach (var other in used)
            if (FlatDistance(spot, other) < minSpacing) return true;
        return false;
    }

    static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    GameObject Spawn(GameObject prefab, Vector3 spot)
    {
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        Vector3 offset = Vector3.zero;
        var agent = prefab.GetComponentInChildren<NavMeshAgent>();
        if (agent != null)
            offset = Quaternion.Inverse(prefab.transform.rotation) * (agent.transform.position - prefab.transform.position)
                     - Vector3.up * agent.baseOffset;
        rotation *= prefab.transform.rotation;

        return Instantiate(prefab, spot - rotation * offset, rotation, transform);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
