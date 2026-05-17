using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSixtySeven : ManosabaCardTemplate
{
    private const string SuspectAmountKey = "SuspectAmount";
    private const int RequiredCardsPlayed = 20;

    public CardSixtySeven()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(26m, ValueProp.Move),
        new DynamicVar(SuspectAmountKey, 3m)
    };

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { "Retain" };

    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC)
                return false;

            var cardsPlayed = CombatManager.Instance?.History.Entries
                .OfType<CardPlayFinishedEntry>()
                .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner) ?? 0;

            return cardsPlayed >= RequiredCardsPlayed;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;

        if (target == null) return;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 造成伤害
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 给予嫌疑
        var suspectAmount = source.DynamicVars[SuspectAmountKey].BaseValue;
        await PowerCmd.Apply<SuspectPower>(choiceContext, target, suspectAmount, source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.BaseValue = 52m;
        DynamicVars[SuspectAmountKey].BaseValue = 6m;
    }
}
