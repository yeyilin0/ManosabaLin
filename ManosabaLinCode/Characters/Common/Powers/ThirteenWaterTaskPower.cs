// ThirteenWaterTaskPower.cs
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class ThirteenWaterTaskPower : ManosabaPowerTemplate
{
    private const int TotalTarget = 100;
    private const int BossWithAmount = 10;
    private const int BossBlockAmount = 20;
    private const int PlayerBlockReward = 20;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public int DamageTarget { get; set; }
    public int BlockTarget { get; set; }

    private int _damageAccumulated;
    private int _blockAccumulated;

 
    protected override string SmartDescriptionLocKey => Id.Entry + ".smartDescription";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("DamageTarget", 0),
        new DynamicVar("BlockTarget", 0),
    };

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var rng = Owner.Player.RunState.Rng.CombatTargets;
        DamageTarget = rng.NextInt(TotalTarget + 1);
        BlockTarget = TotalTarget - DamageTarget;

        DynamicVars["DamageTarget"].BaseValue = DamageTarget;
        DynamicVars["BlockTarget"].BaseValue = BlockTarget;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult results,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner) return;
        _damageAccumulated += results.UnblockedDamage;
    }

    public override async Task AfterBlockGained(
        Creature creature,
        Decimal amount,
        ValueProp props,
        CardModel? cardSource)
    {
        if (creature != Owner) return;
        _blockAccumulated += (int)amount;
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        var combatState = Owner.CombatState;
        bool damageDone = _damageAccumulated >= DamageTarget;
        bool blockDone = _blockAccumulated >= BlockTarget;
        bool success = damageDone && blockDone;

        var boss = combatState.Enemies.FirstOrDefault(e =>
            e.GetPower<ThirteenWaterIntelPower>() != null);
        if (boss == null) return;

        var intelPower = boss.GetPower<ThirteenWaterIntelPower>();
        if (success)
        {
            await CreatureCmd.GainBlock(Owner, PlayerBlockReward, ValueProp.Move, null);
        }
        else
        {
            await PowerCmd.Apply<WithPower>(choiceContext, boss, BossWithAmount, boss, null);
            await CreatureCmd.GainBlock(boss, BossBlockAmount, ValueProp.Move, null);
            if (intelPower != null)
                intelPower.LastTaskFailedCount++;
        }

        await PowerCmd.Remove(this);
    }
}
