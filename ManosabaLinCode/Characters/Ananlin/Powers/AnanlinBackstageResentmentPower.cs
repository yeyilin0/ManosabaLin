using ManosabaLin.Characters.Ananlin.Capabilities;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinBackstageResentmentPower : ManosabaPowerTemplate
{
    private const int SilenceCost = 13;

    private enum AuditionEffect
    {
        GeneratedCard,
        MarginPage,
        IntentRewrite,
        DrawAndMarkCard
    }

    [SavedProperty] public bool FirstAuditionEffectTriggersAgain { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("SilenceCost", SilenceCost)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromCard<BlankPage>(),
        HoverTipFactory.FromCard<MarginPage>()
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var count = Math.Max(0, (int)(Owner.GetPower<AnanlinPeaceOfMindPower>()?.Amount ?? 0));
        if (count <= 0) return;

        Flash();
        var first = RollEffect(player);
        await ExecuteEffect(choiceContext, first);
        if (FirstAuditionEffectTriggersAgain)
            await ExecuteEffect(choiceContext, first);

        for (var i = 1; i < count; i++)
            await ExecuteEffect(choiceContext, RollEffect(player));
    }

    private static AuditionEffect RollEffect(Player player)
    {
        var values = Enum.GetValues<AuditionEffect>();
        return player.RunState.Rng.CombatCardGeneration.NextItem(values);
    }

    private async Task ExecuteEffect(PlayerChoiceContext choiceContext, AuditionEffect effect)
    {
        switch (effect)
        {
            case AuditionEffect.GeneratedCard:
                await AddTemporaryRecordedCard();
                break;
            case AuditionEffect.MarginPage:
                await AddMarginPage(upgraded: true);
                break;
            case AuditionEffect.IntentRewrite:
                await SpendSilenceAndRewrite(choiceContext);
                break;
            case AuditionEffect.DrawAndMarkCard:
                await DrawAndMarkCard(choiceContext);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
        }
    }

    private async Task AddTemporaryRecordedCard()
    {
        if (Owner.Player is not { } player) return;
        if (Owner.CombatState is null) return;

        var sketchbook = player.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        var card = sketchbook?.RollCombatCardFromRecordedPools();
        if (card is null)
        {
            await AddBlankPage(upgraded: false);
            return;
        }

        card.SetFreeIgnoringCardPlayConditions();
        card.AddKeyword(CardKeyword.Exhaust);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
    }

    private async Task AddMarginPage(bool upgraded)
    {
        if (Owner.Player is not { } player) return;
        if (Owner.CombatState is not { } combatState) return;

        var page = combatState.CreateCard<MarginPage>(player);
        if (upgraded)
            CardCmd.Upgrade(page);

        await CardPileCmd.AddGeneratedCardToCombat(page, PileType.Hand, player);
    }

    private async Task AddBlankPage(bool upgraded)
    {
        if (Owner.Player is not { } player) return;
        if (Owner.CombatState is not { } combatState) return;

        var page = combatState.CreateCard<BlankPage>(player);
        if (upgraded)
            CardCmd.Upgrade(page);

        await CardPileCmd.AddGeneratedCardToCombat(page, PileType.Hand, player);
    }

    private async Task SpendSilenceAndRewrite(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player is not { } player) return;

        var sketchbook = player.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        if (sketchbook is null || !sketchbook.CanTriggerSilenceRewrite() || sketchbook.CurrentSilence < SilenceCost)
        {
            await AddBlankPage(upgraded: false);
            return;
        }

        var spent = await sketchbook.SpendSilence(choiceContext, SilenceCost, null);
        if (spent < SilenceCost)
        {
            await AddBlankPage(upgraded: false);
            return;
        }

        await sketchbook.TriggerSilenceRewrite(choiceContext);
    }

    private async Task DrawAndMarkCard(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player is not { } player) return;

        await CardPileCmd.Draw(choiceContext, 1, player);
        var candidates = PileType.Hand.GetPile(player).Cards
            .Where(card => card != null && !card.Keywords.Contains(CardKeyword.Unplayable))
            .ToArray();
        if (candidates.Length == 0) return;

        var selected = player.RunState.Rng.CombatCardSelection.NextItem(candidates);
        selected?.GetOrCreateCapability<AnanlinAuditionPeaceCapability>();
    }
}
