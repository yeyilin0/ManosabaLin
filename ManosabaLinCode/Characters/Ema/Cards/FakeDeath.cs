using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using ManosabaLin.Characters.Emalin;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>假死的真相 - 2费技能, 抽2, ≥3种附魔牌则抽4+队友回2血+全体2能量, 升级1费</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class FakeDeath : ManosabaEmalinCardTemplate
{
    public FakeDeath() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var distinctTypes = EmalinCombatHelper.GetDistinctEnchantmentTypesThisTurn(Owner.Creature, CombatState);

        if (distinctTypes >= 3)
        {
            await CardPileCmd.Draw(choiceContext, 4m, Owner);
            foreach (var ally in CombatState.Allies.Where(a => a is { IsAlive: true } && a != Owner.Creature))
                await CreatureCmd.Heal(ally, 2m);
            foreach (var player in CombatState.Players)
                await PlayerCmd.GainEnergy(2m, player);
        }
        else
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}