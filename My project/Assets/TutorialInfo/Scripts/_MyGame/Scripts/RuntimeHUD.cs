using UnityEngine;
using UnityEngine.UI;

/// <summary>Небольшой слой обратной связи: подсказки и сообщения не требуют ассетов.</summary>
public class RuntimeHUD : MonoBehaviour
{
    public Text toastText;
    public Text hintText;
    public Image boostFill;
    public Image secondBoostFill;

    private float toastTimer;
    private GameManager manager;

    private void Start()
    {
        manager = GameManager.Instance;
        if (manager != null) manager.ToastRequested += ShowToast;
        ShowToast("Собери машины и бонусы. Не врежься в стены!");
    }

    private void Update()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastText != null)
            {
                Color color = toastText.color;
                color.a = Mathf.Clamp01(Mathf.Min(1f, toastTimer * 2f));
                toastText.color = color;
            }
        }

        if (boostFill != null && SnakeMovement.Active != null)
        {
            boostFill.fillAmount = SnakeMovement.Active.BoostEnergy01;
        }
        if (secondBoostFill != null && SnakeMovement.Players.Count > 1 && SnakeMovement.Players[1] != null)
        {
            secondBoostFill.fillAmount = SnakeMovement.Players[1].BoostEnergy01;
        }

        if (hintText != null && manager != null)
        {
            hintText.text = manager.State == GameManager.RunState.Paused
                ? "P / ESC — продолжить"
                : "A/D или ←/→ — поворот    SPACE — ускорение    P — пауза";
        }
    }

    private void OnDestroy()
    {
        if (manager != null) manager.ToastRequested -= ShowToast;
    }

    public void ShowToast(string message)
    {
        if (toastText == null) return;
        toastText.text = message;
        Color color = toastText.color;
        color.a = 1f;
        toastText.color = color;
        toastTimer = 3.2f;
    }
}
