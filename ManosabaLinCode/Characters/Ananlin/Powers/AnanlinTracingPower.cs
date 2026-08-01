using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinTracingPower : ManosabaPowerTemplate
{
    [SavedProperty] public bool FreeCopies { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    internal async Task CopyGeneratedCards(
        PlayerChoiceContext choiceContext,
        IReadOnlyList<CardModel> originals,
        AnansSketchbook sketchbook)
    {
        if (Amount <= 0 || originals.Count == 0) return;
        if (Owner.CombatState is not { } combatState || Owner.Player is not { } ownerPlayer) return;

        Flash();
        var copiesToCreate = (int)Amount;
        for (var i = 0; i < copiesToCreate; i++)
        {
            foreach (var original in originals)
            {
                if (original.CanonicalInstance is not { } canonical) continue;

                var copy = combatState.CreateCard(canonical, ownerPlayer);
                sketchbook.CopyUpgradeLevel(original, copy);
                if (FreeCopies)
                    copy.SetFreeIgnoringCardPlayConditions();

                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, ownerPlayer);
            }
        }

        await PowerCmd.Remove(this);
    }
}
