using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmaBadEnding() : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, DamageProps.card)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override int MaxUpgradeLevel => 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<EmaBadEndingPower>(choiceContext, CombatState!.Enemies, 1, Owner.Creature, this);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        await PowerCmd.Apply<EmaBadEndingRewardPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }
}
