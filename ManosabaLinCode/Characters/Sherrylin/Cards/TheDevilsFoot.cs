using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheDevilsFoot() : ManosabaCardTemplate(13, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Multiplier", 2)
    };

    protected override Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal, ComponentContext componentContext)
    {
        if (card == this) return Task.CompletedTask;
        if (Pile?.Type != PileType.Hand) return Task.CompletedTask;
        EnergyCost.AddThisCombat(-1);
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var multiplier = (int)source.DynamicVars["Multiplier"].BaseValue;

        var exhaustCount = PileType.Exhaust.GetPile(Owner).Cards.Count;

        var damage = exhaustCount * multiplier;
        var block = exhaustCount * multiplier;

        if (damage > 0)
        {
            var enemies = CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
            foreach (var enemy in enemies)
            {
                await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Move, source, cardPlay);
            }
        }

        if (block > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
        }

        // 恢复全部能量至上限
        var maxEnergy = Owner.PlayerCombatState.MaxEnergy;
        var currentEnergy = Owner.PlayerCombatState.Energy;
        var energyToGain = maxEnergy - currentEnergy;
        if (energyToGain > 0)
            await PlayerCmd.GainEnergy(energyToGain, Owner);

        // 打出后重置费用
        EnergyCost.SetThisCombat(13);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Retain);
        DynamicVars["Multiplier"].UpgradeValueBy(1m);
    }
}
