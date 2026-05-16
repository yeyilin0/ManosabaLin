using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class StabbingBlade : ManosabaEmalinCardTemplate
{
    public StabbingBlade() : base(2, CardType.Attack, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            foreach (var tip in HoverTipFactory.FromEnchantment<Witchification>())
                yield return tip;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        var attackCards = PileType.Hand.GetPile(owner).Cards
            .Where(c => c.Type == CardType.Attack && c.Enchantment == null)
            .ToList();

        if (attackCards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(
                new LocString("StabbingBlade", "选择一张攻击卡附魔魔女化"), 1, 1);
            var selected = await CardSelectCmd.FromHand(
                choiceContext, owner, prefs,
                c => c.Type == CardType.Attack && c.Enchantment == null, this);

            var targetCard = selected.FirstOrDefault();
            if (targetCard != null)
            {
                targetCard.SetToFreeThisTurn();

                CardCmd.Enchant(ModelDb.Enchantment<Witchification>().ToMutable(), targetCard, 1m);

                if (bond != null && bond.Estrangement > bond.Affinity)
                {
                    var enemies = CombatState.Enemies.Where(e => e is { IsAlive: true }).ToList();
                    if (enemies.Count > 0)
                    {
                        var rng = owner.RunState.Rng.CombatTargets;
                        var enemy = rng.NextItem(enemies);
                        await CardCmd.AutoPlay(choiceContext, targetCard, enemy);
                    }
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}