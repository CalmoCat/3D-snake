using UnityEngine;

/// <summary>Совместимость со старыми префабами сегментов.</summary>
public class SegmentCollisionHandler : MonoBehaviour
{
    private SnakeBody snakeBody;
    private CarSpawner cachedSpawner;

    public void Initialize(SnakeBody body)
    {
        snakeBody = body;
        cachedSpawner = FindFirstObjectByType<CarSpawner>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Car car = other.GetComponentInParent<Car>();
        if (car == null || !car.TryConsume()) return;
        if (snakeBody == null) snakeBody = GetComponentInParent<SnakeBody>();
        snakeBody?.CutTail( Mathf.Max(2, snakeBody.SegmentCount - 1) );
        cachedSpawner?.OnCarHitBody(car);
    }
}
