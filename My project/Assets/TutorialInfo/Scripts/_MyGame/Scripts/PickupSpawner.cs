using System.Collections.Generic;
using UnityEngine;

/// <summary>Спавнит еду и редкие усилители, а магнит притягивает их к голове.</summary>
public class PickupSpawner : MonoBehaviour
{
    public Vector2 arenaSize = new Vector2(46f, 46f);
    [Range(3, 20)] public int maxPickups = 10;
    public float respawnDelay = 2.5f;
    public float magnetRadius = 14f;

    private readonly List<Pickup> activePickups = new List<Pickup>();
    private float lastSpawnTime;

    private void Start()
    {
        for (int i = 0; i < maxPickups; i++) SpawnPickup();
        lastSpawnTime = Time.time;
    }

    private void Update()
    {
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            if (activePickups[i] == null) activePickups.RemoveAt(i);
        }

        if (activePickups.Count < maxPickups && Time.time - lastSpawnTime >= respawnDelay)
        {
            SpawnPickup();
            lastSpawnTime = Time.time;
        }

        if (GameManager.Instance != null && GameManager.Instance.MagnetTime > 0f && SnakeMovement.Players.Count > 0)
        {
            for (int i = 0; i < activePickups.Count; i++)
            {
                Pickup pickup = activePickups[i];
                if (pickup == null) continue;
                SnakeMovement nearest = FindNearestPlayer(pickup.transform.position);
                if (nearest == null) continue;
                Vector3 flatOffset = nearest.transform.position - pickup.transform.position;
                flatOffset.y = 0f;
                if (flatOffset.magnitude <= magnetRadius)
                {
                    pickup.transform.position = Vector3.MoveTowards(pickup.transform.position, nearest.transform.position, 12f * Time.deltaTime);
                }
            }
        }
    }

    public void SpawnPickup()
    {
        Vector3 position = FindSafePosition();
        GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pickupObject.name = "Pickup_" + activePickups.Count.ToString("00");
        pickupObject.transform.SetParent(transform);
        pickupObject.transform.position = position;
        pickupObject.transform.localScale = new Vector3(0.65f, 0.16f, 0.65f);

        Collider collider = pickupObject.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;
        Rigidbody rigidbody = pickupObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        Pickup pickup = pickupObject.AddComponent<Pickup>();
        pickup.type = RollType();
        pickup.value = pickup.type == PickupType.Growth ? Random.Range(1, 3) : 1;
        ApplyVisual(pickupObject, pickup.type);
        activePickups.Add(pickup);
    }

    private SnakeMovement FindNearestPlayer(Vector3 position)
    {
        SnakeMovement nearest = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < SnakeMovement.Players.Count; i++)
        {
            SnakeMovement player = SnakeMovement.Players[i];
            if (player == null) continue;
            float distance = (player.transform.position - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = player;
            }
        }
        return nearest;
    }

    private Vector3 FindSafePosition()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(-arenaSize.x * 0.5f, arenaSize.x * 0.5f),
                0.9f,
                Random.Range(-arenaSize.y * 0.5f, arenaSize.y * 0.5f));
            SnakeMovement nearest = FindNearestPlayer(candidate);
            if (nearest == null || Vector3.Distance(candidate, nearest.transform.position) > 5f) return candidate;
        }
        return new Vector3(0f, 0.9f, 12f);
    }

    private PickupType RollType()
    {
        float roll = Random.value;
        if (roll < 0.58f) return PickupType.Growth;
        if (roll < 0.73f) return PickupType.Shield;
        if (roll < 0.87f) return PickupType.Magnet;
        if (roll < 0.97f) return PickupType.Fury;
        return PickupType.SlowMotion;
    }

    private void ApplyVisual(GameObject pickupObject, PickupType type)
    {
        Renderer renderer = pickupObject.GetComponent<Renderer>();
        if (renderer == null) return;
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        switch (type)
        {
            case PickupType.Growth: material.color = new Color(0.25f, 1f, 0.45f); break;
            case PickupType.Shield: material.color = new Color(0.25f, 0.65f, 1f); break;
            case PickupType.Magnet: material.color = new Color(1f, 0.35f, 0.85f); break;
            case PickupType.Fury: material.color = new Color(1f, 0.3f, 0.12f); break;
            default: material.color = new Color(1f, 0.85f, 0.2f); break;
        }
        renderer.material = material;
    }
}
