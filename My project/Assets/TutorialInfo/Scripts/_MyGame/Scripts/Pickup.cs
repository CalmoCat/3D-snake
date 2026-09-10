using UnityEngine;

public enum PickupType
{
    Growth,
    Shield,
    Magnet,
    Fury,
    SlowMotion
}

/// <summary>Единая логика бонусов. Бонус может быть собран только один раз.</summary>
public class Pickup : MonoBehaviour
{
    public PickupType type = PickupType.Growth;
    public int value = 1;
    public float rotateSpeed = 80f;
    public float bobHeight = 0.18f;
    public float bobSpeed = 2.4f;

    private Vector3 startPosition;
    private bool collected;

    private void Awake()
    {
        startPosition = transform.position;
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        Vector3 position = startPosition;
        position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<SnakeMovement>() != null) Collect();
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        GameManager.Instance?.CollectPickup(type, value);
        Destroy(gameObject);
    }
}
