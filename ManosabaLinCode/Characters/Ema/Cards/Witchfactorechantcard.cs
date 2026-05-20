using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Witchfactorechantcard : ManosabaCardTemplate
{
    public Witchfactorechantcard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    private static readonly EnchantmentModel[] TrialEnchants =
    [
        ModelDb.Enchantment<Rebuttal>(),
        ModelDb.Enchantment<Agreement>(),
        ModelDb.Enchantment<Doubt>()
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<Witchfactorechantpower>();
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<EmaWitchFactorPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        // 计算层数 = 当前魔女化 / 50
        var withPower = creature.GetPower<WithPower>();
        var currentWith = withPower?.Amount ?? 0;
        var stacks = (int)(currentWith / 50m);

        if (stacks <= 0) return;

        // 施加能力
        await PowerCmd.Apply<Witchfactorechantpower>(
            choiceContext, creature, stacks, creature, this, false);

        // 给卡组里无附魔卡附魔并减费
        var rng = owner.RunState.Rng.CombatCardSelection;
        var deckPile = PileType.Deck.GetPile(owner);
        var unenchanted = deckPile.Cards.Where(c => c.Enchantment == null).ToList();

        var toEnchant = unenchanted
            .OrderBy(_ => rng.NextFloat())
            .Take(stacks)
            .ToList();

        foreach (var card in toEnchant)
        {
            var enchant = rng.NextItem(TrialEnchants);
            CardCmd.Enchant(enchant.ToMutable(), card, 1m);
            card.EnergyCost.UpgradeBy(-1);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
