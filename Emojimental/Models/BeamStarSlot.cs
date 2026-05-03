namespace Emojimental.Models;

public sealed record BeamStarSlot(
    int SourceFieldNodeId,
    FieldNodeSide SourceSide,
    int? TargetFieldNodeId = null,
    FieldNodeSide? TargetSide = null,
    Star? Star = null)
{
    public bool HasStar => Star != null;
}
