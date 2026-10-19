using ManosabaLin.Characters.Common.AncientCurses;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Yalisalin.Components;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     魔女处刑「紫藤亚里沙的结局」。
///     <para>遗言一：按自身【余火】牌数的一半，使全体队友的随机等量牌获得【余火】。</para>
///     <para>遗言二：把 6 张随机原罪诅咒变为 0 费、挂上对应原罪组件，随机塞进队友手里。</para>
///     末尾与另外四张死亡卡一致：消耗全体队友 50 层【魔女化】与 3 层【嫌疑】。
/// </summary>
[RegisterCard(typeof(LinCardPool))]
public sealed class Yalisalindeath : ManosabaCardTemplate
{
    private const int SinCount = 6;
    private const int WithLoss = 50;
    private const int SuspectLoss = 3;

    public Yalisalindeath() : base(-1, CardType.Skill, CardRarity.Ancient, TargetType.AllAllies)
    {
    }

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    public override int MaxUpgradeLevel => 0;

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        if (CombatState is null) return;

        await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);

        var rng = owner.RunState.Rng.CombatCardSelection;

        // 自身「所有牌」里的【余火】牌数，一半（向下取整）
        var fireGiftCount = YalisalinFireComponentRules.AllCombatCards(owner)
            .Count(static card => YalisalinFireComponentRules.HasFireComponent(card)) / 2;

        var teammates = CombatState.GetTeammatesOf(creature)
            .Where(static teammate => teammate is { IsAlive: true, IsPlayer: true })
            .ToArray();

        foreach (var teammate in teammates)
        {
            await ConsumeWithAndSuspect(choiceContext, teammate, creature);

            // 遗言一：该队友随机 fireGiftCount 张牌获得【余火】
            if (fireGiftCount <= 0 || teammate.Player is not { } teammatePlayer)
                continue;

            var fireCandidates = YalisalinFireComponentRules.AllCombatCards(teammatePlayer)
                .Where(static card => card.Rarity is not CardRarity.Token
                    and not CardRarity.Ancient
                    and not CardRarity.Status
                    and not CardRarity.Curse)
                .Where(static card => card.Type is not CardType.Status and not CardType.Curse)
                .Where(static card => !YalisalinFireComponentRules.HasFireComponent(card))
                .Distinct()
                .OrderBy(_ => rng.NextFloat())
                .Take(fireGiftCount)
                .ToArray();

            foreach (var card in fireCandidates)
                YalisalinFireComponentRules.TryAddFireComponent(card);
        }

        // 遗言二：6 张随机原罪诅咒 → 0 费 + 对应原罪组件 → 随机加入队友手里
        if (teammates.Length == 0) return;

        for (var i = 0; i < SinCount; i++)
        {
            var target = rng.NextItem(teammates);
            if (target?.Player is not { } player) continue;

            var sin = AncientSinCardCatalog.CreateRandom(CombatState, player, rng);

            if (!sin.EnergyCost.CostsX)
                sin.EnergyCost.UpgradeBy(-sin.EnergyCost.Canonical);

            sin.TryAddComponent(new Originalsin());

            await CardPileCmd.AddGeneratedCardToCombat(sin, PileType.Hand, player);
        }
    }

    private async Task ConsumeWithAndSuspect(
        PlayerChoiceContext choiceContext,
        Creature teammate,
        Creature applier)
    {
        if (teammate.GetPower<WithPower>() is { Amount: > 0 } withPower)
        {
            var withToRemove = Math.Min(WithLoss, (int)withPower.Amount);
            await PowerCmd.ModifyAmount(choiceContext, withPower, -withToRemove, applier, this, false);
        }

        if (teammate.GetPower<SuspectPower>() is { Amount: > 0 } suspectPower)
        {
            var suspectToRemove = Math.Min(SuspectLoss, (int)suspectPower.Amount);
            await PowerCmd.ModifyAmount(choiceContext, suspectPower, -suspectToRemove, applier, this, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
