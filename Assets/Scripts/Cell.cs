using UnityEngine;
using UnityEngine.EventSystems;

public class Cell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public Unit OccupiedUnit { get; private set; }

    private BattleController battleController;
    private Renderer cellRenderer;
    private Material baseMaterial;
    private Material hoverMaterial;
    private Material moveMaterial;
    private Material attackMaterial;
    private Material selectedMaterial;
    private Material pendingMaterial;
    private CellHighlight highlight = CellHighlight.None;
    private bool isHovered;

    public void Initialize(
        int x,
        int y,
        BattleController controller,
        Material baseMat,
        Material hoverMat,
        Material moveMat,
        Material attackMat,
        Material selectedMat,
        Material pendingMat)
    {
        X = x;
        Y = y;
        battleController = controller;
        baseMaterial = baseMat;
        hoverMaterial = hoverMat;
        moveMaterial = moveMat;
        attackMaterial = attackMat;
        selectedMaterial = selectedMat;
        pendingMaterial = pendingMat;

        cellRenderer = GetComponent<Renderer>();
        gameObject.name = $"Клетка [{x}, {y}]";
        RefreshMaterial();
    }

    public void SetUnit(Unit unit)
    {
        OccupiedUnit = unit;
    }

    public void ClearUnit()
    {
        OccupiedUnit = null;
    }

    public void SetHighlight(CellHighlight newHighlight)
    {
        highlight = newHighlight;
        RefreshMaterial();
    }

    public void SetHover(bool hovered)
    {
        isHovered = hovered;
        RefreshMaterial();
    }

    private void RefreshMaterial()
    {
        if (cellRenderer == null) return;

        if (isHovered && highlight == CellHighlight.None)
        {
            cellRenderer.material = hoverMaterial;
            return;
        }

        switch (highlight)
        {
            case CellHighlight.Move:
                cellRenderer.material = moveMaterial;
                break;
            case CellHighlight.Attack:
                cellRenderer.material = attackMaterial;
                break;
            case CellHighlight.Selected:
                cellRenderer.material = selectedMaterial;
                break;
            case CellHighlight.Pending:
                cellRenderer.material = pendingMaterial;
                break;
            default:
                cellRenderer.material = baseMaterial;
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHover(true);
    public void OnPointerExit(PointerEventData eventData) => SetHover(false);

    public void OnPointerClick(PointerEventData eventData)
    {
        // Selection is handled centrally by BattleController from the mouse position.
        // This method remains for the IPointerClickHandler requirement from the assignment.
    }
}
