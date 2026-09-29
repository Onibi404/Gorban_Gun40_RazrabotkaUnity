using UnityEngine;

public class Battlefield : MonoBehaviour
{
    public const int BoardSize = 8;

    [Header("Префабы")]
    [SerializeField] private Cell cellPrefab;
    [SerializeField] private Unit unitPrefab;

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

    private readonly Cell[,] cells = new Cell[BoardSize, BoardSize];
    private BattleController battleController;

    public void Initialize(BattleController controller)
    {
        battleController = controller;
        CreateBoard();
        CreateUnits();
    }

    private void CreateBoard()
    {
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                CreateCell(x, y);
            }
        }
    }

    private void CreateCell(int x, int y)
    {
        Cell cell = Instantiate(cellPrefab, transform);
        cell.transform.localPosition = new Vector3(x, 0f, y);

        Material baseMaterial = IsDarkCell(x, y) ? darkMaterial : lightMaterial;
        cell.Initialize(
            x, y, battleController,
            baseMaterial,
            hoverMaterial,
            moveMaterial,
            attackMaterial,
            selectedMaterial,
            pendingMaterial);

        cells[x, y] = cell;
    }

    private void CreateUnits()
    {
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (IsDarkCell(x, y))
                    SpawnUnit(PlayerTeam.White, cells[x, y]);
            }
        }

        for (int y = BoardSize - 3; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (IsDarkCell(x, y))
                    SpawnUnit(PlayerTeam.Black, cells[x, y]);
            }
        }
    }

    private void SpawnUnit(PlayerTeam team, Cell cell)
    {
        Unit unit = Instantiate(unitPrefab, transform);
        unit.Initialize(
            team,
            cell,
            battleController,
            whiteUnitMaterial,
            blackUnitMaterial,
            whiteKingMaterial,
            blackKingMaterial);
        cell.SetUnit(unit);
    }

    public Cell GetCell(int x, int y)
    {
        if (!IsInside(x, y)) return null;
        return cells[x, y];
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < BoardSize && y >= 0 && y < BoardSize;
    }

    public bool IsDarkCell(int x, int y)
    {
        return (x + y) % 2 == 1;
    }

    public void ClearHighlights()
    {
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                cells[x, y].SetHighlight(CellHighlight.None);
            }
        }
    }
}
