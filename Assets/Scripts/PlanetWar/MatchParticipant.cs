using System;
using UnityEngine;

[Serializable]
public sealed class MatchParticipant
{
    [SerializeField] private Sprite avatar;
    [SerializeField] private string displayName;
    [SerializeField] private string rank;
    [SerializeField] private string score;

    public Sprite Avatar => avatar;
    public string DisplayName => displayName;
    public string Rank => rank;
    public string Score => score;

    public MatchParticipant(string displayName, string rank, string score, Sprite avatar = null)
    {
        this.avatar = avatar;
        this.displayName = displayName;
        this.rank = rank;
        this.score = score;
    }
}
