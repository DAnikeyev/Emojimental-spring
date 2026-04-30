using BreakInfinity;
using Emojimental.Models;

namespace Emojimental.Services;

public sealed class GameState
{
    private const double Log10Of2 = 0.3010299956639812d;
    private const double SpawnPadding = 32d;
    private const double SpawnGap = 40d;
    private const double SnapThreshold = 24d;
    private const double BeamThickness = 8d;
    private const double OverlapInset = 8d;

    private readonly List<FieldNode> _fieldObjects = new();
    private readonly List<AlignmentBeam> _alignmentBeams = new();
    private DragContext? _dragContext;
    private int _nextFieldObjectId = 1;

    public double FieldWidth { get; private set; } = 1360d;
    public double FieldHeight { get; private set; } = 920d;

    public BigDouble Energy { get; private set; }

    public BigDouble Ice { get; private set; }

    public BigDouble Coin { get; private set; }

    public BigDouble Sapling { get; private set; }

    public BigDouble Tree { get; private set; }

    public BigDouble Water { get; private set; }

    public int SnowmanCount { get; private set; }

    public BigDouble SnowmanIncomePerSecond => SnowmanCount;

    public IReadOnlyList<FieldNode> FieldObjects => _fieldObjects;

    public IReadOnlyList<AlignmentBeam> AlignmentBeams => _alignmentBeams;

    public FieldNode? SelectedFieldObject { get; private set; }

    public event Action? Changed;

    public GameState()
    {
        AddFieldObject(FieldNodeType.Energy);
        AddFieldObject(FieldNodeType.Snow);
    }

    public void AddFieldObject()
        => AddFieldObject(FieldNodeType.Energy);

    public void AddFieldObject(FieldNodeType type)
    {
        var (x, y) = GetSpawnPosition();
        var fieldObject = new FieldNode(_nextFieldObjectId++, type, x, y);
        _fieldObjects.Add(fieldObject);
        SelectedFieldObject = fieldObject;
        RefreshConnections();
        Changed?.Invoke();
    }

    public void RemoveFieldObject()
    {
        if (_fieldObjects.Count == 0)
            return;

        RemoveFieldObject(_fieldObjects[^1].Type);
    }

    public bool CanRemoveFieldObject(FieldNodeType type)
        => _fieldObjects.Any(fieldObject => fieldObject.Type == type);

    public int CountFieldObjects(FieldNodeType type)
        => _fieldObjects.Count(fieldObject => fieldObject.Type == type);

    public void RemoveFieldObject(FieldNodeType type)
    {
        var removed = _fieldObjects.LastOrDefault(fieldObject => fieldObject.Type == type);
        if (removed is null)
            return;

        _fieldObjects.Remove(removed);
        if (SelectedFieldObject?.Id == removed.Id)
            SelectedFieldObject = _fieldObjects.Count > 0 ? _fieldObjects[^1] : null;
        RefreshConnections();
        Changed?.Invoke();
    }

    public void Advance(double deltaSeconds)
    {
        if ((_fieldObjects.Count == 0 && SnowmanCount == 0) || deltaSeconds <= 0d)
            return;

        var generatedEnergy = BigDouble.Zero;
        var generatedIce = BigDouble.Zero;
        foreach (var fieldObject in _fieldObjects)
        {
            var generatedValue = fieldObject.Advance(deltaSeconds);
            if (generatedValue <= BigDouble.Zero)
                continue;

            switch (fieldObject.Type)
            {
                case FieldNodeType.Snow:
                    generatedIce = generatedIce + generatedValue;
                    break;
                default:
                    generatedEnergy = generatedEnergy + generatedValue;
                    break;
            }
        }

        if (generatedEnergy > BigDouble.Zero)
            Energy = Energy + generatedEnergy;

        if (generatedIce > BigDouble.Zero)
            Ice = Ice + generatedIce;

        if (SnowmanCount > 0)
            Coin = Coin + (SnowmanIncomePerSecond * deltaSeconds);

        Changed?.Invoke();
    }

    public void SelectFieldObject(int? fieldObjectId)
    {
        if (fieldObjectId is null)
        {
            if (SelectedFieldObject is null)
                return;

            SelectedFieldObject = null;
            Changed?.Invoke();
            return;
        }

        var fieldObject = _fieldObjects.FirstOrDefault(candidate => candidate.Id == fieldObjectId.Value);
        if (fieldObject is null || SelectedFieldObject?.Id == fieldObject.Id)
            return;

        SelectedFieldObject = fieldObject;
        Changed?.Invoke();
    }

    public bool CanUpgradeSelectedFieldObject()
        => SelectedFieldObject is not null && Energy >= SelectedFieldObject.GetUpgradeCost();

