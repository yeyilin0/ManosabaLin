using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheCopperBeeches() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Block", 6)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var blockAmount = (int)source.DynamicVars["Block"].BaseValue;

        var cardsExhaustedThisTurn = CombatManager.Instance.History.Entries
            .OfType<CardExhaustedEntry>()
            .Any(e => e.HappenedThisTurn(CombatState) && e.Card.Owner == Owner);

        if (cardsExhaustedThisTurn)
        {
            blockAmount *= 2;
        }

        await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Block"].UpgradeValueBy(2m);
    }
}
