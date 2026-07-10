using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ananlin;
using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSeven() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<AnanlinTwo>(IsUpgraded); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new CardsVar(3)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;
        var targetPlayer = target.Player ?? source.Owner;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        for (var i = 0; i < source.DynamicVars.Cards.IntValue; i++)
        {
            var twoCard = source.CombatState.CreateCard<AnanlinTwo>(targetPlayer);
            if (IsUpgraded) twoCard.UpgradeInternal();
            await CardPileCmd.Add(twoCard, PileType.Draw);
            await Cmd.Wait(0.05f);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}