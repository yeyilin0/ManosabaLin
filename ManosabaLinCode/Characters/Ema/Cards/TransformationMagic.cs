using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class TransformationMagic : ManosabaCardTemplate
{
    public TransformationMagic() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Retain; }
    }

    protected override IEnumerable<ICardComponent> CanonicalComponents => [new UniqueComponent()];
    protected override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        var source = this;
        if (player != source.Owner) return;

        // 只在手牌中才触发
        if (source.Pile?.Type != PileType.Hand) return;

        var handCards = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c != this && c.CanPlay())
            .ToList();

        if (handCards.Count == 0) return;

        // 选择一张手牌
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, 1);
        var selected = await CardSelectCmd.FromHand(choiceContext, source.Owner, prefs, null, this);
        var target = selected.FirstOrDefault();

        if (target == null) return;

        // 变形为选择的卡牌
        var newCard = CombatState.CreateCard(target.CanonicalInstance, source.Owner);

        // 复制升级等级
        for (int i = 0; i < target.CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(newCard);

        // 变形
        await CardCmd.Transform(source, newCard);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
