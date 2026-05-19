using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using System;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Xueqinjincard2 : ManosabaCardTemplate
{
    public Xueqinjincard2() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Ethereal; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;

        var allAffinityCards = new[]
        {
            typeof(SwapBodySuccess),
            typeof(GuardianOath),
            typeof(SharedFate),
            typeof(DollGift),
            typeof(TheOnlyClue),
            typeof(SubstituteCost),
            typeof(NoahAffinity),
            typeof(MargaretAffinity),
            typeof(CocoAffinity),
            typeof(AnnAffinity),
        };

        var rng = owner.RunState.Rng.CombatCardSelection;
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", new Type[] { typeof(Player) });

        var generatedCards = allAffinityCards
            .OrderBy(_ => rng.NextFloat())
            .Take(3)
            .Select(type =>
            {
                var genericMethod = createCardMethod.MakeGenericMethod(type);
                var card = (CardModel)genericMethod.Invoke(source.CombatState, new object[] { owner });
                card.SetToFreeThisTurn();
                return card;
            })
            .ToList();

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, generatedCards, owner, prefs);
        var picked = selected.FirstOrDefault();

        if (picked != null)
            await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
    }
}