    public void UpgradeSelectedFieldObject()
    {
        if (SelectedFieldObject is null)
            return;

        var cost = SelectedFieldObject.GetUpgradeCost();
        if (Energy < cost)
            return;

        Energy = Energy - cost;
        SelectedFieldObject.Upgrade();
        Changed?.Invoke();
    }

    public BigDouble GetSnowmanCost()
        => Pow2(SnowmanCount);

    public bool CanHireSnowman()
        => Ice >= GetSnowmanCost();

    public void HireSnowman()
    {
        var cost = GetSnowmanCost();
        if (Ice < cost)
            return;

        Ice = Ice - cost;
        SnowmanCount += 1;
        Changed?.Invoke();
    }

    public void SetFieldSize(double width, double height)
    {
        var nextWidth = Math.Max(FieldNode.Size + SpawnPadding * 2d, Math.Floor(width));
        var nextHeight = Math.Max(FieldNode.Size + SpawnPadding * 2d, Math.Floor(height));
        if (Math.Abs(FieldWidth - nextWidth) < 0.1d && Math.Abs(FieldHeight - nextHeight) < 0.1d)
            return;

        FieldWidth = nextWidth;
        FieldHeight = nextHeight;

        foreach (var fieldObject in _fieldObjects)
            fieldObject.SetPosition(ClampX(fieldObject.X), ClampY(fieldObject.Y));

        RefreshConnections();
        Changed?.Invoke();
    }

    public void BeginFieldObjectDrag(int fieldObjectId)
    {
        var fieldObject = _fieldObjects.FirstOrDefault(candidate => candidate.Id == fieldObjectId);
        if (fieldObject is null)
            return;

        _dragContext = new DragContext(
            fieldObjectId,
            _fieldObjects
                .Where(candidate => candidate.Id != fieldObjectId && Intersects(fieldObject.X, fieldObject.Y, candidate.X, candidate.Y))
                .Select(candidate => candidate.Id)
                .ToHashSet());
    }

    public void EndFieldObjectDrag(int fieldObjectId)
    {
        if (_dragContext?.FieldObjectId == fieldObjectId)
            _dragContext = null;
    }

    public void MoveFieldObject(int fieldObjectId, double x, double y)
    {
        var fieldObject = _fieldObjects.FirstOrDefault(candidate => candidate.Id == fieldObjectId);
        if (fieldObject is null)
            return;

        var ignoredOverlapIds = GetIgnoredOverlapIds(fieldObject);
        var (snappedX, snappedY) = GetSnappedPosition(fieldObject, x, y, ignoredOverlapIds);
        if (WouldOverlap(fieldObject, snappedX, snappedY, ignoredOverlapIds))
            return;

        if (Math.Abs(fieldObject.X - snappedX) < 0.1d && Math.Abs(fieldObject.Y - snappedY) < 0.1d)
            return;

        fieldObject.SetPosition(snappedX, snappedY);
        UpdateDragContext(fieldObject, snappedX, snappedY);
        RefreshConnections();
        Changed?.Invoke();
    }

    private (double X, double Y) GetSpawnPosition()
    {
        var columns = Math.Max(1, (int)((FieldWidth - SpawnPadding * 2d + SpawnGap) / (FieldNode.Size + SpawnGap)));
        var rows = Math.Max(1, (int)((FieldHeight - SpawnPadding * 2d + SpawnGap) / (FieldNode.Size + SpawnGap)));

        for (var index = 0; index < columns * rows; index++)
        {
            var row = index / columns;
            var column = index % columns;

            var x = ClampX(SpawnPadding + column * (FieldNode.Size + SpawnGap));
            var y = ClampY(SpawnPadding + row * (FieldNode.Size + SpawnGap));
            if (!_fieldObjects.Any(candidate => Math.Abs(candidate.X - x) < 0.1d && Math.Abs(candidate.Y - y) < 0.1d))
                return (x, y);
        }

        return (ClampX(SpawnPadding), ClampY(SpawnPadding));
    }

    private (double X, double Y) GetSnappedPosition(FieldNode moving, double x, double y, HashSet<int>? ignoredOverlapIds)
    {
        var snappedX = ClampX(x);
        var snappedY = ClampY(y);

        var nearestXDistance = SnapThreshold + 1d;
        var nearestYDistance = SnapThreshold + 1d;

        foreach (var candidate in _fieldObjects)
        {
            if (candidate.Id == moving.Id || ignoredOverlapIds?.Contains(candidate.Id) == true)
                continue;

            var xDistance = Math.Abs(candidate.X - snappedX);
            if (xDistance <= SnapThreshold && xDistance < nearestXDistance)
            {
                snappedX = candidate.X;
                nearestXDistance = xDistance;
            }

            var yDistance = Math.Abs(candidate.Y - snappedY);
            if (yDistance <= SnapThreshold && yDistance < nearestYDistance)
            {
                snappedY = candidate.Y;
                nearestYDistance = yDistance;
            }
        }

        return (snappedX, snappedY);
    }

