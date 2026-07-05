using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Afflictions;

[RegisterAffliction]
public sealed class ErosionAffliction : AfflictionModel
{
    public override bool CanAfflictCardType(CardType cardType) => true;
    public override bool CanAfflictUnplayableCards => true;
    public override bool IsStackable => false;

    public override void AfterApplied()
    {
        Card?.AddKeyword(CardKeyword.Innate);
        Card?.AddKeyword(CardKeyword.Retain);
    }

    public override void BeforeRemoved()
    {
        Card?.RemoveKeyword(CardKeyword.Innate);
        Card?.RemoveKeyword(CardKeyword.Retain);
    }
}
