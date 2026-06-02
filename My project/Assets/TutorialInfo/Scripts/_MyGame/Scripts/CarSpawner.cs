using UnityEngine;
using System.Collections.Generic;

public class CarSpawner : MonoBehaviour
{
    [Header("Префабы машин")]
    [Tooltip("Массив префабов машин, которые будут спавниться")]
    public GameObject[] carPrefabs;

    [Header("Система маршрутов")]
    [Tooltip("Объект WaypointSystem, содержащий все точки маршрута")]
    public Transform waypointSystem;

    [Header("Настройки спавна")]
    [Tooltip("Максимальное количество машин на карте одновременно")]
    public int maxCarsOnMap = 10;

    [Tooltip("Задержка между спавном новых машин (в секундах)")]
    public float spawnDelay = 2f;

    [Tooltip("Минимальная дистанция от головы змеи для спавна новой машины")]
    public float minDistanceFromHead = 10f;

    [Tooltip("Включить визуализацию спавна (для отладки)")]
    public bool showDebugGizmos = true;

    [Header("Настройки движения машин")]
    [Tooltip("Базовая скорость машин")]
    public float baseCarSpeed = 8f;

    [Tooltip("Разброс скорости машин (± от базовой)")]
    public float speedRandomRange = 3f;

    private List<Car> activeCars = new List<Car>();
    private List<Transform> allWaypoints = new List<Transform>();
    private Transform snakeHead;
    private float lastSpawnTime;
    private int totalCarsSpawned = 0;
    private SnakeMovement cachedSnakeMovement;
    private static List<Transform> _tempSafeWaypoints = new List<Transform>();

    void Start()
    {
        maxCarsOnMap = GameSettings.MaxCarsOnMapValue;
        CollectAllWaypoints();

        cachedSnakeMovement = FindFirstObjectByType<SnakeMovement>();
        if (cachedSnakeMovement != null)
        {
            snakeHead = cachedSnakeMovement.transform;
        }

        if (carPrefabs.Length == 0)
        {
            return;
        }

        if (allWaypoints.Count == 0)
        {
            return;
        }

        for (int i = 0; i < maxCarsOnMap; i++)
        {
            SpawnCar();
        }

        lastSpawnTime = Time.time;
    }

    void Update()
    {
        maxCarsOnMap = GameSettings.MaxCarsOnMapValue;

        if (activeCars.Count < maxCarsOnMap && Time.time - lastSpawnTime >= spawnDelay)
        {
            SpawnCar();
            lastSpawnTime = Time.time;
        }
    }

    void CollectAllWaypoints()
    {
        allWaypoints.Clear();

        if (waypointSystem != null)
        {
            foreach (Transform child in waypointSystem)
            {
                if (child.name.StartsWith("Waypoint") || child.name.Contains("Waypoint"))
                {
                    allWaypoints.Add(child);
                }
            }
        }

        Debug.Log($"CarSpawner: Найдено Waypoint'ов: {allWaypoints.Count}");
    }

    void FindSnakeHead()
    {
        SnakeMovement snakeMovement = FindFirstObjectByType<SnakeMovement>();
        if (snakeMovement != null)
        {
            snakeHead = snakeMovement.transform;
        }
        else
        {
            GameObject headObj = GameObject.FindGameObjectWithTag("SnakeHead");
            if (headObj != null)
                snakeHead = headObj.transform;
        }
    }

