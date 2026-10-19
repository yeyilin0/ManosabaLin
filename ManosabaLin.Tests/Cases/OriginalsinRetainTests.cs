using ManosabaLin.Characters.Common.AncientCurses;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Yalisalin;
using ManosabaLin.Characters.Yalisalin.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using System.Threading.Tasks;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

/// <summary>
///     【原罪诅咒】的时机：<b>回合结束自动保留 → 立刻触发「宽恕」</b>。
///     <para>
///         旧实现是「回合结束保留 → <b>下回合开始</b>才宽恕」（挂在 <c>AfterPlayerTurnStartEarlyPostfix</c>），
///         本组用例守住新时机，同时防止「回合结束 + 下回合开始」双触发
///         ——一次结束回合只允许宽恕 1 次，重复触发会让数值翻倍。
///     </para>
///     <para>
///         ⚠️ 2026-10-12 起原罪诅咒<b>不再自带</b> <see cref="Originalsin" /> 组件
///         （组件 = 【原罪】关键词 + 保留 + 宽恕/自惩，只由获得方挂载，见
///         <c>AncientSinCardCatalog</c> 注释）。所以用例里用
///         <see cref="AddToHand{T}" /> 造出裸卡后必须<b>补挂组件</b>，
///         才能模拟玩家实际获得这张诅咒的路径（魔女因子遗物 / 亚里沙的原罪卡）。
///     </para>
/// </summary>
public sealed class OriginalsinRetainTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Yalisalin>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosaba-originalsin-retain");
    }

    /// <summary>harness 铁律①：每个用例至少执行一个战斗 GameAction。</summary>
    private async Task PlaySomething()
    {
        var card = await AddToHand<Ignite>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
        await WaitForIdle();
    }

    /// <summary>模拟「由获得方挂载组件」的真实获取路径。</summary>
    private WitchificationCurse GiveComponent(WitchificationCurse curse)
    {
        curse.TryAddComponent(new Originalsin());
        return curse;
    }

    /// <summary>
    ///     【魔女化】诅咒在手牌里过回合：回合结束自动保留 → 立刻「宽恕」→ 失去 10 层魔女化（50 → 40）。
    ///     若旧的下回合开始钩子还在（或与新钩子并存），这里会变成 30。
    /// </summary>
    [Fact]
    public async Task Curse_forgives_exactly_once_at_turn_end_after_retain()
    {
        await PlaySomething();

        var curse = GiveComponent(await AddToHand<WitchificationCurse>());
        await ApplyPower<WithPower>(Player.Creature, 50);
        await WaitForIdle();

        Assert.Equal(50, CardTestAssertions.PowerAmount<WithPower>(Player.Creature));

        await EndTurn();
        await WaitForIdle();

        // 「自动保留」：回合结束后这张牌仍然在手牌里（没有被清手牌丢进弃牌堆）
        Assert.Contains(curse, PileType.Hand.GetPile(Player).Cards);

        // 「立刻宽恕」正好 1 次：50 - 10
        Assert.Equal(40, CardTestAssertions.PowerAmount<WithPower>(Player.Creature));
    }

    /// <summary>
    ///     对照用例：不在手牌里的原罪回合结束不会被保留，因此也不会宽恕。
    /// </summary>
    [Fact]
    public async Task Curse_outside_hand_is_not_forgiven()
    {
        await PlaySomething();

        var curse = GiveComponent(await AddToHand<WitchificationCurse>());
        await CardPileCmd.Add(curse, PileType.Exhaust, CardPilePosition.Random);
        await ApplyPower<WithPower>(Player.Creature, 50);
        await WaitForIdle();

        await EndTurn();
        await WaitForIdle();

        Assert.Equal(50, CardTestAssertions.PowerAmount<WithPower>(Player.Creature));
    }
}
