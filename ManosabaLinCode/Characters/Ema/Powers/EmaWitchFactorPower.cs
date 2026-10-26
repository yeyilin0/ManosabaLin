using Godot;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HealthBars;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class EmaWitchFactorPower : ManosabaPowerTemplate, IHealthBarForecastSource
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>
    /// 计算即死阈值：自身层数 + 灾厄层数（如果有）
    /// </summary>
    private int GetKillThreshold()
    {
        var doomAmount = Owner.GetPowerAmount<DoomPower>();
        return Amount + doomAmount;
    }

    private bool ShouldDie()
    {
        return Owner.CurrentHp <= GetKillThreshold();
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!Owner.IsAlive) return;
        if (!ShouldDie()) return;

        // 只有主动攻击造成的不受格挡伤害才触发即死
        // （毒伤/DoT 等回合开始结算的伤害是 Unpowered，不满足 IsPoweredAttack，不会触发）
        if (result.UnblockedDamage <= 0) return;
        if (!result.Props.IsPoweredAttack()) return;

        var results = await DamageWithoutBreakingCardPlay(choiceContext);

        // 常规情况：上述伤害就是标准的「伤害 = 当前生命」，走完伤害管线敌人即死，行为与改动前完全一致。
        // 例外情况：「坚硬外壳」这类强制限伤（Hook.ModifyHpLost 里的 ModifyHpLostBeforeOstyLate 钩子）
        // 会把这一击截断，敌人残血存活 ⇒ 即死失效。此时补一次绕开伤害管线的直接扣血。
        // 判据用 WasTargetKilled：它仅在生命值真的被这一击打到 0 时为 true
        // （文档注明「即使随后被仙女瓶复活也仍为 true」），
        // 因此这条补刀只会针对「伤害被削减」，不会越过「防止死亡」类效果。
        if (Owner.IsAlive && !results.Any(dealt => dealt.WasTargetKilled))
        {
            await KillFallbackAsync();
        }
    }

    /// <summary>
    ///     走正常伤害管线执行即死，但**绝不让异常逃出去**。
    ///     <para>
    ///         ⚠️ 2026-10-25 联机 modpack 实测：外部 mod 在 <c>Hook.ModifyDamage</c> 上挂的补丁一旦抛
    ///         <c>NullReferenceException</c>，这里就会把整条 <c>PlayCardAction</c> 打断 ——
    ///         表现是「牌卡在屏幕中间不动」+「敌人残血不死」（即死没落地）。即死是这张牌的核心效果，
    ///         不能因为别人的补丁崩掉就不生效 ⇒ 失败时直接返回空结果，交给下面的直接扣血兜底。
    ///     </para>
    /// </summary>
    private async Task<IEnumerable<DamageResult>> DamageWithoutBreakingCardPlay(PlayerChoiceContext choiceContext)
    {
        try
        {
            return await CreatureCmd.Damage(
                choiceContext, Owner, Owner.CurrentHp, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        }
        catch (Exception ex)
        {
            MegaCrit.Sts2.Core.Logging.Log.Warn($"[EmaWitchFactor] execute damage failed, falling back to direct hp loss: {ex.GetType().Name}: {ex.Message}", 2);
            return Array.Empty<DamageResult>();
        }
    }

    /// <summary>绕开伤害管线的兜底击杀（限伤 / 伤害管线异常两种情况都走这里）。</summary>
    private async Task KillFallbackAsync()
    {
        if (!Owner.IsAlive) return;

        try
        {
            await CreatureCmd.SetCurrentHp(Owner, 0);
        }
        catch (Exception ex)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error($"[EmaWitchFactor] direct hp loss failed, forcing Kill: {ex.GetType().Name}: {ex.Message}", 2);
            // 最后一道保险：连直接扣血都被拦（例如被别的 mod 的限伤钩子截断）时强制击杀，
            // 否则这张牌等于空放，且敌人残血会一直站在场上。
            try
            {
                await CreatureCmd.Kill(Owner);
            }
            catch (Exception killEx)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error($"[EmaWitchFactor] forced Kill failed: {killEx.GetType().Name}: {killEx.Message}", 2);
            }
        }
    }

    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        return HealthBarForecasts.Single(
            GetKillThreshold(),
            new Color(1f, 0.6f, 0.8f), // #ff99cc
            HealthBarForecastGrowthDirection.FromLeft);
    }
}
