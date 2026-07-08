using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Sherrylin;
using ManosabaLin.Settings;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Hiro.Events;

[RegisterActEvent(typeof(Hive))]
public sealed class TeamCardExchangeEvent : ModEventTemplate
{
    private const int CardsToChooseFrom = 8;
    private const int CardsToPick = 3;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://ManosabaLin/images/events/teamcardexchangeevent.png"
    );

    public override bool IsShared => true;

    public override bool IsAllowed(IRunState state)
    {
        if (!EventSettingsService.IsTeamCardExchangeEventEnabled)
            return false;

        return base.IsAllowed(state);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var canShareCards = Owner != null && CanUseSharedPoolOption(Owner.RunState);

        return
        [
            new EventOption(this, ChooseOwnCards, InitialOptionKey("OWN_CARDS")),
            canShareCards
                ? new EventOption(this, ChooseTeammateCards, InitialOptionKey("TEAMMATE_CARDS"))
                : new EventOption(this, null, InitialOptionKey("TEAMMATE_CARDS_LOCKED"))
        ];
    }

    private async Task ChooseOwnCards()
    {
        if (Owner == null) return;

        await ChooseCardsFromPool(
            Owner.Character.CardPool,
            card => card.Rarity is CardRarity.Common or CardRarity.Uncommon,
            "OWN_CARDS");
    }

    private async Task ChooseTeammateCards()
    {
        var owner = Owner;
        if (owner == null) return;

        var teammatePools = owner.RunState.Players
            .Where(player => player.Character.CardPool.Id != owner.Character.CardPool.Id)
            .Select(player => player.Character.CardPool)
            .ToList();

        var targetPool = Rng.NextItem(teammatePools);
        if (targetPool == null)
        {
            SetEventFinished(PageDescription("TEAMMATE_CARDS_EMPTY"));
            return;
        }

        await ChooseCardsFromPool(
            targetPool,
            card => card.Rarity is CardRarity.Uncommon or CardRarity.Rare,
            "TEAMMATE_CARDS");
    }

    private async Task ChooseCardsFromPool(
        CardPoolModel cardPool,
        Func<CardModel, bool> filter,
        string resultPageKey)
    {
        var owner = Owner;
        if (owner == null) return;

        var options = CardCreationOptions
            .ForNonCombatWithUniformOdds([cardPool], filter)
            .WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);

        var cards = CardFactory.CreateForReward(owner, CardsToChooseFrom, options).ToList();
        var prefs = new CardSelectorPrefs(
            L10NLookup($"{Id.Entry}.pages.{resultPageKey}.selectionScreenPrompt"),
            0,
            CardsToPick)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        await SelectCardsToAddToDeckFromGrid(cards, prefs);
        SetEventFinished(PageDescription(resultPageKey));
    }

    private static bool CanUseSharedPoolOption(IRunState runState)
    {
        var players = runState.Players;
        var specialCount = players.Count(IsManosabaMainCharacter);

        return specialCount > 0 && specialCount < players.Count;
    }

    private static bool IsManosabaMainCharacter(Player player)
    {
        var cardPool = player.Character.CardPool;
        return cardPool is HiroCardPool or EmalinCardPool or SherrylinCardPool;
    }
}
