using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Extensions;
using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmotionHelplessness() : CaseFileCard<EmotionHelplessnessOrb>(-1, CardRarity.Ancient, TargetType.AnyEnemy)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RetainCounterComponent(), new UniqueComponent()];

    protected override bool HasTurnEndInHandEffectC => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>("Strength", 2m),
        new PowerVar<DexterityPower>("Dexterity", 2m),
        new DamageVar("Damage", 5, ValueProp.Move),
        new BlockVar("Block", 5m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromOrb<EmotionHelplessnessOrb>();
        }
    }

    protected override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        if (Owner != player) return;

        EnergyCost.AddThisCombat(1);

        if (EnergyCost.Canonical >= 4 && this is IComponentsCardModel ccm && !ccm.HasComponent<LevitationComponent>())
        {
            ccm.AddComponent(new LevitationComponent());
        }
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext, ComponentContext componentContext)
    {
        var retainCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.HasComponent<RetainCounterComponent>())
            .ToList();

        if (retainCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var target = retainCards[rng.NextInt(retainCards.Count)];

        if (target is IComponentsCardModel ccm)
        {
            var comp = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
            if (comp != null)
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var counterField = typeof(RetainCounterComponent).GetField("_counter", flags);
                if (counterField != null)
                {
                    var current = (int)counterField.GetValue(comp);
                    counterField.SetValue(comp, current + 1);
                }
            }
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        int counter = 1;
        if (source is IComponentsCardModel ccm)
        {
            var comp = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
            if (comp != null)
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var counterField = typeof(RetainCounterComponent).GetField("_counter", flags);
                if (counterField != null)
                    counter = (int)counterField.GetValue(comp);
            }
        }

        await PowerCmd.Apply<StrengthPower>(
            choiceContext, source.Owner.Creature,
            counter * source.DynamicVars["StrengthPower"].BaseValue,
            source.Owner.Creature, source, false);

        await PowerCmd.Apply<DexterityPower>(
            choiceContext, source.Owner.Creature,
            -(counter * (int)source.DynamicVars["Dexterity"].BaseValue),
            source.Owner.Creature, source, false);

        var combatState = source.CombatState;
        if (combatState != null)
        {
            var enemies = combatState.HittableEnemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var target = enemies[rng.NextInt(enemies.Count)];
                await CreatureCmd.Damage(choiceContext, target,
                    counter * source.DynamicVars.Damage.BaseValue,
                    ValueProp.Move, source.Owner.Creature, source);
            }
        }

        await CreatureCmd.GainBlock(source.Owner.Creature,
            counter * source.DynamicVars.Block.BaseValue,
            ValueProp.Move, cardPlay);

        await base.OnPlay(choiceContext, cardPlay, componentContext);
    }
}