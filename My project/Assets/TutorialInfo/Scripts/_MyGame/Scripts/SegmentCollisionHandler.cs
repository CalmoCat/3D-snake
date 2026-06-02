using UnityEngine;

public class SegmentCollisionHandler : MonoBehaviour
{
    private SnakeBody snakeBody;
    private CarSpawner cachedSpawner;

    public void Initialize(SnakeBody body)
    {
        snakeBody = body;
        cachedSpawner = FindFirstObjectByType<CarSpawner>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            Destroy(other.gameObject);

            if (cachedSpawner != null)
            {
                cachedSpawner.SpawnCar();
            }
        }
    }
}