using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Witchgrimoire : ManosabaRelicTemplate
{
    public const int CardCount = 13;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new[]
            {
                new CardsVar(CardCount)
            };
        }
    }

    public override async Task AfterObtained()
    {
        var relic = this;
        var hiroPool = ModelDb.Character<Hiro>().CardPool;

        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { hiroPool })
            .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);

        var cards = CardFactory.CreateForReward(relic.Owner, CardCount, options).ToList();

        var prefs = new CardSelectorPrefs(
            relic.SelectionScreenPrompt,
            0,
            cards.Count);

        var selected = await CardSelectCmd.FromSimpleGridForRewards(
            new BlockingPlayerChoiceContext(),
            cards,
            relic.Owner,
            prefs);

        foreach (var card in selected)
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }
}
