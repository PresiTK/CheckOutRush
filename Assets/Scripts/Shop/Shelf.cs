using System.Collections.Generic;
using UnityEngine;

public class Shelf : MonoBehaviour
{
    [SerializeField] Transform spawnPoint;

    [SerializeField, Min(1)] int copies = 4;

    [SerializeField, Min(0f)] float spacing = 2f;

    readonly List<GameObject> spawned = new List<GameObject>();

    public ProductData Product { get; private set; }

    public void SetProduct(ProductData product)
    {
        Product = product;
        foreach (var copy in spawned)
            if (copy != null) Destroy(copy);
        spawned.Clear();

        if (product == null || product.Prefab == null) return;

        Transform point = spawnPoint != null ? spawnPoint : transform;
        float start = -spacing * (copies - 1) * 0.5f;
        for (int i = 0; i < copies; i++)
        {
            Vector3 position = point.position + point.right * (start + spacing * i);
            spawned.Add(Instantiate(product.Prefab, position, point.rotation, point));
        }
    }
}
