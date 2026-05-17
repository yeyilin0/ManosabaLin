using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Revokation() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int MaxHpLoss = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new CardsVar(1)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<DeathRewind>();
            yield return HoverTipFactory.FromPower<DeathRewindPower>();
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return HoverTipFactory.FromCard<Justice>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        // 降低 3 点血量上限
        await CreatureCmd.LoseMaxHp(choiceContext, source.Owner.Creature, MaxHpLoss, true);

        // 攻击目标
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 收集所有区域的死亡回溯卡牌
        var allCards = new List<CardModel>();

        var drawCards = PileType.Draw.GetPile(source.Owner).Cards.Where(c => c is DeathRewind).ToList();
        var handCards = PileType.Hand.GetPile(source.Owner).Cards.Where(c => c is DeathRewind).ToList();
        var discardCards = PileType.Discard.GetPile(source.Owner).Cards.Where(c => c is DeathRewind).ToList();

        allCards.AddRange(drawCards);
        allCards.AddRange(handCards);
        allCards.AddRange(discardCards);

        // 选择 0~1 张死亡回溯消耗（可不选）
        var cardExhausted = false;
        if (allCards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 0, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext, allCards, source.Owner, prefs
            );

            var card = selected.FirstOrDefault();
            if (card != null)
            {
                await CardCmd.Exhaust(choiceContext, card);
                cardExhausted = true;
            }
        }
        // 移除自身死亡回溯能力
        var hasDeathRewindPower = source.Owner.Creature.GetPower<DeathRewindPower>() != null;
        if (hasDeathRewindPower)
            await PowerCmd.Remove<DeathRewindPower>(source.Owner.Creature);

        // 如果消耗了卡牌或移除了能力，加入一张保留的正义
        if (cardExhausted || hasDeathRewindPower)
        {
            var justice = source.CombatState.CreateCard<Justice>(source.Owner);
            justice.SetToFreeThisTurn();
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(justice, PileType.Hand, source.Owner)
            );
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
