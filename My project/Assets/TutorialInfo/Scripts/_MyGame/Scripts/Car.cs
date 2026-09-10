using System.Collections.Generic;
using UnityEngine;

/// <summary>Движущийся трафик. Машина либо съедается головой, либо откусывает хвост.</summary>
public class Car : MonoBehaviour
{
    [HideInInspector] public Transform waypointSystem;
    [HideInInspector] public float moveSpeed = 8f;

    private readonly List<Transform> waypoints = new List<Transform>();
    private int currentIndex;
    private int requestedStartIndex;
    private SnakeBody cachedSnakeBody;
    private CarSpawner cachedSpawner;
    private bool consumed;

    private void Start()
    {
        cachedSnakeBody = FindFirstObjectByType<SnakeBody>();
        cachedSpawner = FindFirstObjectByType<CarSpawner>();
        LoadWaypoints();
        if (waypoints.Count > 0) currentIndex = Mathf.Clamp(requestedStartIndex, 0, waypoints.Count - 1);
    }

    private void Update()
    {
        if (consumed) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.RunState.Playing) return;
        if (waypoints.Count == 0)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            return;
        }

        Transform target = waypoints[currentIndex];
        if (target == null)
        {
            LoadWaypoints();
            return;
        }

        Vector3 direction = target.position - transform.position;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, 8f * Time.deltaTime);
        }
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        if ((transform.position - target.position).sqrMagnitude < 0.4f * 0.4f)
        {
            currentIndex = (currentIndex + 1) % waypoints.Count;
        }
    }

    public void SetStartWaypointIndex(int index)
    {
        requestedStartIndex = Mathf.Max(0, index);
        currentIndex = requestedStartIndex;
    }

    private void LoadWaypoints()
    {
        waypoints.Clear();
        if (waypointSystem == null) return;
        foreach (Transform child in waypointSystem)
        {
            if (child != null && child.name.StartsWith("Waypoint")) waypoints.Add(child);
        }
    }

    public bool TryConsume()
    {
        if (consumed) return false;
        consumed = true;
        if (cachedSpawner != null) cachedSpawner.OnCarEaten(this);
        Destroy(gameObject);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        SnakeSegment segment = other.GetComponentInParent<SnakeSegment>();
        if (segment != null && segment.segmentIndex >= 1)
        {
            SnakeBody hitBody = segment.owner != null ? segment.owner : cachedSnakeBody;
            if (hitBody != null) hitBody.CutTail(segment.segmentIndex);
            if (cachedSpawner != null) cachedSpawner.OnCarHitBody(this);
            consumed = true;
            Destroy(gameObject);
        }
    }
}
