using Emojimental.Models;

namespace Emojimental.Services;

public sealed class FieldState
{
    private static readonly FieldNodeSide[] AllSides =
    [
        FieldNodeSide.Left,
        FieldNodeSide.Right,
        FieldNodeSide.Top,
        FieldNodeSide.Bottom
    ];

    private const int GridColumns = 6;
    private const int GridRows = 4;
    private const int MaxObjects = GridColumns * GridRows;
    private const double ZoneGap = 32d;
    private const double GridPadding = 24d;

    private const double BeamThickness = 8d;

    private readonly List<FieldNode> _fieldObjects = new();
    private readonly List<AlignmentBeam> _alignmentBeams = new();
    private readonly List<BeamStarSlot> _beamStarSlots = new();
    private int _nextFieldObjectId = 1;

    public double FieldWidth => (FieldNode.Size * GridColumns) + (ZoneGap * (GridColumns - 1)) + (GridPadding * 2d);
    public double FieldHeight => (FieldNode.Size * GridRows) + (ZoneGap * (GridRows - 1)) + (GridPadding * 2d);

    public IReadOnlyList<FieldNode> FieldObjects => _fieldObjects;
    public IReadOnlyList<AlignmentBeam> AlignmentBeams => _alignmentBeams;
    public IReadOnlyList<BeamStarSlot> BeamStarSlots => _beamStarSlots;
    public FieldNode? SelectedFieldObject { get; private set; }

    public void AddFieldObject(FieldNodeType type)
    {
        if (_fieldObjects.Count >= MaxObjects)
            return;

        var zoneIndex = FindFirstEmptyZone();
        if (zoneIndex == -1)
            return;

        var (x, y) = GetZonePosition(zoneIndex);
        var fieldObject = new FieldNode(_nextFieldObjectId++, type, x, y);
        _fieldObjects.Add(fieldObject);
        AddPersistentBeamStarSlots(fieldObject.Id);
        SelectedFieldObject = fieldObject;
        RefreshConnections();
    }

    public void RemoveFieldObject(FieldNodeType type)
    {
        var removed = _fieldObjects.LastOrDefault(fieldObject => fieldObject.Type == type);
        if (removed is null)
            return;

        _fieldObjects.Remove(removed);
        _beamStarSlots.RemoveAll(slot => slot.SourceFieldNodeId == removed.Id);
        if (SelectedFieldObject?.Id == removed.Id)
            SelectedFieldObject = _fieldObjects.Count > 0 ? _fieldObjects[^1] : null;
        RefreshConnections();
    }

    public void SelectFieldObject(int? fieldObjectId)
    {
        if (fieldObjectId is null)
        {
            SelectedFieldObject = null;
            return;
        }

        var fieldObject = _fieldObjects.FirstOrDefault(candidate => candidate.Id == fieldObjectId.Value);
        if (fieldObject is null || SelectedFieldObject?.Id == fieldObject.Id)
            return;

        SelectedFieldObject = fieldObject;
    }

    public void SetFieldSize(double width, double height)
    {
        // Grid size is fixed, so we don't need to change FieldWidth/FieldHeight.
        // But we should refresh connections if needed, though they don't depend on surface size anymore.
        RefreshConnections();
    }

    public void BeginFieldObjectDrag(int fieldObjectId)
    {
        // No special drag context needed for grid snapping
    }

    public void EndFieldObjectDrag(int fieldObjectId)
    {
    }

    public void MoveFieldObject(int fieldObjectId, double x, double y)
    {
        var fieldObject = _fieldObjects.FirstOrDefault(candidate => candidate.Id == fieldObjectId);
        if (fieldObject is null)
            return;

        var zoneIndex = GetZoneIndexFromPosition(x + FieldNode.Size / 2d, y + FieldNode.Size / 2d);
        var (targetX, targetY) = GetZonePosition(zoneIndex);

        if (_fieldObjects.Any(candidate => candidate.Id != fieldObjectId && Math.Abs(candidate.X - targetX) < 0.1d && Math.Abs(candidate.Y - targetY) < 0.1d))
            return;

        if (Math.Abs(fieldObject.X - targetX) < 0.1d && Math.Abs(fieldObject.Y - targetY) < 0.1d)
            return;

        fieldObject.SetPosition(targetX, targetY);
        RefreshConnections();
    }

