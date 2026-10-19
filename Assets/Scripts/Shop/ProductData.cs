using UnityEngine;

[CreateAssetMenu(fileName = "NewProduct", menuName = "Scriptable Objects/Product")]
public class ProductData : ScriptableObject
{
    public string Name;

    public bool IsInRecipe;

    public GameObject Prefab;

    public string DisplayName => string.IsNullOrEmpty(Name) ? name : Name;
}
