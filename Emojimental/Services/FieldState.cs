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
    private readonly Dictionary<int, FieldNode> _fieldObjectsById = new();
    private readonly Dictionary<int, FieldNode> _fieldObjectsByZoneIndex = new();
    private readonly List<AlignmentBeam> _alignmentBeams = new();
    private readonly List<BeamStarSlot> _beamStarSlots = new();
    private readonly Dictionary<(int SourceFieldNodeId, FieldNodeSide SourceSide), int> _beamStarSlotIndices = new();
    private int _nextFieldObjectId = 1;

    public double FieldWidth => (FieldNode.Size * GridColumns) + (ZoneGap * (GridColumns - 1)) + (GridPadding * 2d);
    public double FieldHeight => (FieldNode.Size * GridRows) + (ZoneGap * (GridRows - 1)) + (GridPadding * 2d);

    public IReadOnlyList<FieldNode> FieldObjects => _fieldObjects;
    public IReadOnlyList<AlignmentBeam> AlignmentBeams => _alignmentBeams;
    public IReadOnlyList<BeamStarSlot> BeamStarSlots => _beamStarSlots;
    public FieldNode? SelectedFieldObject { get; private set; }

    public FieldNode? GetFieldObject(int fieldNodeId)
        => _fieldObjectsById.TryGetValue(fieldNodeId, out var fieldObject) ? fieldObject : null;

    public BeamStarSlot? GetBeamStarSlot(int sourceFieldNodeId, FieldNodeSide sourceSide)
        => _beamStarSlotIndices.TryGetValue((sourceFieldNodeId, sourceSide), out var slotIndex)
            ? _beamStarSlots[slotIndex]
            : null;

    public bool HasBeamStarSlot(int sourceFieldNodeId, FieldNodeSide sourceSide)
        => _beamStarSlotIndices.ContainsKey((sourceFieldNodeId, sourceSide));

    public IReadOnlyCollection<int> AddFieldObject(FieldNodeType type)
    {
        if (_fieldObjects.Count >= MaxObjects)
            return Array.Empty<int>();

        var zoneIndex = FindFirstEmptyZone();
        if (zoneIndex == -1)
            return Array.Empty<int>();

        var (x, y) = GetZonePosition(zoneIndex);
        var fieldObject = new FieldNode(_nextFieldObjectId++, type, x, y);
        _fieldObjects.Add(fieldObject);
        _fieldObjectsById[fieldObject.Id] = fieldObject;
        _fieldObjectsByZoneIndex[zoneIndex] = fieldObject;
        AddPersistentBeamStarSlots(fieldObject.Id);
        SelectedFieldObject = fieldObject;
        return RefreshConnections();
    }

    public IReadOnlyCollection<int> RemoveFieldObject(FieldNodeType type)
    {
        var removed = _fieldObjects.LastOrDefault(fieldObject => fieldObject.Type == type);
        if (removed is null)
            return Array.Empty<int>();

        var affectedTargetFieldNodeIds = _beamStarSlots
            .Where(slot => slot.SourceFieldNodeId == removed.Id && slot.Star != null && slot.TargetFieldNodeId != null)
            .Select(slot => slot.TargetFieldNodeId!.Value)
            .ToHashSet();

        _fieldObjects.Remove(removed);
        _fieldObjectsById.Remove(removed.Id);
        _fieldObjectsByZoneIndex.Remove(GetZoneIndex(removed));
        _beamStarSlots.RemoveAll(slot => slot.SourceFieldNodeId == removed.Id);
        RebuildBeamStarSlotIndices();

        if (SelectedFieldObject?.Id == removed.Id)
            SelectedFieldObject = _fieldObjects.Count > 0 ? _fieldObjects[^1] : null;

        foreach (var fieldNodeId in RefreshConnections())
            affectedTargetFieldNodeIds.Add(fieldNodeId);

        return affectedTargetFieldNodeIds.Count == 0 ? Array.Empty<int>() : affectedTargetFieldNodeIds;
    }

    public void SelectFieldObject(int? fieldObjectId)
    {
        if (fieldObjectId is null)
        {
            SelectedFieldObject = null;
            return;
        }

        var fieldObject = GetFieldObject(fieldObjectId.Value);
        if (fieldObject is null || SelectedFieldObject?.Id == fieldObject.Id)
            return;

        SelectedFieldObject = fieldObject;
    }

    public bool SetFieldSize(double width, double height)
        => false;

    public void BeginFieldObjectDrag(int fieldObjectId)
    {
    }

    public void EndFieldObjectDrag(int fieldObjectId)
    {
    }

    public IReadOnlyCollection<int> MoveFieldObject(int fieldObjectId, double x, double y)
    {
        if (!_fieldObjectsById.TryGetValue(fieldObjectId, out var fieldObject))
            return Array.Empty<int>();

        var targetZoneIndex = GetZoneIndexFromPosition(x + FieldNode.Size / 2d, y + FieldNode.Size / 2d);
        var currentZoneIndex = GetZoneIndex(fieldObject);
        if (targetZoneIndex == currentZoneIndex)
            return Array.Empty<int>();

        if (_fieldObjectsByZoneIndex.TryGetValue(targetZoneIndex, out var occupiedFieldObject)
            && occupiedFieldObject.Id != fieldObjectId)
        {
            return Array.Empty<int>();
        }

        var (targetX, targetY) = GetZonePosition(targetZoneIndex);
        _fieldObjectsByZoneIndex.Remove(currentZoneIndex);
        _fieldObjectsByZoneIndex[targetZoneIndex] = fieldObject;
        fieldObject.SetPosition(targetX, targetY);
        return RefreshConnections();
    }

    public IReadOnlyCollection<int> RefreshConnections()
    {
        var previousStarredTargets = new Dictionary<int, int?>();
        for (var index = 0; index < _beamStarSlots.Count; index++)
        {
            if (_beamStarSlots[index].Star != null)
                previousStarredTargets[index] = _beamStarSlots[index].TargetFieldNodeId;
        }

        _alignmentBeams.Clear();
        for (var index = 0; index < _beamStarSlots.Count; index++)
        {
            var slot = _beamStarSlots[index];
            if (slot.TargetFieldNodeId == null && slot.TargetSide == null)
                continue;

            _beamStarSlots[index] = slot with { TargetFieldNodeId = null, TargetSide = null };
        }

        foreach (var fieldObject in _fieldObjects)
            fieldObject.SetConnectionCount(0);

        for (var row = 0; row < GridRows; row++)
        {
            FieldNode? previous = null;
            for (var col = 0; col < GridColumns; col++)
            {
                if (!_fieldObjectsByZoneIndex.TryGetValue((row * GridColumns) + col, out var current))
                    continue;

                if (previous is not null)
                    ConnectAdjacent(previous, current, isHorizontal: true);

                previous = current;
            }
        }

        for (var col = 0; col < GridColumns; col++)
        {
            FieldNode? previous = null;
            for (var row = 0; row < GridRows; row++)
            {
                if (!_fieldObjectsByZoneIndex.TryGetValue((row * GridColumns) + col, out var current))
                    continue;

                if (previous is not null)
                    ConnectAdjacent(previous, current, isHorizontal: false);

                previous = current;
            }
        }

        var affectedTargetFieldNodeIds = new HashSet<int>();
        foreach (var (slotIndex, previousTargetFieldNodeId) in previousStarredTargets)
        {
            var currentTargetFieldNodeId = _beamStarSlots[slotIndex].TargetFieldNodeId;
            if (previousTargetFieldNodeId == currentTargetFieldNodeId)
                continue;

            if (previousTargetFieldNodeId is int previousTarget)
                affectedTargetFieldNodeIds.Add(previousTarget);

            if (currentTargetFieldNodeId is int currentTarget)
                affectedTargetFieldNodeIds.Add(currentTarget);
        }

        return affectedTargetFieldNodeIds.Count == 0 ? Array.Empty<int>() : affectedTargetFieldNodeIds;
    }

    private int FindFirstEmptyZone()
    {
        for (var i = 0; i < MaxObjects; i++)
        {
            if (!_fieldObjectsByZoneIndex.ContainsKey(i))
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

    private void ConnectAdjacent(FieldNode first, FieldNode second, bool isHorizontal)
    {
        if (isHorizontal)
        {
            var left = first.X <= second.X ? first : second;
            var right = ReferenceEquals(left, first) ? second : first;
            var beamLeft = left.X + FieldNode.Size;
            var beamWidth = right.X - beamLeft;
            if (beamWidth <= 0d)
                return;

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
                return;

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

    public bool TryPlaceStar(int sourceFieldNodeId, FieldNodeSide sourceSide, Star star)
    {
        if (!_beamStarSlotIndices.TryGetValue((sourceFieldNodeId, sourceSide), out var slotIndex))
            return false;

        _beamStarSlots[slotIndex] = _beamStarSlots[slotIndex] with { Star = star };
        return true;
    }

    private void AddPersistentBeamStarSlots(int fieldNodeId)
    {
        foreach (var side in AllSides)
        {
            _beamStarSlots.Add(new BeamStarSlot(fieldNodeId, side));
            _beamStarSlotIndices[(fieldNodeId, side)] = _beamStarSlots.Count - 1;
        }
    }

    private void SetBeamStarTarget(int sourceFieldNodeId, FieldNodeSide sourceSide, int targetFieldNodeId, FieldNodeSide targetSide)
    {
        if (!_beamStarSlotIndices.TryGetValue((sourceFieldNodeId, sourceSide), out var slotIndex))
            return;

        _beamStarSlots[slotIndex] = _beamStarSlots[slotIndex] with
        {
            TargetFieldNodeId = targetFieldNodeId,
            TargetSide = targetSide
        };
    }

    private void RebuildBeamStarSlotIndices()
    {
        _beamStarSlotIndices.Clear();
        for (var index = 0; index < _beamStarSlots.Count; index++)
        {
            var slot = _beamStarSlots[index];
            _beamStarSlotIndices[(slot.SourceFieldNodeId, slot.SourceSide)] = index;
        }
    }

    private int GetZoneIndex(FieldNode fieldNode)
        => GetZoneIndexFromPosition(fieldNode.X + FieldNode.Size / 2d, fieldNode.Y + FieldNode.Size / 2d);
}