    public void SpawnCar()
    {
        if (carPrefabs.Length == 0)
        {
            return;
        }

        if (allWaypoints.Count == 0)
        {
            Debug.LogWarning("CarSpawner: Нет Waypoint'ов для спавна!");
            return;
        }

        Transform spawnWaypoint = GetSafeSpawnWaypoint();

        if (spawnWaypoint == null)
        {
            Debug.LogWarning("CarSpawner: Не удалось найти безопасный Waypoint для спавна!");
            return;
        }

        GameObject carPrefab = carPrefabs[Random.Range(0, carPrefabs.Length)];

        GameObject newCar = Instantiate(carPrefab, spawnWaypoint.position, spawnWaypoint.rotation);

        Car carScript = newCar.GetComponent<Car>();
        if (carScript == null)
            carScript = newCar.AddComponent<Car>();

        carScript.waypointSystem = waypointSystem;
        carScript.moveSpeed = baseCarSpeed + Random.Range(-speedRandomRange, speedRandomRange);

        int startIndex = allWaypoints.IndexOf(spawnWaypoint);
        carScript.SetStartWaypointIndex(startIndex);

        newCar.tag = "Car";

        if (newCar.GetComponent<Collider>() == null)
        {
            BoxCollider col = newCar.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        activeCars.Add(carScript);
        totalCarsSpawned++;

        if (showDebugGizmos)
        {
            Debug.Log($"CarSpawner: Спавнена машина {totalCarsSpawned} в точке {spawnWaypoint.name}. Активных машин: {activeCars.Count}");
        }
    }

    Transform GetSafeSpawnWaypoint()
    {
        if (snakeHead == null)
        {
            return allWaypoints[Random.Range(0, allWaypoints.Count)];
        }

        _tempSafeWaypoints.Clear();

        foreach (Transform wp in allWaypoints)
        {
            float distanceToHead = Vector3.Distance(wp.position, snakeHead.position);
            if (distanceToHead >= minDistanceFromHead)
            {
                _tempSafeWaypoints.Add(wp);
            }
        }

        if (_tempSafeWaypoints.Count > 0)
        {
            return _tempSafeWaypoints[Random.Range(0, _tempSafeWaypoints.Count)];
        }

        Transform farthestWaypoint = null;
        float maxDistance = 0f;

        foreach (Transform wp in allWaypoints)
        {
            float distance = Vector3.Distance(wp.position, snakeHead.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthestWaypoint = wp;
            }
        }

        return farthestWaypoint;
    }

    public void OnCarEaten(Car eatenCar)
    {
        if (activeCars.Contains(eatenCar))
            activeCars.Remove(eatenCar);

        if (showDebugGizmos)
        {
            Debug.Log($"CarSpawner: Машина съедена. Активных машин: {activeCars.Count}");
        }
    }

    public void OnCarHitBody(Car hitCar)
    {
        if (activeCars.Contains(hitCar))
            activeCars.Remove(hitCar);

        if (showDebugGizmos)
        {
            Debug.Log($"CarSpawner: Машина врезалась в тело. Активных машин: {activeCars.Count}");
        }
    }

    public void SpawnMultipleCars(int count)
    {
        if (showDebugGizmos)
        {
            Debug.Log($"CarSpawner: Спавн {count} машин после отпадения сегментов");
        }

        for (int i = 0; i < count; i++)
        {
            Invoke(nameof(DelayedSpawn), i * 0.5f);
        }
    }

    void DelayedSpawn()
    {
        if (activeCars.Count < maxCarsOnMap)
        {
            SpawnCar();
        }
    }

    public void RefreshWaypoints()
    {
        CollectAllWaypoints();
    }

    public int GetActiveCarCount()
    {
        return activeCars.Count;
    }

    public int GetTotalCarsSpawned()
    {
        return totalCarsSpawned;
    }

    public void ClearAllCars()
    {
        foreach (Car car in activeCars)
        {
            if (car != null)
                Destroy(car.gameObject);
        }
        activeCars.Clear();
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        if (waypointSystem != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform child in waypointSystem)
            {
                if (child.name.StartsWith("Waypoint") || child.name.Contains("Waypoint"))
                {
                    Gizmos.DrawWireSphere(child.position, 0.5f);
                }
            }
        }

        if (snakeHead != null && Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(snakeHead.position, minDistanceFromHead);
        }
    }
}