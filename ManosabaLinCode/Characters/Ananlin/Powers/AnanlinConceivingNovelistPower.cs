using ManosabaLin.Characters.Ananlin.Capabilities;
using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinConceivingNovelistPower : ManosabaPowerTemplate
{
    private readonly HashSet<ulong> _copiedPlayersThisTurn = [];

    [SavedProperty] public bool FreeCopies { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature.Side == Owner.Side)
            _copiedPlayersThisTurn.Remove(player.NetId);

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner.Player
            && cardPlay.Card.TryGetCapability<AnanlinNovelistCopyCapability>(out _))
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, cardPlay.Card);
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, Amount, Owner, cardPlay.Card);
            return;
        }

        if (!ShouldCopy(cardPlay.Card)) return;

        _copiedPlayersThisTurn.Add(cardPlay.Card.Owner.NetId);
        await AddCopyToHand(choiceContext, cardPlay.Card);
    }

    private bool ShouldCopy(CardModel card)
    {
        if (card.Owner is not { } player) return false;
        if (player == Owner.Player) return false;
        if (player.Creature.Side != Owner.Side || !player.Creature.IsAlive) return false;
        if (card.Rarity != CardRarity.Rare) return false;
        return !_copiedPlayersThisTurn.Contains(player.NetId);
    }

    private async Task AddCopyToHand(PlayerChoiceContext choiceContext, CardModel original)
    {
        if (Owner.Player is not { } player || CombatState is not { } combatState) return;
        if (original.CanonicalInstance is not { } canonical) return;

        Flash();
        var copy = combatState.CreateCard(canonical, player);
        AnanlinCardHelpers.CopyUpgradeLevel(original, copy);
        copy.EnergyCost.SetThisCombat(FreeCopies ? 0 : 1);
        CardCmd.ApplyKeyword(copy, CardKeyword.Exhaust);
        copy.GetOrCreateCapability<AnanlinNovelistCopyCapability>();

        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);
    }
}
