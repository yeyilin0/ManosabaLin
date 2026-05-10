using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class WitchBurn() : ManosabaCardTemplate(-1, CardType.Status, CardRarity.Ancient, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Retain; }
    }

    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Damage", 1m)
    };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var source = this;
        if (player != source.Owner) return;

        if (source.Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(source, PileType.Hand);
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var source = this;

        await CreatureCmd.Damage(
            choiceContext,
            source.Owner.Creature,
            source.DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Move,
            source
        );
    }

    protected override void OnUpgrade()
    {
    }
}
