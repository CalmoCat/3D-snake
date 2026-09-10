using UnityEngine;

/// <summary>
/// Плавное управление головой. Голова всегда движется вперёд, а игрок задаёт
/// только поворот — так управление одинаково хорошо работает с клавиатурой,
/// геймпадом и на мобильном экране.
/// </summary>
public class SnakeMovement : MonoBehaviour
{
    public static SnakeMovement Active { get; private set; }

    [Header("Движение")]
    public float speed = 16f;
    public float rotationSpeed = 210f;
    public float boostMultiplier = 1.65f;
    public float maxBoostEnergy = 2.5f;
    public float boostDrain = 0.85f;
    public float boostRecharge = 0.35f;

    [Header("Безопасность")]
    public float collisionGraceTime = 0.8f;
    public bool useMouseSteering = false;

    private Rigidbody rb;
    private SnakeBody cachedBody;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float horizontalInput;
    private float boostEnergy;
    private float graceTimer;
    private bool consumedCollision;

    public float BoostEnergy01 => maxBoostEnergy <= 0f ? 0f : boostEnergy / maxBoostEnergy;
    public bool IsBoosting { get; private set; }

    private void Awake()
    {
        Active = this;
        rb = GetComponent<Rigidbody>();
        cachedBody = GetComponentInParent<SnakeBody>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        boostEnergy = maxBoostEnergy;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    private void Update()
    {
        if (!CanMove())
        {
            horizontalInput = 0f;
            IsBoosting = false;
            return;
        }

        horizontalInput = Mathf.Clamp(Input.GetAxisRaw("Horizontal"), -1f, 1f);
        if (useMouseSteering && Mathf.Abs(horizontalInput) < 0.05f)
        {
            float center = Screen.width * 0.5f;
            horizontalInput = Mathf.Clamp((Input.mousePosition.x - center) / Mathf.Max(1f, center), -1f, 1f);
        }

        bool wantsBoost = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift);
        IsBoosting = wantsBoost && boostEnergy > 0.02f;
        if (IsBoosting) boostEnergy = Mathf.Max(0f, boostEnergy - boostDrain * Time.deltaTime);
        else boostEnergy = Mathf.Min(maxBoostEnergy, boostEnergy + boostRecharge * Time.deltaTime);
        graceTimer = Mathf.Max(0f, graceTimer - Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!CanMove()) return;

        float turn = horizontalInput * rotationSpeed * Time.fixedDeltaTime;
        Quaternion nextRotation = transform.rotation * Quaternion.Euler(0f, turn, 0f);
        float configuredSpeed = Mathf.Clamp(GameSettings.SnakeSpeed, GameSettings.MinSnakeSpeed, GameSettings.MaxSnakeSpeed);
        float runSpeed = configuredSpeed * GameSettings.DifficultySpeedMultiplier;
        if (GameManager.Instance != null && GameManager.Instance.FuryTime > 0f) runSpeed *= 1.12f;
        if (IsBoosting) runSpeed *= boostMultiplier;

        Vector3 nextPosition = transform.position + (nextRotation * Vector3.forward) * runSpeed * Time.fixedDeltaTime;
        if (rb != null)
        {
            rb.MoveRotation(nextRotation);
            rb.MovePosition(nextPosition);
        }
        else
        {
            transform.SetPositionAndRotation(nextPosition, nextRotation);
        }
    }

    private bool CanMove()
    {
        return GameManager.Instance == null || GameManager.Instance.State == GameManager.RunState.Playing;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || graceTimer > 0f || consumedCollision) return;

        Car car = other.GetComponentInParent<Car>();
        if (car != null && car.TryConsume())
        {
            consumedCollision = true;
            if (cachedBody != null) cachedBody.AddSegment();
            if (GameManager.Instance != null) GameManager.Instance.RegisterCarEaten();
            CarSpawner spawner = FindFirstObjectByType<CarSpawner>();
            if (spawner != null) spawner.OnCarEaten(car);
            Invoke(nameof(ResetCollisionLock), 0.08f);
            return;
        }

        Pickup pickup = other.GetComponentInParent<Pickup>();
        if (pickup != null)
        {
            pickup.Collect();
            return;
        }

        if (other.GetComponentInParent<ArenaHazard>() != null)
        {
            DamageOnce();
            return;
        }

        SnakeSegment hitSegment = other.GetComponentInParent<SnakeSegment>();
        if (hitSegment != null && hitSegment.segmentIndex >= 2)
        {
            DamageOnce();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && collision.collider.GetComponentInParent<ArenaHazard>() != null)
        {
            DamageOnce();
        }
    }

    private void DamageOnce()
    {
        if (graceTimer > 0f) return;
        graceTimer = collisionGraceTime;
        GameManager.Instance?.TakeDamage();
    }

    private void ResetCollisionLock()
    {
        consumedCollision = false;
    }

    public void RespawnAfterHit(bool shielded)
    {
        graceTimer = shielded ? 1.5f : 1.25f;
        consumedCollision = false;
        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
        }
        else
        {
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        }
        cachedBody?.ResetBehindHead();
    }

    public void ResetRun()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        boostEnergy = maxBoostEnergy;
        RespawnAfterHit(true);
    }
}
