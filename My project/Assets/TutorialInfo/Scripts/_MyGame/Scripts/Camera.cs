using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Цель слежения")]
    public Transform target;      

    [Header("Смещение камеры")]
    public Vector3 positionOffset = new Vector3(0, 8, -10);  

    [Header("Поворот камеры")]
    public bool rotateWithTarget = true;      
    public Vector3 rotationOffset = new Vector3(25, 0, 0);  
    public bool smoothRotation = true;        
    public float rotationSmoothSpeed = 5f;     

    [Header("Слежение")]
    public bool smoothFollow = true;          
    public float followSmoothSpeed = 5f;       

    private Quaternion targetRotation;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + target.rotation * positionOffset;

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }

        if (rotateWithTarget)
        {
            targetRotation = target.rotation * Quaternion.Euler(rotationOffset);

            if (smoothRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.rotation = targetRotation;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.green;
            Vector3 targetPos = target.position + target.rotation * positionOffset;
            Gizmos.DrawWireSphere(targetPos, 0.5f);
            Gizmos.DrawLine(transform.position, targetPos);
        }
    }
}