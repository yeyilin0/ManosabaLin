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

/// <summary>小钥匙 - 0费技能, 疑问附魔, 抽1张, 下一张疑问牌费用-1, 升级抽2张</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class SmallKey : ManosabaEmalinCardTemplate
{
    public SmallKey() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.DoubtKeywordId.GetModCardKeyword() };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        var doubtCard = PileType.Hand.GetPile(Owner).Cards
            .FirstOrDefault(c => c != this && c.Enchantment is Doubt && c.CanPlay());
        if (doubtCard != null)
            doubtCard.SetToFreeThisTurn();
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}