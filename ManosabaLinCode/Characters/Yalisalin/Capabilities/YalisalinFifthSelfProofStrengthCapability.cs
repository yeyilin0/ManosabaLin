using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Capabilities;

[RegisterModelCapability]
public sealed class YalisalinFifthSelfProofStrengthCapability : ManosabaCardCapability
{
    [SavedProperty] public int ExtraAttackHits { get; private set; }
    [SavedProperty] public int ExtraSkillPlays { get; private set; }

    public void Add(CardType type)
    {
        switch (type)
        {
            case CardType.Attack:
                ExtraAttackHits++;
                break;
            case CardType.Skill:
                ExtraSkillPlays++;
                break;
        }
    }

    public override int ModifyAttackHitCount(AttackCommand attack, int hitCount)
    {
        return attack.ModelSource == Owner && ExtraAttackHits > 0
            ? hitCount + ExtraAttackHits
            : hitCount;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        return card == Owner && ExtraSkillPlays > 0
            ? playCount + ExtraSkillPlays
            : playCount;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == Owner && cardPlay.IsLastInSeries)
            RemoveFromOwner();

        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        RemoveFromOwner();
        return Task.CompletedTask;
    }
}
