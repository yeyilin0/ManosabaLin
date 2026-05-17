using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using ManosabaLin.Characters.Hiro;

namespace ManosabaLin.ManosabaLinCode.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class Lym : ManosabaCardTemplate
{
    private const int BaseEnergyCost = 2;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.AnyEnemy;

    public Lym() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<LymPower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var markedEnemy = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(markedEnemy);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature, 5,
            source.Owner.Creature, source, false);

        await PowerCmd.Apply<LymPower>(
            choiceContext, markedEnemy, 1,
            source.Owner.Creature, source, false);

        var redirectPower = markedEnemy.Powers.OfType<LymPower>().FirstOrDefault();
        if (redirectPower is not null)
            await redirectPower.ChooseMoveTarget(choiceContext, source.Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}