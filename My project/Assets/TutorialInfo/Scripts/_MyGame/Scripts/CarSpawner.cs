using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Пул трафика: поддерживает обычные префабы, но умеет создать аккуратные
/// процедурные машины, поэтому демо-сцена не ломается без ассетов.
/// </summary>
public class CarSpawner : MonoBehaviour
{
    [Header("Префабы и маршрут")]
    public GameObject[] carPrefabs;
    public Transform waypointSystem;

    [Header("Спавн")]
    [Range(3, 18)] public int maxCarsOnMap = 8;
    [Min(0.2f)] public float spawnDelay = 2.2f;
    [Min(4f)] public float minDistanceFromHead = 12f;
    public bool showDebugGizmos;

    [Header("Движение")]
    public float baseCarSpeed = 7f;
    public float speedRandomRange = 1.5f;

    private readonly List<Car> activeCars = new List<Car>();
    private readonly List<Transform> allWaypoints = new List<Transform>();
    private readonly List<Transform> safeWaypoints = new List<Transform>();
    private Transform snakeHead;
    private float lastSpawnTime;
    private int totalCarsSpawned;

    private void Start()
    {
        GameSettings.Load();
        maxCarsOnMap = Mathf.Clamp(GameSettings.MaxCarsOnMapValue, GameSettings.MinCarsOnMap, GameSettings.MaxCarsOnMap);
        CollectAllWaypoints();
        SnakeMovement movement = FindFirstObjectByType<SnakeMovement>();
        snakeHead = movement != null ? movement.transform : null;

        for (int i = 0; i < maxCarsOnMap; i++) SpawnCar();
        lastSpawnTime = Time.time;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.RunState.Playing) return;
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            if (activeCars[i] == null) activeCars.RemoveAt(i);
        }

        maxCarsOnMap = Mathf.Clamp(GameSettings.MaxCarsOnMapValue, GameSettings.MinCarsOnMap, GameSettings.MaxCarsOnMap);
        int targetCars = GetTargetCarCount();
        if (activeCars.Count < targetCars && Time.time - lastSpawnTime >= spawnDelay / GameSettings.DifficultyTrafficMultiplier)
        {
            SpawnCar();
            lastSpawnTime = Time.time;
        }
    }

    private void CollectAllWaypoints()
    {
        allWaypoints.Clear();
        if (waypointSystem == null) return;
        foreach (Transform child in waypointSystem)
        {
            if (child != null && child.name.StartsWith("Waypoint")) allWaypoints.Add(child);
        }
    }

    public void RefreshWaypoints() => CollectAllWaypoints();

    private int GetTargetCarCount()
    {
        int levelBonus = GameManager.Instance != null ? Mathf.Min(4, Mathf.Max(0, GameManager.Instance.Level - 1) / 2) : 0;
        return Mathf.Min(GameSettings.MaxCarsOnMap, maxCarsOnMap + levelBonus);
    }

    public void SpawnCar()
    {
        if (allWaypoints.Count == 0 || activeCars.Count >= GetTargetCarCount()) return;
        Transform spawnPoint = GetSafeSpawnWaypoint();
        if (spawnPoint == null) return;

        GameObject carObject = ChoosePrefab();
        if (carObject == null) carObject = CreateDefaultCar();
        else carObject = Instantiate(carObject, spawnPoint.position, spawnPoint.rotation);

        carObject.transform.position = spawnPoint.position;
        carObject.transform.rotation = spawnPoint.rotation;
        Car car = carObject.GetComponent<Car>();
        if (car == null) car = carObject.AddComponent<Car>();
        car.waypointSystem = waypointSystem;
        float levelMultiplier = GameManager.Instance == null ? 1f : 1f + Mathf.Min(0.6f, Mathf.Max(0, GameManager.Instance.Level - 1) * 0.05f);
        car.moveSpeed = Mathf.Max(2f, (baseCarSpeed + Random.Range(-speedRandomRange, speedRandomRange)) * GameSettings.DifficultyTrafficMultiplier * levelMultiplier);
        car.SetStartWaypointIndex(allWaypoints.IndexOf(spawnPoint));
        TrySetTag(carObject, "Car");

        Collider collider = carObject.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider box = carObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
        else collider.isTrigger = true;

        Rigidbody body = carObject.GetComponent<Rigidbody>();
        if (body == null) body = carObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        activeCars.Add(car);
        totalCarsSpawned++;
    }

    private GameObject ChoosePrefab()
    {
        if (carPrefabs == null || carPrefabs.Length == 0) return null;
        List<GameObject> valid = new List<GameObject>();
        for (int i = 0; i < carPrefabs.Length; i++) if (carPrefabs[i] != null) valid.Add(carPrefabs[i]);
        if (valid.Count == 0) return null;
        return Instantiate(valid[Random.Range(0, valid.Count)]);
    }

    private Transform GetSafeSpawnWaypoint()
    {
        if (allWaypoints.Count == 0) return null;
        safeWaypoints.Clear();
        for (int i = 0; i < allWaypoints.Count; i++)
        {
            Transform point = allWaypoints[i];
            if (point == null) continue;
            if (snakeHead != null && Vector3.Distance(point.position, snakeHead.position) < minDistanceFromHead) continue;

            bool occupied = false;
            for (int c = 0; c < activeCars.Count; c++)
            {
                if (activeCars[c] != null && Vector3.Distance(activeCars[c].transform.position, point.position) < 4f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) safeWaypoints.Add(point);
        }

        if (safeWaypoints.Count > 0) return safeWaypoints[Random.Range(0, safeWaypoints.Count)];
        return allWaypoints[Random.Range(0, allWaypoints.Count)];
    }

    private GameObject CreateDefaultCar()
    {
        GameObject root = new GameObject("TrafficCar");
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        body.transform.localScale = new Vector3(1.6f, 0.65f, 2.8f);
        RemoveCollider(body);

        Renderer renderer = body.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material paint = new Material(SurfaceShader());
            paint.color = Color.HSVToRGB(Random.value, 0.65f, 0.9f);
            renderer.material = paint;
        }

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(root.transform);
        cabin.transform.localPosition = new Vector3(0f, 0.98f, -0.1f);
        cabin.transform.localScale = new Vector3(1.2f, 0.45f, 1.2f);
        RemoveCollider(cabin);
        Renderer cabinRenderer = cabin.GetComponent<Renderer>();
        if (cabinRenderer != null)
        {
            Material glass = new Material(SurfaceShader());
            glass.color = new Color(0.08f, 0.15f, 0.2f);
            cabinRenderer.material = glass;
        }
        return root;
    }

    private Shader SurfaceShader() => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    private void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private void TrySetTag(GameObject target, string tag)
    {
        try { target.tag = tag; } catch (UnityException) { }
    }

    public void OnCarEaten(Car car)
    {
        activeCars.Remove(car);
    }

    public void OnCarHitBody(Car car)
    {
        activeCars.Remove(car);
    }

    public void SpawnMultipleCars(int count)
    {
        for (int i = 0; i < Mathf.Max(0, count); i++) Invoke(nameof(SpawnCar), i * 0.35f);
    }

    public int GetActiveCarCount() => activeCars.Count;
    public int GetTotalCarsSpawned() => totalCarsSpawned;

    public void ClearAllCars()
    {
        for (int i = 0; i < activeCars.Count; i++) if (activeCars[i] != null) Destroy(activeCars[i].gameObject);
        activeCars.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || waypointSystem == null) return;
        Gizmos.color = Color.cyan;
        foreach (Transform child in waypointSystem)
        {
            if (child != null && child.name.StartsWith("Waypoint")) Gizmos.DrawWireSphere(child.position, 0.45f);
        }
        if (snakeHead != null) Gizmos.DrawWireSphere(snakeHead.position, minDistanceFromHead);
    }
}
