using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public PlayerHealth health;
    public DayNightCycle cycle;
    public GameObject panel;
    public Text description;
    public Button restartButton;
    bool restarting;
    void Start()
    {
        if (!panel || !description || !restartButton || !health)
        { Debug.LogError("GameOverUI is missing references.", this); enabled = false; return; }
        restartButton.onClick.AddListener(Restart);
        panel.SetActive(health.IsDead);
        if (health.IsDead) Show();
    }
    void OnDestroy() { if (restartButton) restartButton.onClick.RemoveListener(Restart); }
    public void Show()
    {
        if (!panel || !description) return;
        description.text = "SURVIVED UNTIL DAY " + (cycle ? cycle.CurrentDay : 1) + "\n\nCause of death: " + health.CauseOfDeath;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }
    public void Restart()
    {
        if (restarting || !health || health.IsAlive) return;
        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().path);
    }
}
