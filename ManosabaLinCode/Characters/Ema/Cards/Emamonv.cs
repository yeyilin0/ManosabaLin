using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
[RegisterCharacterStarterCard(typeof(Emalin.Emalin))]
public sealed class Emamonv : ManosabaCardTemplate

{
    public Emamonv() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromCard<Emamqinjin>();
            yield return HoverTipFactory.FromCard<Emamshuyuan>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var cardQinjin = CombatState.CreateCard<Emamqinjin>(owner);
        var cardShuyuan = CombatState.CreateCard<Emamshuyuan>(owner);

        var options = new List<CardModel> { cardQinjin, cardShuyuan };
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, options, owner, prefs);

        // ⚠️ 挂起后战斗可能已结束/玩家已死亡：引擎文档明示「an effect can be suspended past the end of
        // its own combat」（出牌效果体内的 await 会被挂过战斗结束），CardCmd.AutoPlay 内部就有同款守卫
        // （IsOverOrEnding || Owner.Creature.IsDead）会静默跳过。联机中队友掉线/分歧中止会话时会整体
        // 拆除战斗状态 ⇒ player.Creature.CombatState 变 null，此时 GetDistinctForCombat 的
        // `player.Creature.CombatState.CreateCard` 直接 NRE，把出牌流程整个炸掉。
        // （2026-10-16 godot (13).log：队友选完牌后会话中止，本卡 OnPlay 恢复执行即 NRE。）
        if (CombatManager.Instance.IsOverOrEnding || owner.Creature.IsDead)
            return;

        var chosen = selected.FirstOrDefault();

        if (chosen == null) return;

        await CardCmd.AutoPlay(choiceContext, chosen, null);

        if (CombatManager.Instance.IsOverOrEnding || owner.Creature.IsDead)
            return;

        var keywords = new[]
        {
            EmalinKeywordRules.AgreeCardKeyword,
            EmalinKeywordRules.DoubtCardKeyword,
            EmalinKeywordRules.RebuttalCardKeyword
        };

        var cards = CardFactory.GetDistinctForCombat(
            owner,
            owner.Character.CardPool
                .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
                .Where(c => keywords.Any(k => c.HasModKeyword(k))),
            3,
            owner.RunState.Rng.CombatCardGeneration
        ).ToList();

        if (cards.Count == 0) return;

        foreach (var card in cards)
        {
            if (card.Enchantment != null) continue;

            card.AddKeyword(CardKeyword.Ethereal);

            if (EmalinKeywordRules.HasAgreeKeyword(card))
                CardCmd.Enchant(ModelDb.Enchantment<Agreement>().ToMutable(), card, 1m);
            else if (EmalinKeywordRules.HasRebuttalKeyword(card))
                CardCmd.Enchant(ModelDb.Enchantment<Rebuttal>().ToMutable(), card, 1m);
            else if (EmalinKeywordRules.HasDoubtKeyword(card))
                CardCmd.Enchant(ModelDb.Enchantment<Doubt>().ToMutable(), card, 1m);
        }

        var pickPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var pickResult = await CardSelectCmd.FromSimpleGrid(choiceContext, cards, owner, pickPrefs);

        if (CombatManager.Instance.IsOverOrEnding || owner.Creature.IsDead)
            return;

        var picked = pickResult.FirstOrDefault();

        if (picked == null) return;

        // 可以免费打出一次：本回合费用为 0，或保留至该牌被打出为止
        picked.SetToFreeThisTurn();

        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
    }
    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}