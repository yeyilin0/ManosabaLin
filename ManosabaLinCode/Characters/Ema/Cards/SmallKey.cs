using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class SmallKey : ManosabaCardTemplate
{
    public SmallKey() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.DoubtCardKeyword };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        // 卡面（本地化=规范）：「手牌中**第一张**带【审判】附魔的牌费用-1」——只按手牌顺序取第一张，
        // 没有任何「要付得起」的前提。⚠️ 绝不能用 CanPlay() 过滤：它把「能量够不够」也算进来，
        // 而能量在多人下是瞬时变化的 ⇒ 两个对端会选中不同的牌，一端把某张牌改成 0 费、另一端没改，
        // 卡片费用进校验和 ⇒ 直接 StateDivergence（2026-10-15 队友被主机踢下线就是这个形状）。
        var trialCard = PileType.Hand.GetPile(Owner).Cards
            .FirstOrDefault(c => c != this
                                 && c.Enchantment is Rebuttal or Agreement or Doubt);
        if (trialCard != null)
            trialCard.EnergyCost.UpgradeBy(-1);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
