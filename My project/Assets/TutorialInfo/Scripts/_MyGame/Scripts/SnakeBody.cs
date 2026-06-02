using UnityEngine;
using System.Collections.Generic;

public class SnakeBody : MonoBehaviour
{
    public Transform head;
    public GameObject segmentPrefab;
    public float spacing = 4f;
    public float lerpSpeed = 20f;
    public Vector3 localOffset = new Vector3(0, 0, -2.4f);
    public Vector3 segmentRotationFix = new Vector3(0, 90, 0);
    public int startSegments = 4;

    private List<Transform> segments = new List<Transform>();
    private List<Vector3> positionsHistory = new List<Vector3>();
    private const int BaseHistorySize = 600;
    private const float HistoryStep = 0.05f;

    void Start()
    {
        if (head == null) return;
        segments.Add(head);
        Vector3 tailAnchor = head.TransformPoint(localOffset);
        Vector3 backDir = -head.forward;
        for (int i = 0; i < BaseHistorySize; i++)
        {
            positionsHistory.Add(tailAnchor + backDir * (HistoryStep * i));
        }
        for (int i = 0; i < startSegments; i++) AddSegment();
    }

    void FixedUpdate()
    {
        if (head == null || segments.Count == 0) return;
        Vector3 tailAnchor = head.TransformPoint(localOffset);

        Vector3 last = positionsHistory.Count > 0 ? positionsHistory[0] : tailAnchor;
        Vector3 delta = tailAnchor - last;
        float dist = delta.magnitude;
        if (dist > 0.0001f)
        {
            Vector3 dir = delta / dist;
            while (dist >= HistoryStep)
            {
                last += dir * HistoryStep;
                positionsHistory.Insert(0, last);
                dist -= HistoryStep;
            }
        }

        int dynamicHistorySize = GetRequiredHistorySize();
        if (positionsHistory.Count > dynamicHistorySize)
        {
            int excessCount = positionsHistory.Count - dynamicHistorySize;
            positionsHistory.RemoveRange(dynamicHistorySize, excessCount);
        }

        for (int i = 1; i < segments.Count; i++)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(i * (spacing / HistoryStep)), 0, positionsHistory.Count - 1);
            segments[i].position = positionsHistory[index];
            segments[i].LookAt(segments[i - 1]);
            segments[i].Rotate(segmentRotationFix);
        }
    }

    public void AddSegment()
    {
        int newSegmentIndex = segments.Count;
        EnsureHistoryCapacity(newSegmentIndex + 2);
        int historyIndex = Mathf.Clamp(Mathf.RoundToInt(newSegmentIndex * (spacing / HistoryStep)), 0, positionsHistory.Count - 1);
        Vector3 spawnPosition = positionsHistory.Count > 0 ? positionsHistory[historyIndex] : head.TransformPoint(localOffset);
        Quaternion spawnRotation = segments[segments.Count - 1].rotation;
        GameObject newSeg = Instantiate(segmentPrefab, spawnPosition, spawnRotation);
        newSeg.tag = "Untagged";

        SnakeSegment segData = newSeg.GetComponent<SnakeSegment>();
        if (segData == null) segData = newSeg.AddComponent<SnakeSegment>();
        segData.segmentIndex = newSegmentIndex;

        segments.Add(newSeg.transform);
    }

    public void CutTail(int fromIndex)
    {
        if (fromIndex <= 0 || fromIndex >= segments.Count) return;
        int countToRemove = segments.Count - fromIndex;
        for (int i = 0; i < countToRemove; i++)
        {
            Transform last = segments[segments.Count - 1];
            segments.RemoveAt(segments.Count - 1);
            Destroy(last.gameObject);
        }
    }

    public void CheckHeadCollisionWithBody() { }

    private int GetRequiredHistorySize()
    {
        int minForSegments = Mathf.CeilToInt((segments.Count + 3) * (spacing / HistoryStep));
        return Mathf.Max(BaseHistorySize, minForSegments + 120);
    }

    private void EnsureHistoryCapacity(int segmentCountHint)
    {
        int required = Mathf.CeilToInt((segmentCountHint + 3) * (spacing / HistoryStep)) + 120;
        if (positionsHistory.Count >= required) return;

        Vector3 backDir = head != null ? -head.forward : Vector3.back;
        Vector3 last = positionsHistory.Count > 0 ? positionsHistory[positionsHistory.Count - 1] : Vector3.zero;

        int toAdd = required - positionsHistory.Count;
        for (int i = 0; i < toAdd; i++)
        {
            last += backDir * HistoryStep;
            positionsHistory.Add(last);
        }
    }
}