using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Magicsniperrifle : ManosabaRelicTemplate, IEasyRightClickableRelic
{
    public const int MaxCounters = 6;
    private int _counters;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override int DisplayAmount => _counters;

    public override bool ShowCounter => true;

    [SavedProperty]
    public int Counters
    {
        get => _counters;
        set
        {
            AssertMutable();
            _counters = value;
            InvokeDisplayAmountChanged();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(MaxCounters); }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (Counters < MaxCounters)
        {
            Counters++;
            Flash();
        }
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        if (Counters <= 0) return;

        Counters--;
        Flash();

        var bullet = Owner.Creature.CombatState.CreateCard<BulletCard>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(bullet, PileType.Hand, Owner);
    }

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        // TODO: 实现效果
    }
}
