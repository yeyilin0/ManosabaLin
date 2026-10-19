using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.AncientCurses;
using ManosabaLin.Characters.Common.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Yalisalin.Relics;

/// <summary>
///     魔女因子（亚里沙第二个初始遗物）。
///     <para>
///         局内<b>实时</b>检测「所有牌」里有没有【魔女化】诅咒卡；只要当前没有，就立刻补一张进抽牌堆。
///     </para>
///     <para>
///         「所有牌」按项目统一口径 = 手牌 + 抽牌堆 + 弃牌堆（<b>不含</b>消耗堆），
///         与「全席宣判」等卡一致 —— 所以【魔女化】被消耗掉之后会被重新补上。
///     </para>
/// </summary>
[RegisterRelic(typeof(YalisalinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Yalisalin), Order = 10)]
public sealed class YalisalinWitchFactor : ManosabaRelicTemplate
{
    /// <summary>
    ///     补牌本身也会触发牌堆变化钩子，用静态集合挡住递归（与 <c>MagnifyingGlass.Swapping</c> 同款做法）。
    ///     用 static 而非实例字段，避免污染遗物的序列化状态。
    /// </summary>
    private static readonly HashSet<Player> Supplying = [];

    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeCombatStart()
    {
        await EnsureCurseInPlay();
    }

    /// <summary>任意牌堆变化后立刻复核一次（打出 / 抽到 / 弃掉 / 消耗都会走到这里）。</summary>
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        await EnsureCurseInPlay();
    }

    /// <summary>回合开始再兜一次底，覆盖「回合切换时才清空」的情况。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        await EnsureCurseInPlay();
    }

    private async Task EnsureCurseInPlay()
    {
        if (Owner.Creature?.CombatState is not { } combatState) return;

        // ⚠️ 必须在「战斗进行中」才补牌：
        // 战后选奖励牌（CardReward.OnSelect）等场景也会触发 AfterCardChangedPiles，
        // 而彼时 CombatManager.IsInProgress == false ⇒ AddGeneratedCardToCombat 会返回空数组，
        // 其内部 `(...)[0]` 直接抛 ArgumentOutOfRangeException，把奖励选择流程整个打断（点了没反应）。
        // 引擎文档建议用 IsOverOrEnding（而非 !IsInProgress）判断「战斗已结束/正在结束」。
        // 开局补牌不受影响：BeforeCombatStart 触发时 IsInProgress 已置 true。
        if (CombatManager.Instance.IsOverOrEnding) return;

        if (!Supplying.Add(Owner)) return;

        try
        {
            var allCards = PileType.Hand.GetPile(Owner).Cards
                .Concat(PileType.Draw.GetPile(Owner).Cards)
                .Concat(PileType.Discard.GetPile(Owner).Cards);

            if (allCards.Any(static card => card is WitchificationCurse))
                return;

            var curse = combatState.CreateCard<WitchificationCurse>(Owner);

            // 原罪诅咒本身已不自带组件：由本遗物发放的【魔女化】额外挂上「对应原罪」组件。
            curse.TryAddComponent(new Originalsin());

            await CardPileCmd.AddGeneratedCardToCombat(
                curse,
                PileType.Draw,
                Owner,
                CardPilePosition.Random);
            Flash();
        }
        finally
        {
            Supplying.Remove(Owner);
        }
    }
}
