public sealed class CheckersMove
{
    public Unit Unit { get; }
    public Cell From { get; }
    public Cell To { get; }
    public Unit CapturedUnit { get; }

    public bool IsAttack => CapturedUnit != null;

    public CheckersMove(Unit unit, Cell from, Cell to, Unit capturedUnit = null)
    {
        Unit = unit;
        From = from;
        To = to;
        CapturedUnit = capturedUnit;
    }
}
