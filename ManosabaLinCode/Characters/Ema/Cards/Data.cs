using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>资料 - 0费技能, 疑问附魔, 抽2张, 本回合所有疑问牌费用-1</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class Data : ManosabaEmalinCardTemplate
{
    public Data() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.DoubtKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        foreach (var card in PileType.Hand.GetPile(Owner).Cards)
        {
            if (card.Enchantment is Doubt && card.CanPlay())
                card.SetToFreeThisTurn();
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}