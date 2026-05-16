using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class ChainedTrust : ManosabaEmalinCardTemplate
{
    public ChainedTrust() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<NyxmPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("NyxmStacks", 1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        await PowerCmd.Apply<NyxmPower>(
            choiceContext, creature, DynamicVars["NyxmStacks"].BaseValue, creature, this, false);

        var discardPile = PileType.Discard.GetPile(owner);
        if (discardPile.Cards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(
                new LocString("ChainedTrust", "选择1张卡回收"), 1, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext, discardPile.Cards, owner, prefs);
            var retrieved = selected.FirstOrDefault();
            if (retrieved != null)
                await CardPileCmd.Add(retrieved, PileType.Hand);
        }

        if (bond != null && bond.Estrangement > bond.Affinity)
        {
            var drawPile = PileType.Draw.GetPile(owner);
            var attackCards = drawPile.Cards
                .Where(c => c.Type == CardType.Attack)
                .ToList();

            if (attackCards.Count > 0)
            {
                var rng = owner.RunState.Rng.CombatCardSelection;
                var randomAttack = rng.NextItem(attackCards);

                var enemies = CombatState.Enemies.Where(e => e is { IsAlive: true }).ToList();
                var target = enemies.Count > 0
                    ? owner.RunState.Rng.CombatTargets.NextItem(enemies)
                    : null;

                await CardCmd.AutoPlay(choiceContext, randomAttack, target);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["NyxmStacks"].UpgradeValueBy(1);
    }
}