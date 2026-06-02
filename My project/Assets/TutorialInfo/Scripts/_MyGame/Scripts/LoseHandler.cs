using UnityEngine;

public class LoseHandler : MonoBehaviour
{
    [Header("Настройки проигрыша")]
    

    [Tooltip("Тег для дома или других препятствий")]
    public string houseTag = "House";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(houseTag))
        {
            RestartGame();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(houseTag))
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        Debug.Log("Столкновение! Потеря жизни...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage();
        }
    }
}