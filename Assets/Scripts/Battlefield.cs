using UnityEngine;

public class Battlefield : MonoBehaviour
{
    public const int BoardSize = 8;

    [Header("Материалы клеток")]
    [SerializeField] private Material lightMaterial;
    [SerializeField] private Material darkMaterial;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material moveMaterial;
    [SerializeField] private Material attackMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material pendingMaterial;

    [Header("Материалы шашек")]
    [SerializeField] private Material whiteUnitMaterial;
    [SerializeField] private Material blackUnitMaterial;
    [SerializeField] private Material whiteKingMaterial;
    [SerializeField] private Material blackKingMaterial;

    private Cell[,] cells;

    public void Initialize(BattleController controller)
    {
        CacheSceneCells(controller);
        LinkSceneUnits(controller);
    }

    private void CacheSceneCells(BattleController controller)
    {
        cells = new Cell[BoardSize, BoardSize];

        Cell[] sceneCells = GetComponentsInChildren<Cell>(true);

        foreach (Cell cell in sceneCells)
        {
            Vector3 localPosition =
                transform.InverseTransformPoint(cell.transform.position);

            int x = Mathf.RoundToInt(localPosition.x);
            int y = Mathf.RoundToInt(localPosition.z);

            if (!IsInside(x, y))
            {
                Debug.LogError(
                    $"Battlefield: клетка {cell.name} находится вне поля 8x8.",
                    cell);

                continue;
            }

            if (cells[x, y] != null)
            {
                Debug.LogError(
                    $"Battlefield: в координатах [{x}, {y}] найдено две клетки.",
                    cell);

                continue;
            }

            Material baseMaterial =
                IsDarkCell(x, y)
                    ? darkMaterial
                    : lightMaterial;

            cell.Initialize(
                x,
                y,
                controller,
                baseMaterial,
                hoverMaterial,
                moveMaterial,
                attackMaterial,
                selectedMaterial,
                pendingMaterial);

            cell.ClearUnit();

            cells[x, y] = cell;
        }

        if (sceneCells.Length != BoardSize * BoardSize)
        {
            Debug.LogError(
                $"Battlefield: в сцене должно быть {BoardSize * BoardSize} клеток, " +
                $"найдено {sceneCells.Length}.",
                this);
        }
    }

    private void LinkSceneUnits(BattleController controller)
    {
        Unit[] sceneUnits =
            GetComponentsInChildren<Unit>(true);

        foreach (Unit unit in sceneUnits)
        {
            Vector3 localPosition =
                transform.InverseTransformPoint(unit.transform.position);

            int x = Mathf.RoundToInt(localPosition.x);
            int y = Mathf.RoundToInt(localPosition.z);

            Cell cell = GetCell(x, y);

            if (cell == null)
            {
                Debug.LogError(
                    $"Battlefield: для шашки {unit.name} " +
                    $"не найдена клетка [{x}, {y}].",
                    unit);

                continue;
            }

            PlayerTeam team;

            if (y <= 2)
            {
                team = PlayerTeam.White;
            }
            else if (y >= BoardSize - 3)
            {
                team = PlayerTeam.Black;
            }
            else
            {
                Debug.LogError(
                    $"Battlefield: стартовая шашка {unit.name} " +
                    $"находится в недопустимом ряду {y}.",
                    unit);

                continue;
            }

            unit.Initialize(
                team,
                cell,
                controller,
                whiteUnitMaterial,
                blackUnitMaterial,
                whiteKingMaterial,
                blackKingMaterial);

            cell.SetUnit(unit);
        }

        if (sceneUnits.Length != 24)
        {
            Debug.LogError(
                $"Battlefield: в стартовой сцене должно быть 24 шашки, " +
                $"найдено {sceneUnits.Length}.",
                this);
        }
    }

    public Cell GetCell(int x, int y)
    {
        if (cells == null || !IsInside(x, y))
            return null;

        return cells[x, y];
    }

    public bool IsInside(int x, int y)
    {
        return
            x >= 0 &&
            x < BoardSize &&
            y >= 0 &&
            y < BoardSize;
    }

    public bool IsDarkCell(int x, int y)
    {
        return (x + y) % 2 == 1;
    }

    public void ClearHighlights()
    {
        if (cells == null)
            return;

        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                cells[x, y]?.SetHighlight(CellHighlight.None);
            }
        }
    }
}
