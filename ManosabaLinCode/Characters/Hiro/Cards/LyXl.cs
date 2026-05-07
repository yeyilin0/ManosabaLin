using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class LyXl() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Ancient, TargetType.AnyPlayer)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Retain; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<Powerthreethree>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await PowerCmd.Apply<WithPower>(
            choiceContext, target, 100,
            source.Owner.Creature, source, false
        );

        await PowerCmd.Apply<Powerthreethree>(
            choiceContext, target, 1,
            source.Owner.Creature, source, false
        );

        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext, target, 1,
            source.Owner.Creature, source, false
        );

        await CreatureCmd.Damage(
            choiceContext,
            source.Owner.Creature,
            999m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null
        );
    }
}