    public void RefreshConnections()
    {
        _alignmentBeams.Clear();
        for (var index = 0; index < _beamStarSlots.Count; index++)
            _beamStarSlots[index] = _beamStarSlots[index] with { TargetFieldNodeId = null, TargetSide = null };

        foreach (var fieldObject in _fieldObjects)
            fieldObject.SetConnectionCount(0);

        foreach (var row in _fieldObjects.GroupBy(fieldObject => Math.Round(fieldObject.Y, 3)))
            ConnectAdjacent(row.OrderBy(fieldObject => fieldObject.X).ToList(), isHorizontal: true);

        foreach (var column in _fieldObjects.GroupBy(fieldObject => Math.Round(fieldObject.X, 3)))
            ConnectAdjacent(column.OrderBy(fieldObject => fieldObject.Y).ToList(), isHorizontal: false);
    }

    private int FindFirstEmptyZone()
    {
        for (var i = 0; i < MaxObjects; i++)
        {
            var (x, y) = GetZonePosition(i);
            if (!_fieldObjects.Any(candidate => Math.Abs(candidate.X - x) < 0.1d && Math.Abs(candidate.Y - y) < 0.1d))
                return i;
        }
        return -1;
    }

    private int GetZoneIndexFromPosition(double x, double y)
    {
        var col = (int)Math.Floor((x - GridPadding) / (FieldNode.Size + ZoneGap));
        var row = (int)Math.Floor((y - GridPadding) / (FieldNode.Size + ZoneGap));

        col = Math.Clamp(col, 0, GridColumns - 1);
        row = Math.Clamp(row, 0, GridRows - 1);

        return (row * GridColumns) + col;
    }

    private (double X, double Y) GetZonePosition(int zoneIndex)
    {
        var row = zoneIndex / GridColumns;
        var col = zoneIndex % GridColumns;

        var x = GridPadding + col * (FieldNode.Size + ZoneGap);
        var y = GridPadding + row * (FieldNode.Size + ZoneGap);

        return (x, y);
    }

    private void ConnectAdjacent(
        IReadOnlyList<FieldNode> group,
        bool isHorizontal)
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
                    BeamThickness,
                    left.Id,
                    right.Id,
                    FieldNodeSide.Right,
                    FieldNodeSide.Left));

                SetBeamStarTarget(left.Id, FieldNodeSide.Right, right.Id, FieldNodeSide.Left);
                SetBeamStarTarget(right.Id, FieldNodeSide.Left, left.Id, FieldNodeSide.Right);
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
                    beamHeight,
                    top.Id,
                    bottom.Id,
                    FieldNodeSide.Bottom,
                    FieldNodeSide.Top));

                SetBeamStarTarget(top.Id, FieldNodeSide.Bottom, bottom.Id, FieldNodeSide.Top);
                SetBeamStarTarget(bottom.Id, FieldNodeSide.Top, top.Id, FieldNodeSide.Bottom);
            }

            first.SetConnectionCount(first.ConnectionCount + 1);
            second.SetConnectionCount(second.ConnectionCount + 1);
        }
    }

    public bool TryPlaceStar(int sourceFieldNodeId, FieldNodeSide sourceSide)
    {
        var slotIndex = _beamStarSlots.FindIndex(slot => slot.SourceFieldNodeId == sourceFieldNodeId && slot.SourceSide == sourceSide && !slot.HasStar);
        if (slotIndex < 0)
            return false;

        _beamStarSlots[slotIndex] = _beamStarSlots[slotIndex] with { HasStar = true };
        return true;
    }

    private void AddPersistentBeamStarSlots(int fieldNodeId)
    {
        foreach (var side in AllSides)
            _beamStarSlots.Add(new BeamStarSlot(fieldNodeId, side));
    }

    private void SetBeamStarTarget(int sourceFieldNodeId, FieldNodeSide sourceSide, int targetFieldNodeId, FieldNodeSide targetSide)
    {
        var slotIndex = _beamStarSlots.FindIndex(slot => slot.SourceFieldNodeId == sourceFieldNodeId && slot.SourceSide == sourceSide);
        if (slotIndex < 0)
            return;

        _beamStarSlots[slotIndex] = _beamStarSlots[slotIndex] with
        {
            TargetFieldNodeId = targetFieldNodeId,
            TargetSide = targetSide
        };
    }

    private double ClampX(double value)
        => Math.Clamp(value, GridPadding, FieldWidth - FieldNode.Size - GridPadding);

    private double ClampY(double value)
        => Math.Clamp(value, GridPadding, FieldHeight - FieldNode.Size - GridPadding);

    private static bool Intersects(double firstX, double firstY, double secondX, double secondY)
    {
        var overlapsX = firstX < secondX + FieldNode.Size - 0.1d && firstX + FieldNode.Size > secondX + 0.1d;
        var overlapsY = firstY < secondY + FieldNode.Size - 0.1d && firstY + FieldNode.Size > secondY + 0.1d;
        return overlapsX && overlapsY;
    }
}
