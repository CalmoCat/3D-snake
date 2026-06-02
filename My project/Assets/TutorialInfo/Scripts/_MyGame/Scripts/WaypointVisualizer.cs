using UnityEngine;
using System.Collections.Generic;

public class WaypointVisualizer : MonoBehaviour
{
    public Color lineColor = Color.yellow;
    public float waypointRadius = 0.5f;

    void OnDrawGizmos()
    {
        List<Transform> waypoints = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Waypoint"))
                waypoints.Add(child);
        }

        if (waypoints.Count < 2) return;

        Gizmos.color = lineColor;
        foreach (Transform wp in waypoints)
        {
            Gizmos.DrawWireSphere(wp.position, waypointRadius);
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        if (waypoints.Count > 2)
        {
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
        }
    }
}