using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Плавное управление головой. Голова всегда движется вперёд, а игрок задаёт
/// только поворот — так управление одинаково хорошо работает с клавиатурой,
/// геймпадом и на мобильном экране.
/// </summary>
public class SnakeMovement : MonoBehaviour
{
    private static readonly List<SnakeMovement> activePlayers = new List<SnakeMovement>();
    public static SnakeMovement Active { get; private set; }
    public static IReadOnlyList<SnakeMovement> Players => activePlayers;

    [Header("Игрок")]
    [Range(0, 1)] public int playerIndex;

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
        if (!activePlayers.Contains(this)) activePlayers.Add(this);
        if (Active == null) Active = this;
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
        activePlayers.Remove(this);
        if (Active == this) Active = activePlayers.Count > 0 ? activePlayers[0] : null;
    }

    private void Update()
    {
        if (!CanMove())
        {
            horizontalInput = 0f;
            IsBoosting = false;
            return;
        }

        horizontalInput = ReadTurnInput();
        if (playerIndex == 0 && useMouseSteering && Mathf.Abs(horizontalInput) < 0.05f)
        {
            float center = Screen.width * 0.5f;
            horizontalInput = Mathf.Clamp((Input.mousePosition.x - center) / Mathf.Max(1f, center), -1f, 1f);
        }

        bool wantsBoost = playerIndex == 0
            ? Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift)
            : Input.GetKey(KeyCode.Keypad0) || Input.GetKey(KeyCode.RightShift);
        IsBoosting = wantsBoost && boostEnergy > 0.02f;
        if (IsBoosting) boostEnergy = Mathf.Max(0f, boostEnergy - boostDrain * Time.deltaTime);
        else boostEnergy = Mathf.Min(maxBoostEnergy, boostEnergy + boostRecharge * Time.deltaTime);
        graceTimer = Mathf.Max(0f, graceTimer - Time.deltaTime);
    }

    private float ReadTurnInput()
    {
        if (playerIndex == 1)
        {
            float arrows = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) arrows -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) arrows += 1f;
            return arrows;
        }

        float wasd = 0f;
        if (Input.GetKey(KeyCode.A)) wasd -= 1f;
        if (Input.GetKey(KeyCode.D)) wasd += 1f;
        return wasd;
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
            if (GameManager.Instance != null) GameManager.Instance.RegisterCarEaten(this);
            CarSpawner spawner = FindFirstObjectByType<CarSpawner>();
            if (spawner != null) spawner.OnCarEaten(car);
            Invoke(nameof(ResetCollisionLock), 0.08f);
            return;
        }

        Pickup pickup = other.GetComponentInParent<Pickup>();
        if (pickup != null)
        {
            pickup.Collect(this);
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
        if (collision == null) return;
        if (collision.collider.GetComponentInParent<ArenaHazard>() != null ||
            collision.collider.GetComponentInParent<SnakeMovement>() != null)
        {
            DamageOnce();
        }
    }

    private void DamageOnce()
    {
        if (graceTimer > 0f) return;
        graceTimer = collisionGraceTime;
        GameManager.Instance?.TakeDamage(this);
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
