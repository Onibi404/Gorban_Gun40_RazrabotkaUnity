using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class Unit : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
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

    private string TeamName =>
        Team == PlayerTeam.White
            ? "Белая"
            : "Чёрная";

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

        unitRenderer =
            GetComponent<Renderer>();

        ApplyMaterial();
        SnapToCell();
        UpdateName();
    }

    public void SetCell(Cell cell)
    {
        CurrentCell = cell;
        UpdateName();
    }

    public void SnapToCell()
    {
        if (CurrentCell == null)
            return;

        transform.position =
            CurrentCell.transform.position +
            Vector3.up * 0.35f;
    }

    public void Promote()
    {
        if (IsKing)
            return;

        IsKing = true;

        transform.localScale =
            new Vector3(
                0.72f,
                0.28f,
                0.72f);

        ApplyMaterial();
        UpdateName();
    }

    private void ApplyMaterial()
    {
        unitRenderer.material =
            Team == PlayerTeam.White
                ? IsKing
                    ? whiteKingMaterial
                    : whiteMaterial
                : IsKing
                    ? blackKingMaterial
                    : blackMaterial;
    }

    private void UpdateName()
    {
        string unitType =
            IsKing
                ? "дамка"
                : "шашка";

        gameObject.name =
            $"{TeamName} {unitType} " +
            $"[{CurrentCell.X}, {CurrentCell.Y}]";
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        CurrentCell?.SetHover(true);
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        CurrentCell?.SetHover(false);
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (
            eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        battleController?.HandleCellSelected(
            CurrentCell);
    }
}
