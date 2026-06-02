using UnityEngine;

public class SnakeMovement : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 180f;
    private Rigidbody rb;
    private SnakeBody cachedBody;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cachedBody = GetComponentInParent<SnakeBody>();
        speed = GameSettings.SnakeSpeed;
        rotationSpeed = GameSettings.SnakeRotationSpeed;
    }

    void FixedUpdate()
    {
        speed = GameSettings.SnakeSpeed;
        rotationSpeed = GameSettings.SnakeRotationSpeed;

        float rotation = Input.GetAxis("Horizontal");
        Quaternion turnRotation = Quaternion.Euler(0f, rotation * rotationSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
        rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
            if (cachedBody != null) cachedBody.AddSegment();

            Car eatenCar = other.GetComponent<Car>();
            if (eatenCar != null)
            {
                CarSpawner spawner = FindFirstObjectByType<CarSpawner>();
                if (spawner != null) spawner.OnCarEaten(eatenCar);
            }

            Destroy(other.gameObject);
        }

        if (other.CompareTag("House"))
        {
            if (GameManager.Instance != null) GameManager.Instance.TakeDamage();
        }

        SnakeSegment hitSegment = other.GetComponent<SnakeSegment>();
        if (hitSegment != null && hitSegment.segmentIndex >= 2)
        {
            if (GameManager.Instance != null) GameManager.Instance.TakeDamage();
        }
    }
}