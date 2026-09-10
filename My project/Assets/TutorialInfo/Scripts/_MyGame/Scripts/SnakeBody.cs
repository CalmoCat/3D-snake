using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хвост следует по истории движения головы. История ограничена реальной
/// длиной змеи, поэтому после долгого забега список не растёт бесконечно и не
/// вызывает скачков FPS.
/// </summary>
public class SnakeBody : MonoBehaviour
{
    [Header("Состав змеи")]
    public Transform head;
    public GameObject segmentPrefab;
    public float spacing = 1.55f;
    public float lerpSpeed = 20f;
    public Vector3 localOffset = new Vector3(0f, 0f, -0.75f);
    public Vector3 segmentRotationFix = Vector3.zero;
    public int startSegments = 4;

    [Header("Визуал")]
    public Color bodyColor = new Color(0.15f, 0.9f, 0.7f);
    public Color tailColor = new Color(0.08f, 0.45f, 0.5f);

    private readonly List<Transform> segments = new List<Transform>();
    private readonly List<Vector3> positionsHistory = new List<Vector3>();
    private const float HistoryStep = 0.05f;
    private const int MinimumHistory = 320;
    public int SegmentCount => segments.Count;
    public int BodyLength => Mathf.Max(0, segments.Count - 1);

    private void Start()
    {
        Initialize();
    }

    private void FixedUpdate()
    {
        if (head == null || segments.Count == 0) return;
        AppendHeadPosition();
        TrimHistory();

        for (int i = 1; i < segments.Count; i++)
        {
            int historyIndex = Mathf.Clamp(Mathf.RoundToInt(i * spacing / HistoryStep), 0, positionsHistory.Count - 1);
            Vector3 target = positionsHistory[historyIndex];
            Transform segment = segments[i];
            segment.position = Vector3.Lerp(segment.position, target, Mathf.Clamp01(lerpSpeed * Time.fixedDeltaTime));

            Vector3 lookDirection = segments[i - 1].position - segment.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                segment.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up) * Quaternion.Euler(segmentRotationFix);
            }
        }
    }

    public void Initialize()
    {
        if (head == null) return;
        if (segments.Count == 0) segments.Add(head);
        if (positionsHistory.Count == 0) ResetBehindHead();
        while (BodyLength < Mathf.Max(0, startSegments)) AddSegment();
    }

    private void AppendHeadPosition()
    {
        Vector3 current = head.TransformPoint(localOffset);
        Vector3 last = positionsHistory.Count > 0 ? positionsHistory[0] : current;
        Vector3 delta = current - last;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return;

        Vector3 direction = delta / distance;
        while (distance >= HistoryStep)
        {
            last += direction * HistoryStep;
            positionsHistory.Insert(0, last);
            distance -= HistoryStep;
        }
    }

    private void TrimHistory()
    {
        int required = Mathf.Max(MinimumHistory, Mathf.CeilToInt((segments.Count + 5) * spacing / HistoryStep) + 80);
        if (positionsHistory.Count > required)
        {
            positionsHistory.RemoveRange(required, positionsHistory.Count - required);
        }
    }

    public void AddSegment()
    {
        AddSegments(1);
    }

    public void AddSegments(int amount)
    {
        InitializeHeadOnly();
        if (head == null || amount <= 0) return;

        for (int i = 0; i < amount; i++)
        {
            int segmentIndex = segments.Count;
            EnsureHistoryCapacity(segmentIndex + 3);
            int historyIndex = Mathf.Clamp(Mathf.RoundToInt(segmentIndex * spacing / HistoryStep), 0, positionsHistory.Count - 1);
            Vector3 position = positionsHistory.Count > 0 ? positionsHistory[historyIndex] : head.TransformPoint(localOffset);
            Quaternion rotation = segments[segments.Count - 1].rotation;
            GameObject instance = segmentPrefab != null
                ? Instantiate(segmentPrefab, position, rotation, transform)
                : CreateFallbackSegment(position, rotation, segmentIndex);

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < colliders.Length; c++) colliders[c].isTrigger = true;
            SnakeSegment metadata = instance.GetComponent<SnakeSegment>();
            if (metadata == null) metadata = instance.AddComponent<SnakeSegment>();
            metadata.segmentIndex = segmentIndex;
            metadata.owner = this;
            instance.tag = "Untagged";
            segments.Add(instance.transform);
        }
        UpdateSegmentMetadata();
    }

    private void InitializeHeadOnly()
    {
        if (head == null) return;
        if (segments.Count == 0) segments.Add(head);
        if (positionsHistory.Count == 0) ResetBehindHead();
    }

    public void CutTail(int fromIndex)
    {
        if (fromIndex <= 0 || fromIndex >= segments.Count) return;
        for (int i = segments.Count - 1; i >= fromIndex; i--)
        {
            Transform tail = segments[i];
            segments.RemoveAt(i);
            if (tail != null && tail != head) Destroy(tail.gameObject);
        }
        TrimHistory();
        UpdateSegmentMetadata();
    }

    public void ResetBehindHead()
    {
        if (head == null) return;
        Vector3 anchor = head.TransformPoint(localOffset);
        Vector3 back = -head.forward;
        positionsHistory.Clear();
        int historyLength = Mathf.Max(MinimumHistory, (segments.Count + startSegments + 8) * 40);
        for (int i = 0; i < historyLength; i++) positionsHistory.Add(anchor + back * (HistoryStep * i));

        for (int i = 1; i < segments.Count; i++)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(i * spacing / HistoryStep), 0, positionsHistory.Count - 1);
            segments[i].position = positionsHistory[index];
            segments[i].rotation = head.rotation;
        }
    }

    private void EnsureHistoryCapacity(int segmentCountHint)
    {
        if (head == null) return;
        int required = Mathf.Max(MinimumHistory, Mathf.CeilToInt((segmentCountHint + 6) * spacing / HistoryStep) + 80);
        Vector3 direction = -head.forward;
        Vector3 last = positionsHistory.Count > 0 ? positionsHistory[positionsHistory.Count - 1] : head.TransformPoint(localOffset);
        while (positionsHistory.Count < required)
        {
            last += direction * HistoryStep;
            positionsHistory.Add(last);
        }
    }

    private void UpdateSegmentMetadata()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            SnakeSegment data = segments[i].GetComponent<SnakeSegment>();
            if (data == null) data = segments[i].gameObject.AddComponent<SnakeSegment>();
            data.segmentIndex = i;
            data.owner = this;
        }
    }

    private GameObject CreateFallbackSegment(Vector3 position, Quaternion rotation, int index)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        segment.name = "SnakeSegment_" + index.ToString("00");
        segment.transform.SetParent(transform);
        segment.transform.SetPositionAndRotation(position, rotation);
        segment.transform.localScale = Vector3.one * 1.05f;

        Renderer renderer = segment.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(FindSurfaceShader());
            float shade = Mathf.Clamp01(index / 18f);
            material.color = Color.Lerp(bodyColor, tailColor, shade);
            renderer.material = material;
        }
        return segment;
    }

    private Shader FindSurfaceShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
    }
}
