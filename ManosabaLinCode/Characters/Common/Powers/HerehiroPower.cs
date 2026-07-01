// HerehiroPower.cs
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.HandSize;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class HerehiroPower : ManosabaPowerTemplate, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public List<CardModel> RememberedCards { get; private set; } = new();

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        RememberedCards = new List<CardModel>(RememberedCards);
    }

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        if (player != Owner.Player) return currentMaxHandSize;
        return currentMaxHandSize - Amount;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        foreach (var card in RememberedCards)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
        RememberedCards.Clear();
    }
}
