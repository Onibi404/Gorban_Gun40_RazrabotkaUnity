using System.Collections.Generic;
using UnityEngine;

public class CheckersCommand : IGameplayCommand
{
    private readonly Battlefield battlefield;
    private readonly PlayerController playerController;

    private readonly List<CheckersMove> availableMoves = new List<CheckersMove>();
    private Unit selectedUnit;
    private CheckersMove pendingMove;

    public PlayerTeam CurrentTeam { get; private set; } = PlayerTeam.White;
    public Unit SelectedUnit => selectedUnit;
    public CheckersMove PendingMove => pendingMove;

    public CheckersCommand(Battlefield battlefield, PlayerController playerController)
    {
        this.battlefield = battlefield;
        this.playerController = playerController;
    }

    public void Interact(Cell cell)
    {
        if (cell == null || playerController.IsBusy)
            return;

        if (cell.OccupiedUnit != null && cell.OccupiedUnit.Team == CurrentTeam)
        {
            SelectUnit(cell.OccupiedUnit);
            return;
        }

        if (selectedUnit == null)
            return;

        CheckersMove move = availableMoves.Find(candidate => candidate.To == cell);
        if (move != null)
            SetPendingMove(move);
    }

    public void Confirm()
    {
        if (pendingMove == null || playerController.IsBusy)
            return;

        CheckersMove move = pendingMove;
        playerController.VisualizeMove(move, () => CompleteMove(move));
    }

    public void Cancel()
    {
        if (playerController.IsBusy)
            return;

        if (pendingMove != null)
        {
            pendingMove = null;
            ShowAvailableMoves();
            return;
        }

        ClearSelection();
    }

    private void SelectUnit(Unit unit)
    {
        selectedUnit = unit;
        pendingMove = null;
        availableMoves.Clear();
        availableMoves.AddRange(GetLegalMoves(unit));
        ShowAvailableMoves();
    }

    private void SetPendingMove(CheckersMove move)
    {
        pendingMove = move;
        battlefield.ClearHighlights();
        selectedUnit.CurrentCell.SetHighlight(CellHighlight.Selected);
        move.To.SetHighlight(CellHighlight.Pending);
    }

    private void ShowAvailableMoves()
    {
        battlefield.ClearHighlights();

        if (selectedUnit == null)
            return;

        selectedUnit.CurrentCell.SetHighlight(CellHighlight.Selected);
        foreach (CheckersMove move in availableMoves)
            move.To.SetHighlight(move.IsAttack ? CellHighlight.Attack : CellHighlight.Move);
    }

    private void ClearSelection()
    {
        selectedUnit = null;
        pendingMove = null;
        availableMoves.Clear();
        battlefield.ClearHighlights();
    }

    private void CompleteMove(CheckersMove move)
    {
        move.From.ClearUnit();
        move.To.SetUnit(move.Unit);
        move.Unit.SetCell(move.To);

        if (move.CapturedUnit != null)
        {
            Cell capturedCell = move.CapturedUnit.CurrentCell;
            capturedCell.ClearUnit();
            Object.Destroy(move.CapturedUnit.gameObject);
        }

        TryPromote(move.Unit);
        CurrentTeam = CurrentTeam == PlayerTeam.White ? PlayerTeam.Black : PlayerTeam.White;
        ClearSelection();

        Debug.Log($"Ход завершён. Теперь ходят: {(CurrentTeam == PlayerTeam.White ? "Белые" : "Чёрные")}");
    }

    private void TryPromote(Unit unit)
    {
        if (unit.IsKing)
            return;

        bool reachedEnd =
            (unit.Team == PlayerTeam.White && unit.CurrentCell.Y == Battlefield.BoardSize - 1) ||
            (unit.Team == PlayerTeam.Black && unit.CurrentCell.Y == 0);

        if (reachedEnd)
            unit.Promote();
    }

    private List<CheckersMove> GetLegalMoves(Unit unit)
    {
        return unit.IsKing ? GetKingMoves(unit) : GetManMoves(unit);
    }

    private List<CheckersMove> GetManMoves(Unit unit)
    {
        List<CheckersMove> moves = new List<CheckersMove>();
        int forward = unit.Team == PlayerTeam.White ? 1 : -1;

        AddSimpleMove(unit, -1, forward, moves);
        AddSimpleMove(unit, 1, forward, moves);

        AddCapture(unit, -1, -1, moves);
        AddCapture(unit, -1, 1, moves);
        AddCapture(unit, 1, -1, moves);
        AddCapture(unit, 1, 1, moves);

        return moves;
    }

    private void AddSimpleMove(Unit unit, int dx, int dy, List<CheckersMove> moves)
    {
        Cell to = battlefield.GetCell(unit.CurrentCell.X + dx, unit.CurrentCell.Y + dy);
        if (to != null && to.OccupiedUnit == null)
            moves.Add(new CheckersMove(unit, unit.CurrentCell, to));
    }

    private void AddCapture(Unit unit, int dx, int dy, List<CheckersMove> moves)
    {
        Cell enemyCell = battlefield.GetCell(unit.CurrentCell.X + dx, unit.CurrentCell.Y + dy);
        Cell landingCell = battlefield.GetCell(unit.CurrentCell.X + dx * 2, unit.CurrentCell.Y + dy * 2);

        if (enemyCell == null || landingCell == null)
            return;

        Unit enemy = enemyCell.OccupiedUnit;
        if (enemy != null && enemy.Team != unit.Team && landingCell.OccupiedUnit == null)
            moves.Add(new CheckersMove(unit, unit.CurrentCell, landingCell, enemy));
    }

    private List<CheckersMove> GetKingMoves(Unit unit)
    {
        List<CheckersMove> moves = new List<CheckersMove>();
        AddKingDirection(unit, 1, 1, moves);
        AddKingDirection(unit, 1, -1, moves);
        AddKingDirection(unit, -1, 1, moves);
        AddKingDirection(unit, -1, -1, moves);
        return moves;
    }

    private void AddKingDirection(Unit unit, int dx, int dy, List<CheckersMove> moves)
    {
        int x = unit.CurrentCell.X + dx;
        int y = unit.CurrentCell.Y + dy;

        while (battlefield.IsInside(x, y))
        {
            Cell cell = battlefield.GetCell(x, y);

            if (cell.OccupiedUnit == null)
            {
                moves.Add(new CheckersMove(unit, unit.CurrentCell, cell));
                x += dx;
                y += dy;
                continue;
            }

            if (cell.OccupiedUnit.Team == unit.Team)
                break;

            Cell landing = battlefield.GetCell(x + dx, y + dy);
            if (landing != null && landing.OccupiedUnit == null)
                moves.Add(new CheckersMove(unit, unit.CurrentCell, landing, cell.OccupiedUnit));

            break;
        }
    }
}
