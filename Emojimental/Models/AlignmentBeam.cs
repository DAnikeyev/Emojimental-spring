namespace Emojimental.Models;

public sealed record AlignmentBeam(
    bool IsHorizontal,
    double Left,
    double Top,
    double Width,
    double Height,
    int SourceFieldNodeId,
    int TargetFieldNodeId,
    FieldNodeSide SourceSide,
    FieldNodeSide TargetSide);

