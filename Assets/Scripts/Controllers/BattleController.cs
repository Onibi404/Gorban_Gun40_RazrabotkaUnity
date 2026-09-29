using UnityEngine;

public class BattleController : MonoBehaviour
{
    [SerializeField] private Battlefield battlefield;
    [SerializeField] private PlayerController playerController;

    private CheckersCommand checkersCommand;
    private Camera mainCamera;
    private Cell hoveredCell;

    public PlayerTeam CurrentTeam => checkersCommand != null
        ? checkersCommand.CurrentTeam
        : PlayerTeam.White;

    private void Start()
    {
        mainCamera = Camera.main;

        if (battlefield == null || playerController == null)
        {
            Debug.LogError("BattleController: не назначена ссылка на Battlefield или PlayerController.");
            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("BattleController: главная камера не найдена. Камера должна иметь тег MainCamera.");
            enabled = false;
            return;
        }

        checkersCommand = new CheckersCommand(battlefield, playerController);
        battlefield.Initialize(this);
    }

    private void Update()
    {
        if (checkersCommand == null || playerController.IsBusy)
            return;

        UpdateHover();

        if (Input.GetMouseButtonDown(0))
            HandleMouseClick();

        if (Input.GetKeyDown(KeyCode.Escape))
            checkersCommand.Cancel();

        if (Input.GetKeyDown(KeyCode.Space))
            checkersCommand.Confirm();
    }

    private void UpdateHover()
    {
        Cell cellUnderMouse = GetCellUnderMouse();

        if (cellUnderMouse == hoveredCell)
            return;

        if (hoveredCell != null)
            hoveredCell.SetHover(false);

        hoveredCell = cellUnderMouse;

        if (hoveredCell != null)
            hoveredCell.SetHover(true);
    }

    private void HandleMouseClick()
    {
        Cell selectedCell = GetCellUnderMouse();
        if (selectedCell != null)
            HandleCellSelected(selectedCell);
    }

    // Deliberately does not use Unity Physics.
    // The mouse ray is intersected with the mathematical plane of the board,
    // then the local X/Z position is converted to checkerboard coordinates.
    private Cell GetCellUnderMouse()
    {
        if (mainCamera == null || battlefield == null)
            return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane boardPlane = new Plane(Vector3.up, battlefield.transform.position);

        if (!boardPlane.Raycast(ray, out float enter))
            return null;

        Vector3 worldPoint = ray.GetPoint(enter);
        Vector3 localPoint = battlefield.transform.InverseTransformPoint(worldPoint);

        int x = Mathf.FloorToInt(localPoint.x + 0.5f);
        int y = Mathf.FloorToInt(localPoint.z + 0.5f);

        return battlefield.GetCell(x, y);
    }

    public void HandleCellSelected(Cell cell)
    {
        if (checkersCommand != null)
            checkersCommand.Interact(cell);
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 460, 142), "Шашки");
        GUI.Label(new Rect(25, 38, 430, 20), $"Ход: {GetTeamName(CurrentTeam)}");
        GUI.Label(new Rect(25, 60, 430, 20), "ЛКМ: выбрать шашку или клетку назначения");
        GUI.Label(new Rect(25, 82, 430, 20), "Пробел: подтвердить ход    Esc: отменить");
        GUI.Label(new Rect(25, 104, 430, 20), "Зелёный — ход, красный — взятие");
        GUI.Label(new Rect(25, 124, 430, 20), "Синий — выбранная шашка, фиолетовый — выбранный ход");
    }

    private static string GetTeamName(PlayerTeam team)
    {
        return team == PlayerTeam.White ? "Белые" : "Чёрные";
    }
}
