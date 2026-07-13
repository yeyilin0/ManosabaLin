using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinSealedPagePower : ManosabaPowerTemplate
{
    [SavedProperty] public bool UpgradedPages { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    internal void SetUpgradedPages()
    {
        UpgradedPages = true;
    }

    internal async Task AfterSilenceRightClickRewrite(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player is not { } player || CombatState is null) return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);

        var blankPage = CombatState.CreateCard<BlankPage>(player);
        if (UpgradedPages)
            CardCmd.Upgrade(blankPage);

        await CardPileCmd.AddGeneratedCardToCombat(blankPage, PileType.Hand, player);
    }
}
