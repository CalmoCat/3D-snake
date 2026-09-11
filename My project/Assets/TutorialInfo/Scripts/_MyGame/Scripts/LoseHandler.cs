using UnityEngine;

public class LoseHandler : MonoBehaviour
{
    [Tooltip("Тег старой сцены; новые препятствия используют ArenaHazard и не зависят от тегов.")]
    public string houseTag = "House";

    private void OnTriggerEnter(Collider other)
    {
        if (IsHazard(other)) RestartGame();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null && IsHazard(collision.collider)) RestartGame();
    }

    private bool IsHazard(Collider collider)
    {
        if (collider == null) return false;
        if (collider.GetComponentInParent<ArenaHazard>() != null) return true;
        try { return collider.CompareTag(houseTag); } catch (UnityException) { return false; }
    }

    public void RestartGame()
    {
        GameManager.Instance?.TakeDamage();
    }
}
