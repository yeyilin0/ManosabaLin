using ManosabaLin.Characters.Sherrylin.Capabilities;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class ZeroEnergyGrab() : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardCount", 2m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var cardCount = source.DynamicVars["CardCount"].IntValue;
        var combatState = source.CombatState;
        if (combatState == null)
            return;

        var rng = source.Owner.RunState.Rng.CombatCardSelection;

        var allPools = source.Owner.UnlockState.CharacterCardPools;
        var zeroCostCards = allPools
            .SelectMany(p => p.AllCards)
            .Where(c => c.EnergyCost.Canonical == 0 && c.Type != CardType.Curse && c.Type != CardType.Status)
            .Distinct()
            .ToList();

        for (int i = 0; i < cardCount && zeroCostCards.Count > 0; i++)
        {
            var idx = rng.NextInt(zeroCostCards.Count);
            var cardModel = zeroCostCards[idx];
            var newCard = combatState.CreateCard(cardModel, source.Owner);
            newCard.GetOrCreateCapability<RemoveOnPlayCapability>();
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, source.Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["CardCount"].UpgradeValueBy(1m);
    }
}
