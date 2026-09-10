using UnityEngine;

/// <summary>Камера следует за головой без рывков и не меняет высоту при резком повороте.</summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Цель")]
    public Transform target;
    public Vector3 positionOffset = new Vector3(0f, 15f, -14f);
    public Vector3 rotationOffset = new Vector3(42f, 0f, 0f);

    [Header("Плавность")]
    public bool rotateWithTarget = true;
    public bool smoothFollow = true;
    public bool smoothRotation = true;
    public float followSmoothSpeed = 7f;
    public float rotationSmoothSpeed = 8f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + target.rotation * positionOffset;
        if (smoothFollow)
        {
            float smooth = 1f - Mathf.Exp(-followSmoothSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / Mathf.Max(0.01f, followSmoothSpeed), Mathf.Infinity, Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smooth * 0.35f);
        }
        else transform.position = desiredPosition;

        if (rotateWithTarget)
        {
            Quaternion desiredRotation = target.rotation * Quaternion.Euler(rotationOffset);
            if (smoothRotation)
            {
                float smooth = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.unscaledDeltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, smooth);
            }
            else transform.rotation = desiredRotation;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Vector3 targetPosition = target.position + target.rotation * positionOffset;
        Gizmos.DrawWireSphere(targetPosition, 0.45f);
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}
