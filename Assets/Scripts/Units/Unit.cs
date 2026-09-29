using UnityEngine;
using UnityEngine.EventSystems;

public class Unit : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public PlayerTeam Team { get; private set; }
    public bool IsKing { get; private set; }
    public Cell CurrentCell { get; private set; }

    private BattleController battleController;
    private Renderer unitRenderer;
    private Material whiteMaterial;
    private Material blackMaterial;
    private Material whiteKingMaterial;
    private Material blackKingMaterial;

    public void Initialize(
        PlayerTeam team,
        Cell cell,
        BattleController controller,
        Material white,
        Material black,
        Material whiteKing,
        Material blackKing)
    {
        Team = team;
        CurrentCell = cell;
        battleController = controller;
        whiteMaterial = white;
        blackMaterial = black;
        whiteKingMaterial = whiteKing;
        blackKingMaterial = blackKing;

        unitRenderer = GetComponent<Renderer>();
        ApplyMaterial();
        SnapToCell();
        gameObject.name = $"{GetTeamName(team)} шашка [{cell.X}, {cell.Y}]";
    }

    public void SetCell(Cell cell)
    {
        CurrentCell = cell;
        gameObject.name = $"{GetTeamName(Team)} {(IsKing ? "дамка" : "шашка")} [{cell.X}, {cell.Y}]";
    }

    public void SnapToCell()
    {
        if (CurrentCell == null) return;
        transform.position = CurrentCell.transform.position + Vector3.up * 0.35f;
    }

    public void Promote()
    {
        if (IsKing) return;
        IsKing = true;
        transform.localScale = new Vector3(0.72f, 0.28f, 0.72f);
        ApplyMaterial();
        gameObject.name = $"{GetTeamName(Team)} дамка [{CurrentCell.X}, {CurrentCell.Y}]";
    }

    private void ApplyMaterial()
    {
        if (unitRenderer == null) return;

        if (Team == PlayerTeam.White)
            unitRenderer.material = IsKing ? whiteKingMaterial : whiteMaterial;
        else
            unitRenderer.material = IsKing ? blackKingMaterial : blackMaterial;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        CurrentCell?.SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CurrentCell?.SetHover(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Selection is handled centrally by BattleController from the mouse position.
        // This method remains for the IPointerClickHandler requirement from the assignment.
    }

    private static string GetTeamName(PlayerTeam team)
    {
        return team == PlayerTeam.White ? "Белая" : "Чёрная";
    }
}
