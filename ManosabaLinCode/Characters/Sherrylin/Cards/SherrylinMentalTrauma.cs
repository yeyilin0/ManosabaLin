using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
[RegisterCharacterStarterCard(typeof(Sherrylin))]
[RegisterArchaicToothTranscendence(typeof(TheFool))]
public class SherrylinMentalTrauma : ManosabaCardTemplate
{
    public SherrylinMentalTrauma() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<EmotionPower>(
            choiceContext, Owner.Creature, 1,
            Owner.Creature, this, false);

        if (IsUpgraded)
        {
            var drawPile = PileType.Draw.GetPile(Owner);
            if (drawPile.Cards.Any())
            {
                var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 1);
                var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, drawPile.Cards, Owner, prefs);
                var card = selected.FirstOrDefault();
                if (card != null)
                    await CardCmd.Exhaust(choiceContext, card);  // 替换原来的 CardPileCmd.Add(card, PileType.Exhaust)
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 用户 2026-10-15 指令：升级后获得固有。
        // 与原版 Afterimage / 同池 SuperStrength 一致 —— 必须走 AddKeyword 的实时写入：
        // _keywords 是首次读取时从 CanonicalKeywords 快照下来的实例状态（且随克隆传播），
        // 用 CanonicalKeywords + IsUpgraded 的写法在「已经读过关键字后再升级」时会被快照吞掉。
        AddKeyword(CardKeyword.Innate);
    }
}
