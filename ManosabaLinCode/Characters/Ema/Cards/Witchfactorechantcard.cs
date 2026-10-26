using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Witchfactorechantcard : ManosabaCardTemplate
{
    public Witchfactorechantcard() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    private static readonly EnchantmentModel[] TrialEnchants =
    [
        ModelDb.Enchantment<Rebuttal>(),
        ModelDb.Enchantment<Agreement>(),
        ModelDb.Enchantment<Doubt>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Divisor", 50)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<Witchfactorechantpower>();
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<EmaWitchFactorPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var withPower = creature.GetPower<WithPower>();
        var currentWith = withPower?.Amount ?? 0;
        var divisor = DynamicVars["Divisor"].IntValue;
        var stacks = (int)(currentWith / divisor);

        if (stacks <= 0) return;

        await PowerCmd.Apply<Witchfactorechantpower>(
            choiceContext, creature, stacks, creature, this, false);

        var rng = owner.RunState.Rng.CombatCardSelection;
        var deckPile = PileType.Deck.GetPile(owner);

        // ⚠️★ 取证：本卡是艾玛体系里**唯一**会永久改主牌组（PileType.Deck）卡牌费用的地方。
        // 主牌组不在 NetFullCombatState 的校验和里（引擎 PileType 文档：「卡在房间之间住的地方，
        // 新战斗开始时全部克隆进抽牌堆」）⇒ 两端一旦在这条路径上不一致，**要等下一场战斗克隆时才暴露**
        // （#289 / #434 两个分歧包都是这个形态：最后一个匹配项 = Exiting xxx room，紧接着的新战斗
        // 「After player turn start」就炸）。
        // 已用真实基础费用/升级 Delta 核过账：#434 里主机侧永久降费 0 张、客户端 2 张 ⇒ 不是「挑的卡不同」，
        // 而是**次数/是否执行**不对称。下行日志把「输入」与「输出」都打出来，下一份分歧包可直接对账。
        Log.Info($"[WitchFactorEnchant] IN netId={owner.NetId} deckCards={deckPile.Cards.Count} " +
                 $"with={(int)currentWith} stacks={stacks}");

        var unenchanted = deckPile.Cards.Where(c =>
            c.Enchantment == null
            && c.Rarity != CardRarity.Token
            && c.Rarity != CardRarity.Ancient
            && c.Type != CardType.Status
            && c.Type != CardType.Curse).ToList();

        // ⚠️★ 这里改的是 **主牌组（PileType.Deck）里的常驻卡**，而 NetFullCombatState 只序列化战斗牌堆
        // （ Hand/Draw/Discard/Exhaust/Play ）—— 主卡不在校验和里。一旦两端挑中的不是同一批卡，
        // 分歧要等到「下一场战斗的第一个 After player turn start」才暴露（#289 / #434 都是这个形态：
        // 差异全是对端多出来的 permanent 降费，且卡面恰好都是本卡才会上的审判附魔）。
        // ⇒ 先把「牌组列表顺序」这个**不可校验**的自由度消掉：按卡名+升级层数做稳定排序后再随机挑，
        //    只要两端牌组内容一致（数量/构成已由其它同步字段保证），挑出来的就是同一批卡。
        var ordered = unenchanted
            .OrderBy(static c => c.Id.Entry, StringComparer.Ordinal)
            .ThenBy(static c => c.CurrentUpgradeLevel)
            .ToList();

        var toEnchant = ordered
            .OrderBy(_ => rng.NextFloat())
            .Take(stacks)
            .ToList();

        // ⚠️★ 逐卡取证。-1 前后各取一次「含全部修饰器的当前费用」：
        // 若两端日志里 before/after 一样（即 after 没变小），就说明这行在那一端根本没生效 —— 直接就能把
        // 「数量不对称」定位到「写入失败」，而不是再猜。
        foreach (var card in toEnchant)
        {
            var enchant = rng.NextItem(TrialEnchants);
            CardCmd.Enchant(enchant.ToMutable(), card, 1m);

            var before = card.EnergyCost.GetWithModifiers(CostModifiers.All);
            card.EnergyCost.UpgradeBy(-1);
            var after = card.EnergyCost.GetWithModifiers(CostModifiers.All);

            Log.Info($"[WitchFactorEnchant] APPLY netId={owner.NetId} card={card.Id.Entry}#{card.CurrentUpgradeLevel} " +
                     $"before={before} after={after} isEnding={CombatManager.Instance.IsEnding}");
        }

        // 取证用：下一份分歧包里直接对账两端挑的是不是同一批卡。
        Log.Info($"[WitchFactorEnchant] OUT netId={owner.NetId} stacks={stacks} candidates={ordered.Count} " +
                 $"isEnding={CombatManager.Instance.IsEnding} " +
                 $"picks=[{string.Join(", ", toEnchant.Select(static c => c.Id.Entry + "#" + c.CurrentUpgradeLevel))}]");
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Divisor"].UpgradeValueBy(-20);
    }
}
