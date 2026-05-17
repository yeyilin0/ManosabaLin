using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
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
public sealed class Emamonv : ManosabaEmalinCardTemplate

{
    public Emamonv() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromCard<Emamonvqinjin>();
            yield return HoverTipFactory.FromCard<Emamonvshuyuan>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var cardQinjin = CombatState.CreateCard<Emamonvqinjin>(owner);
        var cardShuyuan = CombatState.CreateCard<Emamonvshuyuan>(owner);

        var options = new List<CardModel> { cardQinjin, cardShuyuan };
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, options, owner, prefs);
        var chosen = selected.FirstOrDefault();

        if (chosen == null) return;

        await CardCmd.AutoPlay(choiceContext, chosen, null);

        var keywords = new[]
        {
            EmalinKeywordRules.AgreeKeywordId,
            EmalinKeywordRules.DoubtKeywordId,
            EmalinKeywordRules.RebuttalKeywordId
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

            // 加上虚无关键词
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
        var picked = pickResult.FirstOrDefault();

        if (picked != null)
            await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}