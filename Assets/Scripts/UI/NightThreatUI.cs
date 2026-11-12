using UnityEngine;
using UnityEngine.UI;

public class NightThreatUI : MonoBehaviour
{
    public EnemySpawner enemies;
    public Text label;
    string notice;
    float expires, nextRefresh;
    bool burning;
    void OnEnable()
    {
        if (enemies && enemies.cycle)
        {
            enemies.cycle.OnPhaseChanged += Phase;
            enemies.Warning += Warn;
            burning = enemies.safe.IsSafeZoneActive;
        }
    }

    void OnDisable()
    {
        if (enemies && enemies.cycle)
        {
            enemies.cycle.OnPhaseChanged -= Phase;
            enemies.Warning -= Warn;
        }
    }

    void Warn(string message)
    {
        notice = message;
        expires = Time.time + 3;
    }

    void Phase()
    {
        if (!enemies.cycle.IsNighttime)
        {
            notice = "";
            expires = 0;
        }
        else
            Warn("NIGHT HAS FALLEN");
    }

    void Update()
    {
        if (!enemies || !label)
            return;
        label.enabled = !InventoryUI.IsOpen && !BuildingController.ModalOpen && !PlayerSurvivalController.InputLocked;
        if (Time.time < nextRefresh)
            return;
        nextRefresh = Time.time + .2f;
        bool active = enemies.safe.IsSafeZoneActive;
        if (burning && !active && enemies.cycle.IsNighttime)
            Warn("THE CAMPFIRE HAS GONE OUT!");
        burning = active;
        if (!enemies.cycle.IsNighttime)
        {
            label.text = "";
            return;
        }

        bool safe = enemies.safe.IsPlayerSafe;
        label.text = "<color=#FFD86B>NIGHT " + enemies.cycle.CurrentDay + "</color>   |   WAVE " + enemies.CurrentWave + "   |   ENEMIES: <color=#FF8A6B>" + enemies.ActiveCount + "</color>"
            + "\nCAMPFIRE STATUS: " + (safe ? "<color=#8EE06A>SAFE</color>" : "<color=#FF5A4A>EXPOSED</color>")
            + (Time.time < expires ? "\n<color=#FFB347>" + notice + "</color>" : "");
    }
}
