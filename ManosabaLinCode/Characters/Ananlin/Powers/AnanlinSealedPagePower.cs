using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Relics;
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

        var blessedObject = player.Relics.OfType<BlessedObject>().FirstOrDefault();
        var page = blessedObject is not null
            ? blessedObject.CreateBlankPageOrReplacement(CombatState, UpgradedPages, player)
            : CombatState.CreateCard<BlankPage>(player);
        if (blessedObject is null && UpgradedPages)
            CardCmd.Upgrade(page);

        await CardPileCmd.AddGeneratedCardToCombat(page, PileType.Hand, player);
    }
}
