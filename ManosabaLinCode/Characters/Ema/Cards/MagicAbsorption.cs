using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class MagicAbsorption : ManosabaCardTemplate
{
    private const int EnergyCost = 3;
    private const CardType Type = CardType.Skill;
    private const CardRarity Rarity = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.AnyEnemy;

    private const int WithReduction = 30;
    private const int ShieldAmount = 20;
    private const int NyxmStacks = 1;

    public MagicAbsorption() : base(EnergyCost, Type, Rarity, CardTarget)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Ethereal;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<NyxmPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>("WithReduction", WithReduction),
        new IntVar("Shield", ShieldAmount),
        new PowerVar<YlsmPower>("NyxmStacks", NyxmStacks)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        if (target == null) return;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var targetWithPower = target.GetPower<WithPower>();
        if (targetWithPower != null)
        {
            var reductionAmount = System.Math.Min(
                source.DynamicVars["WithReduction"].IntValue,
                (int)targetWithPower.Amount);
            if (reductionAmount > 0)
                await PowerCmd.ModifyAmount(
                    choiceContext, targetWithPower,
                    -reductionAmount,
                    source.Owner.Creature,
                    source,
                    false
                );
        }

        await CreatureCmd.GainBlock(
            source.Owner.Creature,
            source.DynamicVars["Shield"].BaseValue,
            ValueProp.Move,
            cardPlay
        );

        await PowerCmd.Apply<NyxmPower>(
            choiceContext,
            source.Owner.Creature,
            source.DynamicVars["NyxmStacks"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["WithReduction"].UpgradeValueBy(30m);
        DynamicVars["Shield"].UpgradeValueBy(10m);
        RemoveKeyword(CardKeyword.Ethereal);
    }
}