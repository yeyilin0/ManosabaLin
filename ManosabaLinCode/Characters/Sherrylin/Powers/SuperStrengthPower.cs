using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 怪力能力：回合开始获得20魔女化和1张冲击波的拳风。
/// </summary>
[RegisterPower]
public sealed class SuperStrengthPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    [SavedProperty] public bool CardUpgraded { get; set; }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        Flash();

       
        var newCard = Owner.CombatState.CreateCard<ShockwaveFist>(Owner.Player);
        if (CardUpgraded)
            newCard.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner.Player);
    }
}
