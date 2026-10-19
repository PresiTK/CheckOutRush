using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] Transform shelvesRoot;

    [SerializeField] List<ProductData> products = new List<ProductData>();

    [SerializeField, Min(1)] int recipeSize = 3;

    [SerializeField] List<ProductData> recipe = new List<ProductData>();

    [SerializeField] GameTimer timer = new GameTimer();

    [Header("Carrito")]
    [SerializeField] List<Transform> cartPoints = new List<Transform>();

    [SerializeField, Min(0.01f)] float cartProductScale = 0.55f;

    [SerializeField] AudioClip collectClip;
    [SerializeField, Range(0f, 1f)] float collectVolume = 1f;

    public IReadOnlyList<ProductData> Recipe => recipe;
    public GameTimer Timer => timer;

    public event Action RecipeChosen;
    public bool IsRecipeChosen { get; private set; }

    public event Action<ProductData> ProductCollected;

    public event Action<bool> GameEnded;
    public bool IsGameOver { get; private set; }

    readonly HashSet<ProductData> collected = new HashSet<ProductData>();

    Shelf[] shelves;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
        timer.Finished += OnTimerFinished;

        shelves = shelvesRoot != null
            ? shelvesRoot.GetComponentsInChildren<Shelf>()
            : FindObjectsByType<Shelf>(FindObjectsInactive.Exclude);
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        timer.Finished -= OnTimerFinished;
        Time.timeScale = 1f;
        Instance = null;
    }

    void Start()
    {
        if (products.Count == 0)
        {
            Debug.LogWarning("No hay productos que repartir en los estantes.", this);
            return;
        }

        FillShelves();
        ChooseRecipe();
        timer.Begin();
    }

    void Update()
    {
        timer.Tick(Time.deltaTime);
    }

    void FillShelves()
    {
        var pool = new List<ProductData>();
        foreach (var product in products)
            if (product != null && !pool.Contains(product)) pool.Add(product);
        Shuffle(pool);

        if (pool.Count < shelves.Length)
            Debug.LogWarning($"Hay {shelves.Length} estanterías y solo {pool.Count} productos distintos: {shelves.Length - pool.Count} se quedan vacías.", this);

        var order = new List<Shelf>(shelves);
        Shuffle(order);
        for (int i = 0; i < order.Count; i++)
            order[i].SetProduct(i < pool.Count ? pool[i] : null);
    }

    void ChooseRecipe()
    {
        foreach (var product in products) product.IsInRecipe = false;
        recipe.Clear();
        collected.Clear();

        var onShelves = new List<ProductData>();
        foreach (var shelf in shelves)
            if (shelf.Product != null && !onShelves.Contains(shelf.Product)) onShelves.Add(shelf.Product);

        Shuffle(onShelves);
        int count = Mathf.Min(recipeSize, onShelves.Count);
        for (int i = 0; i < count; i++)
        {
            onShelves[i].IsInRecipe = true;
            recipe.Add(onShelves[i]);
        }

        IsRecipeChosen = true;
        RecipeChosen?.Invoke();
    }

    public bool Collect(ProductData product)
    {
        if (IsGameOver || product == null || !product.IsInRecipe || !collected.Add(product)) return false;

        PutInCart(product);
        if (collectClip != null) SoundManager.Instance.PlaySfx(collectClip, collectVolume);
        ProductCollected?.Invoke(product);
        if (collected.Count >= recipe.Count) EndGame(true);
        return true;
    }

    void PutInCart(ProductData product)
    {
        if (product.Prefab == null || cartPoints.Count == 0) return;

        Transform point = cartPoints[(collected.Count - 1) % cartPoints.Count];
        var item = Instantiate(product.Prefab, point.position, point.rotation, point);
        item.transform.localScale *= cartProductScale;
    }

    void OnTimerFinished() => EndGame(false);

    void EndGame(bool won)
    {
        if (IsGameOver) return;

        IsGameOver = true;
        timer.Stop();
        Time.timeScale = 0f;

        var input = InputManager.Instance;
        if (input != null)
        {
            input.InputMap.Player.Disable();
            input.InputMap.UI.Enable();
        }

        GameEnded?.Invoke(won);
    }

    public bool IsCollected(ProductData product) => collected.Contains(product);

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
