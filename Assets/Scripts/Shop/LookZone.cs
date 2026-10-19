using System.Collections.Generic;
using UnityEngine;

public class LookZone : MonoBehaviour
{
    [SerializeField, TextArea] string message = "Esta es una estantería";

    readonly HashSet<Collider> inside = new HashSet<Collider>();

    BoxCollider zone;
    Shelf shelf;

    public ProductData Product => shelf != null ? shelf.Product : null;

    public string Message => shelf != null && shelf.Product != null ? $"Esta es la estantería de {shelf.Product.DisplayName}" : message;

    public Vector3 Front => zone != null ? zone.bounds.center : transform.position;

    public bool PlayerInside
    {
        get
        {
            inside.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);
            return inside.Count > 0;
        }
    }

    void Awake()
    {
        shelf = GetComponent<Shelf>();
        foreach (var box in GetComponents<BoxCollider>())
            if (box.isTrigger) zone = box;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other)) inside.Add(other);
    }

    void OnTriggerExit(Collider other) => inside.Remove(other);

    void OnDisable() => inside.Clear();

    static bool IsPlayer(Collider collider) => collider.transform.root.GetComponentInChildren<PlayerMovement>() != null;
}