    private bool WouldOverlap(FieldNode moving, double x, double y, HashSet<int>? ignoredOverlapIds)
    {
        foreach (var candidate in _fieldObjects)
        {
            if (candidate.Id == moving.Id || ignoredOverlapIds?.Contains(candidate.Id) == true)
                continue;

            if (Intersects(x, y, candidate.X, candidate.Y))
                return true;
        }

        return false;
    }

    private void RefreshConnections()
    {
        _alignmentBeams.Clear();
        foreach (var fieldObject in _fieldObjects)
            fieldObject.SetConnectionCount(0);

        foreach (var row in _fieldObjects.GroupBy(fieldObject => Math.Round(fieldObject.Y, 3)))
            ConnectAdjacent(row.OrderBy(fieldObject => fieldObject.X).ToList(), isHorizontal: true);

        foreach (var column in _fieldObjects.GroupBy(fieldObject => Math.Round(fieldObject.X, 3)))
            ConnectAdjacent(column.OrderBy(fieldObject => fieldObject.Y).ToList(), isHorizontal: false);
    }

    private void ConnectAdjacent(IReadOnlyList<FieldNode> group, bool isHorizontal)
    {
        for (var index = 0; index < group.Count - 1; index++)
        {
            var first = group[index];
            var second = group[index + 1];

            if (isHorizontal)
            {
                var left = first.X <= second.X ? first : second;
                var right = ReferenceEquals(left, first) ? second : first;
                var beamLeft = left.X + FieldNode.Size;
                var beamWidth = right.X - beamLeft;
                if (beamWidth <= 0d)
                    continue;

                _alignmentBeams.Add(new AlignmentBeam(
                    true,
                    beamLeft,
                    ((left.Y + right.Y) / 2d) + FieldNode.Size / 2d - BeamThickness / 2d,
                    beamWidth,
                    BeamThickness));
            }
            else
            {
                var top = first.Y <= second.Y ? first : second;
                var bottom = ReferenceEquals(top, first) ? second : first;
                var beamTop = top.Y + FieldNode.Size;
                var beamHeight = bottom.Y - beamTop;
                if (beamHeight <= 0d)
                    continue;

                _alignmentBeams.Add(new AlignmentBeam(
                    false,
                    ((top.X + bottom.X) / 2d) + FieldNode.Size / 2d - BeamThickness / 2d,
                    beamTop,
                    BeamThickness,
                    beamHeight));
            }

            first.SetConnectionCount(first.ConnectionCount + 1);
            second.SetConnectionCount(second.ConnectionCount + 1);
        }
    }

    private double ClampX(double value)
        => Math.Clamp(value, SpawnPadding, FieldWidth - FieldNode.Size - SpawnPadding);

    private double ClampY(double value)
        => Math.Clamp(value, SpawnPadding, FieldHeight - FieldNode.Size - SpawnPadding);

    private HashSet<int>? GetIgnoredOverlapIds(FieldNode moving)
        => _dragContext?.FieldObjectId == moving.Id ? _dragContext.IgnoredOverlapIds : null;

    private void UpdateDragContext(FieldNode moving, double x, double y)
    {
        if (_dragContext?.FieldObjectId != moving.Id || _dragContext.IgnoredOverlapIds.Count == 0)
            return;

        _dragContext.IgnoredOverlapIds.RemoveWhere(ignoredId =>
        {
            var candidate = _fieldObjects.FirstOrDefault(fieldObject => fieldObject.Id == ignoredId);
            return candidate is null || !Intersects(x, y, candidate.X, candidate.Y);
        });
    }

    private static bool Intersects(double firstX, double firstY, double secondX, double secondY)
    {
        var overlapsX = firstX < secondX + FieldNode.Size - OverlapInset && firstX + FieldNode.Size > secondX + OverlapInset;
        var overlapsY = firstY < secondY + FieldNode.Size - OverlapInset && firstY + FieldNode.Size > secondY + OverlapInset;
        return overlapsX && overlapsY;
    }

    private static BigDouble Pow2(int exponent)
    {
        if (exponent <= 0)
            return BigDouble.One;

        var base10Exponent = exponent * Log10Of2;
        var wholeExponent = (int)Math.Floor(base10Exponent);
        var mantissa = Math.Pow(10d, base10Exponent - wholeExponent);
        return new BigDouble(mantissa, wholeExponent);
    }

    private sealed record DragContext(int FieldObjectId, HashSet<int> IgnoredOverlapIds);
}
