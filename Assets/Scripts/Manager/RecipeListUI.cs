using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class RecipeListUI : MonoBehaviour
{
    [SerializeField] TMP_Text itemPrefab;

    [SerializeField, Min(1f)] float fontSize = 32f;

    [SerializeField] Color textColor = Color.white;

    [SerializeField] Color collectedColor = new Color(1f, 1f, 1f, 0.5f);

    [SerializeField] int itemCount;

    readonly Dictionary<ProductData, TMP_Text> items = new Dictionary<ProductData, TMP_Text>();
    GameManager subscribed;

    void Start()
    {
        subscribed = GameManager.Instance;
        if (subscribed == null)
        {
            Debug.LogWarning("No hay GameManager: la lista de la compra se queda vacía.", this);
            return;
        }

        subscribed.RecipeChosen += Build;
        subscribed.ProductCollected += MarkCollected;
        if (subscribed.IsRecipeChosen) Build();
    }

    void OnDestroy()
    {
        if (subscribed == null) return;
        subscribed.RecipeChosen -= Build;
        subscribed.ProductCollected -= MarkCollected;
        subscribed = null;
    }

    void Build()
    {
        Clear();

        foreach (var product in subscribed.Recipe)
        {
            TMP_Text item = CreateItem();
            item.name = product.DisplayName;
            item.text = product.DisplayName;
            items[product] = item;
            if (subscribed.IsCollected(product)) MarkCollected(product);
        }
        itemCount = items.Count;
    }

    void MarkCollected(ProductData product)
    {
        if (!items.TryGetValue(product, out TMP_Text item)) return;
        item.fontStyle |= FontStyles.Strikethrough;
        item.color = collectedColor;
    }

    TMP_Text CreateItem()
    {
        if (itemPrefab != null) return Instantiate(itemPrefab, transform, false);

        var go = new GameObject("Item", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = textColor;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    void Clear()
    {
        foreach (var item in items.Values)
            if (item != null) Destroy(item.gameObject);
        items.Clear();
        itemCount = 0;
    }
}
