using ManosabaLin.Characters.Sherrylin.Components;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     2887（2 费 攻击・全体・稀有）：
///     消耗你的全部手牌，并失去你的全部格挡，获得失去格挡等量的下回合格挡；
///     每消耗 1 张牌，对所有敌人造成 6 点伤害；
///     每消耗 2 张牌（升级后每 1 张），将 1 张【后继机】加入抽牌堆。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class SelfDestruct2887()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    private const int BaseDamagePerCard = 6;
    private const int BaseCardsPerSuccessor = 2;
    private const int UpgradedCardsPerSuccessor = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamagePerCard, ValueProp.Move),
        new DynamicVar("CardsPerSuccessor", BaseCardsPerSuccessor)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<SuccessorUnit>(false);
            yield return HoverTipFactory.FromCard<SuccessorUnit2>(false);
            yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner is not { } owner || CombatState is not { } combatState)
            return;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 失去全部格挡，等比留到下回合（沿用原版「下回合格挡」能力）。
        var blockLost = owner.Creature.Block;
        if (blockLost > 0m)
        {
            await CreatureCmd.LoseBlock(choiceContext, owner.Creature, blockLost, owner.Creature);
            await PowerCmd.Apply<BlockNextTurnPower>(
                choiceContext, owner.Creature, blockLost, owner.Creature, this, false);
        }

        // 消耗全部手牌。走正常消耗流程，因此余火 / 原罪等「被消耗」钩子照常触发。
        var hand = PileType.Hand.GetPile(owner).Cards
            .Where(card => !ReferenceEquals(card, this))
            .ToList();

        var exhausted = 0;
        foreach (var card in hand)
        {
            if (await CardCmd.Exhaust(choiceContext, card) is not null)
                exhausted++;
        }

        if (exhausted <= 0)
            return;

        await DamageCmd.Attack(exhausted * DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var perSuccessor = DynamicVars["CardsPerSuccessor"].IntValue;
        if (perSuccessor <= 0)
            return;

        var successors = exhausted / perSuccessor;
        for (var i = 0; i < successors; i++)
        {
            // 「后继机」泛指后继机与后继机2：每次随机加其中一种到抽牌堆。
            CardModel token;
            if (owner.RunState.Rng.CombatCardGeneration.NextFloat() < 0.5f)
                token = combatState.CreateCard<SuccessorUnit2>(owner);
            else
                token = combatState.CreateCard<SuccessorUnit>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Draw, owner, CardPilePosition.Random);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["CardsPerSuccessor"].BaseValue = UpgradedCardsPerSuccessor;
    }
}
