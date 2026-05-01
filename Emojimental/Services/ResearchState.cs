using Emojimental.Models;

namespace Emojimental.Services;

public sealed class ResearchState
{
    private readonly HashSet<ResearchType> _ownedResearch = [];

    public IReadOnlyCollection<ResearchType> OwnedResearch => _ownedResearch;

    public bool HasResearch(ResearchType type) => _ownedResearch.Contains(type);

    public void AddResearch(ResearchType type) => _ownedResearch.Add(type);
}
