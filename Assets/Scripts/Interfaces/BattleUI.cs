using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [Header("Информация о ходе")]
    [SerializeField] private TMP_Text turnText;

    [Header("Перезапуск")]
    [SerializeField] private GameObject restartPanel;
    [SerializeField] private Image restartFill;

    private void Awake()
    {
        restartPanel.SetActive(false);
        restartFill.fillAmount = 0f;
    }

    public void SetCurrentTeam(PlayerTeam team)
    {
        turnText.text =
            $"Ход: {GetTeamName(team)}";
    }

    public void ShowRestartProgress(bool visible)
    {
        restartPanel.SetActive(visible);
    }

    public void SetRestartProgress(float progress)
    {
        restartFill.fillAmount =
            Mathf.Clamp01(progress);
    }

    private string GetTeamName(PlayerTeam team)
    {
        return team == PlayerTeam.White
            ? "Белые"
            : "Чёрные";
    }
}
