using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheValleyOfFear() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(1, ValueProp.Unblockable | ValueProp.Unpowered)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        var count = handCards.Count;

        foreach (var card in handCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        await CardPileCmd.Draw(choiceContext, count, Owner);

        var enemies = CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
        var rng = Owner.RunState.Rng.CombatTargets;

        for (var i = 0; i < count; i++)
        {
            if (enemies.Count == 0) break;
            var randomEnemy = rng.NextItem(enemies);

            await CreatureCmd.Damage(choiceContext, randomEnemy, DynamicVars.Damage.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, source, cardPlay);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
