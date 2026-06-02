using UnityEngine;
using System.Collections.Generic;

public class Car : MonoBehaviour
{
    [HideInInspector] public Transform waypointSystem;
    [HideInInspector] public float moveSpeed = 8f;

    private List<Transform> waypoints = new List<Transform>();
    private int currentIdx = 0;
    private SnakeBody cachedSnakeBody;
    private CarSpawner cachedSpawner;

    void Start()
    {
        cachedSnakeBody = FindFirstObjectByType<SnakeBody>();
        cachedSpawner = FindFirstObjectByType<CarSpawner>();
        if (waypointSystem != null) LoadWaypoints();
    }

    void Update()
    {
        if (waypoints.Count == 0) return;

        Transform target = waypoints[currentIdx];

        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
        }

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            currentIdx = (currentIdx + 1) % waypoints.Count;
        }
    }

    public void SetStartWaypointIndex(int index)
    {
        currentIdx = index;
    }

    void LoadWaypoints()
    {
        waypoints.Clear();
        foreach (Transform child in waypointSystem)
        {
            if (child.name.Contains("Waypoint"))
            {
                waypoints.Add(child);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        SnakeSegment segment = other.GetComponent<SnakeSegment>();
        if (segment != null)
        {
            if (cachedSnakeBody != null)
            {
                cachedSnakeBody.CutTail(segment.segmentIndex);
            }

            if (cachedSpawner != null) cachedSpawner.OnCarHitBody(this);
        }
    }
}   