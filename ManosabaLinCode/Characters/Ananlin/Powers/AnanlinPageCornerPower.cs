using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinPageCornerPower : ManosabaPowerTemplate
{
    private const int EnergyThreshold = 4;

    [SavedProperty] public int PendingEnergy { get; set; }
    [SavedProperty] public bool UpgradedPages { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar("EnergyThreshold", EnergyThreshold)
    ];

    internal void SetUpgradedPages()
    {
        UpgradedPages = true;
    }

    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (amount <= 0 || card.Owner != Owner.Player || CombatState is null) return;

        PendingEnergy += amount;
        var pages = PendingEnergy / EnergyThreshold;
        if (pages <= 0) return;

        PendingEnergy %= EnergyThreshold;
        Flash();

        for (var i = 0; i < pages; i++)
        {
            var blankPage = CombatState.CreateCard<MarginPage>(card.Owner);
            if (UpgradedPages)
                CardCmd.Upgrade(blankPage);

            await CardPileCmd.AddGeneratedCardToCombat(blankPage, PileType.Hand, card.Owner);
        }
    }
}
