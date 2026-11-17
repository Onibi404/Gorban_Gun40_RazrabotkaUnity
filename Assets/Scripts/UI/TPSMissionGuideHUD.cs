using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Always-on mission checklist. The scene setup tool assigns references;
/// there are no runtime scene searches and no changes to player input.
/// </summary>
[DisallowMultipleComponent]
public sealed class TPSMissionGuideHUD : MonoBehaviour
{
    [Header("Linked by Tools > TPS Shooter > Add Mission Guide HUD")]
    [SerializeField] private TPSMissionDirector _mission;
    [SerializeField] private TMP_Text _currentTask;
    [SerializeField] private TMP_Text _steps;

    private int _lastStage = -1;
    private int _lastKills = -1;
    private int _lastRequiredKills = -1;

    public void Configure(TPSMissionDirector mission, TMP_Text currentTask, TMP_Text steps)
    {
        _mission = mission;
        _currentTask = currentTask;
        _steps = steps;
        _lastStage = -1;
        UpdateGuide();
    }

    private void OnEnable()
    {
        _lastStage = -1;
    }

    private void Update()
    {
        UpdateGuide();
    }

    private void UpdateGuide()
    {
        if (_mission == null || _steps == null || _currentTask == null)
            return;

        int stage = Mathf.Clamp(_mission.CurrentStage, 0, 6);
        int required = Mathf.Max(1, _mission.RequiredKills);
        int kills = Mathf.Clamp(_mission.CurrentKills, 0, required);
        if (stage == _lastStage && kills == _lastKills && required == _lastRequiredKills)
            return;

        _lastStage = stage;
        _lastKills = kills;
        _lastRequiredKills = required;

        string[] tasks =
        {
            "Войти в зелёную зону",
            "Подобрать ключ-карту",
            "Убить " + required + " врагов",
            "Открыть дверь (клавиша E)",
            "Войти в голубую зону",
            "Дойти до фиолетовой эвакуации",
            "Миссия завершена"
        };

        if (stage == 2)
            tasks[2] = "Убить врагов (" + kills + "/" + required + ")";

        _currentTask.text = stage == 6
            ? "<color=#7DE8BA><b>МИССИЯ ВЫПОЛНЕНА!</b></color>"
            : "<color=#FFE19D><b>СЕЙЧАС:</b></color> " + tasks[stage];

        var builder = new StringBuilder(420);
        for (int i = 0; i < tasks.Length; i++)
        {
            if (i > 0) builder.Append('\n');
            if (i < stage)
                builder.Append("<color=#75DAB0>").Append(i).Append(". ").Append(tasks[i]).Append("</color>");
            else if (i == stage)
                builder.Append("<color=#FFE19D><b>> ").Append(i).Append(". ").Append(tasks[i]).Append("</b></color>");
            else
                builder.Append("<color=#A8B4C8>").Append(i).Append(". ").Append(tasks[i]).Append("</color>");
        }
        _steps.text = builder.ToString();
    }
}
