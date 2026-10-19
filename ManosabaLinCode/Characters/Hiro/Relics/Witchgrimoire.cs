using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using AnanlinCharacter = ManosabaLin.Characters.Ananlin.Ananlin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Witchgrimoire : ManosabaRelicTemplate
{
    public const int CardCount = 39;

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
        var emaPool = ModelDb.GetById<CharacterModel>(new ModelId("CHARACTER", "MANOSABA_LIN_CHARACTER_EMALIN")).CardPool;
        var sherrylinPool = ModelDb.GetById<CharacterModel>(new ModelId("CHARACTER", "MANOSABA_LIN_CHARACTER_SHERRYLIN")).CardPool;
        var ananlinPool = ModelDb.Character<AnanlinCharacter>().CardPool;

        // 只从「角色卡牌图鉴」内可见的卡里 roll：衍生卡（Token / 选项牌等 shouldShowInCardLibrary=false，
        // 如「接受」「被称为冲击波的拳风」）不是正式构筑卡，不应被本遗物这类随机发卡效果发出。
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
                new CardPoolModel[] { hiroPool, emaPool, sherrylinPool, ananlinPool },
                card => card.ShouldShowInCardLibrary)
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
