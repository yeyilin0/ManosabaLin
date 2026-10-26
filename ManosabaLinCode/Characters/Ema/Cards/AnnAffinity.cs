using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class AnnAffinity : ManosabaCardTemplate
{
    public AnnAffinity() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("PickCount", 1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond is null)
            bond = await PowerCmd.Apply<BondPower>(choiceContext, creature, 1m, creature, this, false);
        if (bond is not null)
            bond.Affinity++;

        var pickCount = DynamicVars["PickCount"].IntValue;

        var drawPile = PileType.Draw.GetPile(owner);
        var drawCards = drawPile.Cards.ToList();
        if (drawCards.Count == 0) return;

        // ⚠️ CardSelectorPrefs 的第二个参数是 MinSelect、第三个才是 MaxSelect。
        // 之前写成 (prompt, pickCount, min(pickCount, 张数))，在「升级后 pickCount=2、抽牌堆只剩 1 张」时
        // 会得到 MinSelect=2 / MaxSelect=1 —— Min > Max，确认按钮要求选中 2 张但最多只能选 1 张，
        // 于是确认键永远灰着，选择界面卡死。这里把两者一起按实际张数收紧：min == max == 实际可选数。
        var pick = Math.Min(pickCount, drawCards.Count);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, pick);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, drawCards, owner, prefs);

        // 等待选择界面完全关闭后再继续：原版选择屏幕是延迟释放（QueueFreeSafely），
        // 选完立即继续会让残留的牌框短暂留在屏幕上（过一会才消除）
        await Cmd.Wait(0.15f);

        foreach (var card in selected)
        {
            if (bond != null && bond.Affinity > bond.Estrangement)
            {
                card.EnergyCost.SetThisCombat(0);
            }
            else
            {
                var randomCost = owner.RunState.Rng.CombatEnergyCosts.NextInt(3);
                card.EnergyCost.SetThisCombat(randomCost);
            }
            card.InvokeEnergyCostChanged();
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["PickCount"].UpgradeValueBy(1);
    }
}