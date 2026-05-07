using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Seven() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<Two>(IsUpgraded); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new CardsVar(3)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        for (var i = 0; i < source.DynamicVars.Cards.IntValue; i++)
        {
            var twoCard = source.CombatState.CreateCard<Two>(target.Player);
            if (IsUpgraded) twoCard.UpgradeInternal();
            await CardPileCmd.AddGeneratedCardToCombat(twoCard, PileType.Draw, target.Player);
            await Cmd.Wait(0.05f);
        }
    }

    protected override void OnUpgrade()
    {
    }
}