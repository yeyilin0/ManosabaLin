using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardSevenTeen : ManosabaCardTemplate
{
    // 记录本回合已经触发过的伪证层数阈值
    private int _lastTriggeredThreshold;

    public CardSevenTeen() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<JusticePower>(1m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<JusticePower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["JusticePower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        if (IsUpgraded) await PlayerCmd.GainEnergy(1m, source.Owner);
    }

    protected override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, // ★ 新增
        PowerModel power,
        decimal amountChanged,
        Creature? applier,
        CardModel? cardSource, ComponentContext componentContext)
    {
        var source = this;

        if (power is not PerjuryPower)
            return;

        if (amountChanged <= 0)
            return;

        if (power.Owner != source.Owner.Creature)
            return;

        if (source.Pile.Type == PileType.Hand)
            return;

        var currentAmount = power.Amount;
        var currentThreshold = currentAmount / 4;

        if (currentThreshold <= source._lastTriggeredThreshold)
            return;

        var triggerCount = currentThreshold - source._lastTriggeredThreshold;
        source._lastTriggeredThreshold = currentThreshold;

        for (var i = 0; i < triggerCount; i++) await CardPileCmd.Add(source, PileType.Hand);
    }

    protected override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        var source = this;
        if (player != source.Owner)
            return;

        source._lastTriggeredThreshold = 0;
        await Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}