using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.ValueProps;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Reviveritualpower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _revivePending;

    public void SetRevivePending(bool value) => _revivePending = value;

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner) return true;
        if (_revivePending) return false;
        return true;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();

        // Revive to 1 HP
        await CreatureCmd.Heal(creature, 1m);

        // +1 max HP (also heals by 1, so creature ends at 2 HP)
        await CreatureCmd.GainMaxHp(creature, 1m);

        // 999 Buffer
        await PowerCmd.Apply<BufferPower>(
            new ThrowingPlayerChoiceContext(), creature, 999m, creature, null, false);

        // Generate fragments = creature's max HP
        var fragmentCount = (int)creature.MaxHp;
        var allPlayers = Owner.CombatState.Players.ToList();
        if (allPlayers.Count > 0)
        {
            var rng = Owner.Player.RunState.Rng.CombatTargets;
            for (int i = 0; i < fragmentCount; i++)
            {
                var targetPlayer = rng.NextItem(allPlayers);
                var pileType = rng.NextDouble() < 0.5 ? PileType.Draw : PileType.Discard;
                var fragment = Owner.CombatState.CreateCard<Revivefragment>(Owner.Player);
                await CardPileCmd.AddGeneratedCardToCombat(fragment, pileType, targetPlayer);
            }
        }

        _revivePending = false;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        // If ally is at full HP, remove the power peacefully
        if (Owner.CurrentHp >= Owner.MaxHp)
        {
            await PowerCmd.Remove(this);
            return;
        }

        if (Amount <= 1)
        {
            // Timer expired: strip Buffer and kill
            var buffer = Owner.GetPower<BufferPower>();
            if (buffer != null)
                await PowerCmd.Remove(buffer);

            await CreatureCmd.Damage(
                choiceContext, Owner, Owner.MaxHp,
                ValueProp.Unblockable, Owner, null);
            await PowerCmd.Remove(this);
        }
        else
        {
            await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null, false);
        }
    }
}
