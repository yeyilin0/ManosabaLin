using ManosabaLin.Characters.Common.Components.Abstracts;
using ManosabaLin.Characters.Common.AncientCurses;
using ManosabaLin.Characters.Common.AncientCurses.Powers;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using TempStrength = ManosabaLin.Characters.Common.Powers.TempStrength;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Common.Components;

public sealed partial class Originalsin : KeywordLikeComponent
{
    private const string HoverTipTitleKey = "ManosabaLin.Originalsin.hovertip.title";
    private const string HiroparanoidTipKey = "ManosabaLin.Originalsin.Hiroparanoid.hovertip.description";
    private const string MargeCharmTipKey = "ManosabaLin.Originalsin.MargeCharm.hovertip.description";
    private const string MeruruCowardiceTipKey = "ManosabaLin.Originalsin.MeruruCowardice.hovertip.description";
    private const string NoahEnsnareTipKey = "ManosabaLin.Originalsin.NoahEnsnare.hovertip.description";
    private const string NayukaJealousyTipKey = "ManosabaLin.Originalsin.NayukaJealousy.hovertip.description";
    private const string SherryVoidTipKey = "ManosabaLin.Originalsin.SherryVoid.hovertip.description";
    private const string RaiyaMadnessTipKey = "ManosabaLin.Originalsin.RaiyaMadness.hovertip.description";
    private const string AnanlinVanityTipKey = "ManosabaLin.Originalsin.AnanlinVanity.hovertip.description";
    private const string EmaregretTipKey = "ManosabaLin.Originalsin.Emaregret.hovertip.description";
    private const string HannadelusionTipKey = "ManosabaLin.Originalsin.Hannadelusion.hovertip.description";
    private const string CocoworryTipKey = "ManosabaLin.Originalsin.Cocoworry.hovertip.description";
    private const string ArisaGuiltTipKey = "ManosabaLin.Originalsin.ArisaGuilt.hovertip.description";
    private const string MiliaLostTipKey = "ManosabaLin.Originalsin.MiliaLost.hovertip.description";
    private const string WitchificationCurseTipKey = "ManosabaLin.Originalsin.WitchificationCurse.hovertip.description";

    private const string HiroparanoidPrefixKey = "ManosabaLin.Originalsin.Hiroparanoid.prefix";
    private const string MargeCharmPrefixKey = "ManosabaLin.Originalsin.MargeCharm.prefix";
    private const string MeruruCowardicePrefixKey = "ManosabaLin.Originalsin.MeruruCowardice.prefix";
    private const string NoahEnsnarePrefixKey = "ManosabaLin.Originalsin.NoahEnsnare.prefix";
    private const string NayukaJealousyPrefixKey = "ManosabaLin.Originalsin.NayukaJealousy.prefix";
    private const string SherryVoidPrefixKey = "ManosabaLin.Originalsin.SherryVoid.prefix";
    private const string RaiyaMadnessPrefixKey = "ManosabaLin.Originalsin.RaiyaMadness.prefix";
    private const string AnanlinVanityPrefixKey = "ManosabaLin.Originalsin.AnanlinVanity.prefix";
    private const string EmaregretPrefixKey = "ManosabaLin.Originalsin.Emaregret.prefix";
    private const string HannadelusionPrefixKey = "ManosabaLin.Originalsin.Hannadelusion.prefix";
    private const string CocoworryPrefixKey = "ManosabaLin.Originalsin.Cocoworry.prefix";
    private const string ArisaGuiltPrefixKey = "ManosabaLin.Originalsin.ArisaGuilt.prefix";
    private const string MiliaLostPrefixKey = "ManosabaLin.Originalsin.MiliaLost.prefix";
    private const string WitchificationCursePrefixKey = "ManosabaLin.Originalsin.WitchificationCurse.prefix";

    protected override LocString PrefixLocString => Card switch
    {
        Hiroparanoid => new LocString("cards", HiroparanoidPrefixKey),
        MargeCharm => new LocString("cards", MargeCharmPrefixKey),
        MeruruCowardice => new LocString("cards", MeruruCowardicePrefixKey),
        NoahEnsnare => new LocString("cards", NoahEnsnarePrefixKey),
        NayukaJealousy => new LocString("cards", NayukaJealousyPrefixKey),
        SherryVoid => new LocString("cards", SherryVoidPrefixKey),
        RaiyaMadness => new LocString("cards", RaiyaMadnessPrefixKey),
        AnanlinVanity => new LocString("cards", AnanlinVanityPrefixKey),
        Emaregret => new LocString("cards", EmaregretPrefixKey),
        Hannadelusion => new LocString("cards", HannadelusionPrefixKey),
        Cocoworry => new LocString("cards", CocoworryPrefixKey),
        ArisaGuilt => new LocString("cards", ArisaGuiltPrefixKey),
        MiliaLost => new LocString("cards", MiliaLostPrefixKey),
        WitchificationCurse => new LocString("cards", WitchificationCursePrefixKey),
        _ => base.PrefixLocString,
    };

    public override IEnumerable<IHoverTip> HoverTips
    {
        get
        {
            var descriptionKey = Card switch
            {
                Hiroparanoid => HiroparanoidTipKey,
                MargeCharm => MargeCharmTipKey,
                MeruruCowardice => MeruruCowardiceTipKey,
                NoahEnsnare => NoahEnsnareTipKey,
                NayukaJealousy => NayukaJealousyTipKey,
                SherryVoid => SherryVoidTipKey,
                RaiyaMadness => RaiyaMadnessTipKey,
                AnanlinVanity => AnanlinVanityTipKey,
                Emaregret => EmaregretTipKey,
                Hannadelusion => HannadelusionTipKey,
                Cocoworry => CocoworryTipKey,
                ArisaGuilt => ArisaGuiltTipKey,
                MiliaLost => MiliaLostTipKey,
                WitchificationCurse => WitchificationCurseTipKey,
                _ => null,
            };
            if (descriptionKey == null) yield break;

            var title = new LocString("cards", HoverTipTitleKey);
            var description = new LocString("cards", descriptionKey);

            // 组件悬浮提示的描述里用了 {energyPrefix:energyIcons(...)} 占位符，
            // 必须像 CardEffectHoverTipFactory.FromCard 那样把 energyPrefix（取自本组件所属卡牌的能量色）
            // 作为格式变量注入，否则 SmartFormat 找不到该 selector 会直接抛
            // “No source extension could handle the selector named energyPrefix”。
            var energyPrefix = EnergyIconHelper.GetPrefix(Card);
            title.Add("energyPrefix", energyPrefix);
            description.Add("energyPrefix", energyPrefix);
            Card.DynamicVars.AddTo(title);
            Card.DynamicVars.AddTo(description);

            yield return new HoverTip(title, description);
        }
    }

    /// <summary>
    ///     本回合是否在回合结束时保留了自己。只有真的执行过保留的那一侧才为 true，
    ///     用于 <see cref="AfterSideTurnEndPostfix" /> 判定「这张牌是被保留下来的」，
    ///     避免把「回合结束阶段才被塞进手牌」的牌误判成保留。
    /// </summary>
    private bool _retainedForTurnEnd;

    public override Task BeforeSideTurnEndPostfix(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants,
        ComponentContext componentContext)
    {
        if (Card?.Owner?.Creature is { } creature && side == creature.Side)
        {
            Card.GiveSingleTurnRetain();
            _retainedForTurnEnd = true;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    ///     回合结束「自动保留」后<b>立刻</b>触发「宽恕」。
    ///     <para>
    ///         引擎的回合结束顺序是：<c>BeforeSideTurnEnd</c>（本组件在这里保留）→ 手牌里的回合结束牌效果
    ///         → <c>BeforeFlush</c> → 清手牌 → <c>AfterSideTurnEnd</c>。
    ///         放在 <c>AfterSideTurnEnd</c> 才能保证两点：① 被保留的牌此刻仍在手牌里；
    ///         ② 宽恕效果新加入手牌的牌不会被同一回合的清手牌冲掉（例如 Cocoworry 的「互换」）。
    ///     </para>
    ///     <para>
    ///         ⚠️ 旧实现挂在 <c>AfterPlayerTurnStartEarlyPostfix</c>（下回合开始才宽恕），
    ///         已删除 —— 否则会和这里一起构成「一回合宽恕两次」。
    ///     </para>
    /// </summary>
    public override async Task AfterSideTurnEndPostfix(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants,
        ComponentContext componentContext)
    {
        if (!_retainedForTurnEnd) return;
        _retainedForTurnEnd = false;

        if (Card?.Owner?.Creature is not { } creature) return;
        if (side != creature.Side) return;
        if (Card.Pile?.Type != PileType.Hand) return;

        await Forgive(choiceContext);
    }

    public override async Task OnPlayPostfix(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await Punish(choiceContext);
    }

    /// <summary>
    /// 任意原罪触发「自惩」后触发（含被打出自惩与卡牌主动触发的免费自惩）。
    /// </summary>
    public static event Action<PlayerChoiceContext, CardModel>? PunishTriggered;

    /// <summary>
    /// 任意原罪触发「宽恕」后触发（含主动宽恕与卡牌自动宽恕）。
    /// </summary>
    public static event Action<PlayerChoiceContext, CardModel>? ForgiveTriggered;

    /// <summary>
    /// 立刻触发一次本组件的「宽恕」效果，并计入本场宽恕次数。
    /// </summary>
    public async Task Forgive(PlayerChoiceContext choiceContext)
    {
        var owner = Card?.Owner;
        if (owner != null)
        {
            await PowerCmd.Apply<OriginalsinForgivenessCounterPower>(
                choiceContext, owner.Creature, 1m, owner.Creature, Card, false);
            await PowerCmd.Apply<OriginalsinResolveCounterPower>(
                choiceContext, owner.Creature, 1m, owner.Creature, Card, false);
        }

        switch (Card)
        {
            case Hiroparanoid:
                await ForgiveHiroparanoid(choiceContext);
                break;
            case MargeCharm:
                await ForgiveMargeCharm(choiceContext);
                break;
            case MeruruCowardice:
                await ForgiveMeruruCowardice(choiceContext);
                break;
            case NoahEnsnare:
                await ForgiveNoahEnsnare(choiceContext);
                break;
            case NayukaJealousy:
                await ForgiveNayukaJealousy(choiceContext);
                break;
            case SherryVoid:
                await ForgiveSherryVoid(choiceContext);
                break;
            case RaiyaMadness:
                await ForgiveRaiyaMadness(choiceContext);
                break;
            case AnanlinVanity:
                await ForgiveAnanlinVanity(choiceContext);
                break;
            case Emaregret:
                await ForgiveEmaregret(choiceContext);
                break;
            case Hannadelusion:
                await ForgiveHannadelusion(choiceContext);
                break;
            case Cocoworry:
                await ForgiveCocoworry(choiceContext);
                break;
            case ArisaGuilt:
                await ForgiveArisaGuilt(choiceContext);
                break;
            case MiliaLost:
                await ForgiveMiliaLost(choiceContext);
                break;
            case WitchificationCurse:
                await ForgiveWitchificationCurse(choiceContext);
                break;
        }

        if (Card != null)
            ForgiveTriggered?.Invoke(choiceContext, Card);
    }

    /// <summary>
    /// 立刻触发一次本组件的「自惩」效果，并广播自惩触发事件。
    /// </summary>
    public async Task Punish(PlayerChoiceContext choiceContext)
    {
        var owner = Card?.Owner;
        if (owner != null)
        {
            await PowerCmd.Apply<OriginalsinResolveCounterPower>(
                choiceContext, owner.Creature, 1m, owner.Creature, Card, false);
        }

        switch (Card)
        {
            case Hiroparanoid:
                await PunishHiroparanoid(choiceContext);
                break;
            case MargeCharm:
                await PunishMargeCharm(choiceContext);
                break;
            case MeruruCowardice:
                await PunishMeruruCowardice(choiceContext);
                break;
            case NoahEnsnare:
                await PunishNoahEnsnare(choiceContext);
                break;
            case NayukaJealousy:
                await PunishNayukaJealousy(choiceContext);
                break;
            case SherryVoid:
                await PunishSherryVoid(choiceContext);
                break;
            case RaiyaMadness:
                await PunishRaiyaMadness(choiceContext);
                break;
            case AnanlinVanity:
                await PunishAnanlinVanity(choiceContext);
                break;
            case Emaregret:
                await PunishEmaregret(choiceContext);
                break;
            case Hannadelusion:
                await PunishHannadelusion(choiceContext);
                break;
            case Cocoworry:
                await PunishCocoworry(choiceContext);
                break;
            case ArisaGuilt:
                await PunishArisaGuilt(choiceContext);
                break;
            case MiliaLost:
                await PunishMiliaLost(choiceContext);
                break;
            case WitchificationCurse:
                await PunishWitchificationCurse(choiceContext);
                break;
        }

        if (Card != null)
            PunishTriggered?.Invoke(choiceContext, Card);
    }

    private async Task ForgiveHiroparanoid(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext, owner.Creature, 1m, owner.Creature, card);

        card.DynamicVars["DiscardCount"].BaseValue += 1m;
    }

    private async Task PunishHiroparanoid(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        await CardPileCmd.Draw(choiceContext, 1, owner);

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            new CardSelectorPrefs(
                new LocString("cards", $"{card.Id.Entry}.selectionScreenPrompt"), 1, 1),
            null,
            card)).FirstOrDefault();

        if (selected != null)
            await CardCmd.Exhaust(choiceContext, selected);
    }

    private async Task ForgiveMargeCharm(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["Chance"].BaseValue += 5m;

        var me = owner.Creature;
        var combatState = me.CombatState;
        if (combatState == null) return;

        var enemies = combatState.Creatures
            .Where(c => c.IsAlive && c.Side != me.Side)
            .ToList();
        if (enemies.Count == 0) return;

        var rng = owner.RunState?.Rng.CombatCardGeneration;
        if (rng == null) return;

        var target = rng.NextItem(enemies);
        if (target == null) return;

        var regen = me.GetPower<RegenPower>()?.Amount ?? 0m;
        var plating = me.GetPower<PlatingPower>()?.Amount ?? 0m;
        if (regen <= 0 && plating <= 0) return;

        if (regen >= plating)
            await PowerCmd.Apply<RegenPower>(choiceContext, target, Half(regen), me, card);
        else
            await PowerCmd.Apply<PlatingPower>(choiceContext, target, Half(plating), me, card);
    }

    private async Task PunishMargeCharm(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var me = owner.Creature;

        var combatState = me.CombatState;
        if (combatState == null) return;

        var enemies = combatState.Creatures
            .Where(c => c.IsAlive && c.Side != me.Side)
            .ToList();
        if (enemies.Count == 0) return;

        var rng = owner.RunState?.Rng.CombatCardGeneration;
        if (rng == null) return;

        var enemy = rng.NextItem(enemies);
        if (enemy == null) return;

        var regen = enemy.GetPower<RegenPower>()?.Amount ?? 0m;
        var plating = enemy.GetPower<PlatingPower>()?.Amount ?? 0m;
        if (regen <= 0 && plating <= 0) return;

        var amount = Half(regen >= plating ? regen : plating);

        if (regen >= plating)
            await PowerCmd.Apply<RegenPower>(choiceContext, me, amount, me, card);
        else
            await PowerCmd.Apply<PlatingPower>(choiceContext, me, amount, me, card);

        await CreatureCmd.Damage(
            choiceContext,
            me,
            amount,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            card,
            null);
    }

    private async Task ForgiveMeruruCowardice(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var me = owner.Creature;

        if (card.DynamicVars.TryGetValue("Penalty", out var penalty))
            penalty.BaseValue = Math.Max(0m, penalty.BaseValue - 1m);

        if (me.GetPower<OriginalsinMeruruBlockTracker>() is null)
            await PowerCmd.Apply<OriginalsinMeruruBlockTracker>(choiceContext, me, 1m, me, card);

        var tracker = me.GetPower<OriginalsinMeruruBlockTracker>();
        var dmg = tracker?.LastTurnGained ?? 0m;
        if (dmg <= 0m) return;

        var combatState = me.CombatState;
        if (combatState == null) return;
        var enemies = combatState.Enemies.Where(c => c.IsAlive).ToList();
        if (enemies.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        var enemy = rng.NextItem(enemies);
        if (enemy == null) return;

        await CreatureCmd.Damage(
            choiceContext,
            enemy,
            dmg,
            ValueProp.Unpowered | ValueProp.Move,
            card,
            null);
    }

    private async Task PunishMeruruCowardice(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var me = owner.Creature;

        var lost = Math.Min(15m, me.Block);
        if (lost > 0m)
            await CreatureCmd.LoseBlock(choiceContext, me, lost, me);

        if (lost <= 0m) return;
        var combatState = me.CombatState;
        if (combatState == null) return;

        var allies = combatState.Allies.Where(c => c.IsAlive).ToList();
        if (allies.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        var ally = rng.NextItem(allies);
        if (ally == null) return;

        await CreatureCmd.Heal(ally, lost);
    }

    private async Task ForgiveNoahEnsnare(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var rng = owner.RunState.Rng.CombatCardGeneration;

        if (rng.NextFloat() < 0.5f)
            await CardPileCmd.Add(card, PileType.Discard, CardPilePosition.Random);
        else
            await CardCmd.Exhaust(choiceContext, card);

        if (card.DynamicVars.TryGetValue("PaintCount", out var paintCount))
            paintCount.BaseValue += 1m;

        var paints = PileType.Hand.GetPile(owner).Cards.OfType<SelfControlledPaint>()
            .Concat(PileType.Draw.GetPile(owner).Cards.OfType<SelfControlledPaint>())
            .Concat(PileType.Discard.GetPile(owner).Cards.OfType<SelfControlledPaint>())
            .Count();

        if (paints > 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, owner.Creature, paints, owner.Creature, card);
    }

    private async Task PunishNoahEnsnare(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        if (owner.Creature.CombatState is not { } combatState) return;

        var allies = combatState.Players.Where(p => p.Creature.IsAlive).ToList();
        if (allies.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        var ally = rng.NextItem(allies);
        if (ally == null) return;

        var paint = combatState.CreateCard<SelfControlledPaint>(ally);
        await CardPileCmd.AddGeneratedCardToCombat(paint, PileType.Draw, ally, CardPilePosition.Random);
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, ally.Creature, 2m, owner.Creature, card);
    }

    private async Task ForgiveNayukaJealousy(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["Chance"].BaseValue += 5m;

        var discard = PileType.Discard.GetPile(owner).Cards.ToList();
        var n = discard.Count;
        if (n <= 0) return;

        var pool = PileType.Draw.GetPile(owner).Cards
            .Concat(PileType.Hand.GetPile(owner).Cards)
            .Concat(PileType.Discard.GetPile(owner).Cards)
            .Where(c => c.Pile?.Type != PileType.Exhaust)
            .ToList();
        if (pool.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        var count = Math.Min(n, pool.Count);
        for (var i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;
            var pick = rng.NextItem(pool);
            if (pick == null) break;
            pool.Remove(pick);
            await CardPileCmd.Add(pick, PileType.Exhaust, CardPilePosition.Random);
        }
    }

    private async Task PunishNayukaJealousy(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var n = PileType.Discard.GetPile(owner).Cards.Count;
        if (n <= 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        for (var i = 0; i < n; i++)
        {
            if (rng.NextFloat() >= 0.1f) continue;

            var drawPile = PileType.Draw.GetPile(owner);
            var exhaustPile = PileType.Exhaust.GetPile(owner);
            if (drawPile.Cards.Count == 0 || exhaustPile.Cards.Count == 0) continue;

            var fromDraw = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                drawPile,
                owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1))).FirstOrDefault();
            if (fromDraw == null) continue;

            var fromExhaust = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                exhaustPile,
                owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1))).FirstOrDefault();
            if (fromExhaust == null) continue;

            await CardPileCmd.Add(fromDraw, PileType.Exhaust, CardPilePosition.Random);
            await CardPileCmd.Add(fromExhaust, PileType.Draw, CardPilePosition.Random);
        }
    }

    private async Task ForgiveSherryVoid(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["Threshold"].BaseValue += 15m;
        await PowerCmd.Apply<OriginalsinSherryShareTempStrengthPower>(
            choiceContext, owner.Creature, 1m, owner.Creature, card);
    }

    private async Task PunishSherryVoid(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        await PowerCmd.Apply<StrengthPower>(choiceContext, owner.Creature, -1m, owner.Creature, card);
        await PowerCmd.Apply<TempStrength>(choiceContext, owner.Creature, 4m, owner.Creature, card);
    }

    private async Task ForgiveRaiyaMadness(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["WhiffChance"].BaseValue += 5m;
        card.DynamicVars["ExtraChance"].BaseValue += 5m;

        var extra = owner.Creature.GetPower<OriginalsinRaiyaExtraPower>();
        if (extra is null)
            extra = await PowerCmd.Apply<OriginalsinRaiyaExtraPower>(
                choiceContext, owner.Creature, card.DynamicVars["ExtraChance"].BaseValue, owner.Creature, card);
        if (card is RaiyaMadness madness)
            extra?.SyncFromCard(madness);
    }

    private async Task PunishRaiyaMadness(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["WhiffChance"].BaseValue += 5m;
        card.DynamicVars["ExtraChance"].BaseValue += 5m;

        var extra = owner.Creature.GetPower<OriginalsinRaiyaExtraPower>();
        if (extra is null)
            extra = await PowerCmd.Apply<OriginalsinRaiyaExtraPower>(
                choiceContext, owner.Creature, card.DynamicVars["ExtraChance"].BaseValue, owner.Creature, card);
        if (extra != null)
        {
            extra.ForcedExtra = true;
            if (card is RaiyaMadness madness)
                extra.SyncFromCard(madness);
        }
    }

    private async Task ForgiveAnanlinVanity(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
        card.DynamicVars["DrawCount"].BaseValue += 1m;
        card.DynamicVars["DiscardCount"].BaseValue += 1m;
    }

    private async Task PunishAnanlinVanity(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var rng = owner.RunState.Rng.CombatCardGeneration;

        await CardPileCmd.Draw(choiceContext, 3, owner);

        var selectable = PileType.Hand.GetPile(owner).Cards.Where(c => !ReferenceEquals(c, card)).ToList();
        if (selectable.Count == 0) return;

        var count = Math.Min(2, selectable.Count);
        var toDiscard = (await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, count, count),
            c => !ReferenceEquals(c, card),
            card)).ToList();

        foreach (var c in toDiscard)
        {
            var pile = rng.NextFloat() < 0.5f ? PileType.Draw : PileType.Discard;
            await CardPileCmd.Add(c, pile, CardPilePosition.Random);
        }
    }

    private async Task ForgiveEmaregret(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["HpLoss"].BaseValue += 1m;
        await PowerCmd.Apply<OriginalsinEmaregretWitchOnHpLossPower>(
            choiceContext, owner.Creature, 1m, owner.Creature, card);
    }

    private async Task PunishEmaregret(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var me = owner.Creature;

        if (me.GetPower<OriginalsinEmaregretBlockLossPower>() is null)
            await PowerCmd.Apply<OriginalsinEmaregretBlockLossPower>(choiceContext, me, 1m, me, card);

        var lost = me.GetPower<OriginalsinEmaregretBlockLossPower>()?.LostThisTurn ?? 0m;
        if (lost <= 0m) return;

        await CreatureCmd.Heal(me, lost);

        var combatState = me.CombatState;
        if (combatState == null) return;
        var enemies = combatState.Enemies.Where(c => c.IsAlive).ToList();
        if (enemies.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        var enemy = rng.NextItem(enemies);
        if (enemy == null) return;

        await PowerCmd.Apply<EmaWitchFactorPower>(choiceContext, enemy, lost, me, card);
    }

    private async Task ForgiveHannadelusion(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["Chance"].BaseValue += 5m;
        if (card is Hannadelusion hanna)
            await hanna.RunGoldChange(owner.RunState.Rng.CombatCardGeneration);
    }

    private async Task PunishHannadelusion(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        if (card is not Hannadelusion hanna) return;

        var gold = owner.Gold;
        if (gold <= 0) return;

        await PlayerCmd.LoseGold(gold, owner);
        var rolls = (int)(gold / 10);
        var chance = (float)(card.DynamicVars["Chance"].BaseValue / 100m);
        var rng = owner.RunState.Rng.CombatCardGeneration;

        for (var i = 0; i < rolls; i++)
        {
            if (rng.NextFloat() >= chance) continue;
            await hanna.RunGoldChange(rng);
        }
    }

    private async Task ForgiveCocoworry(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var draw = PileType.Draw.GetPile(owner).Cards.ToList();
        if (draw.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        var picked = rng.NextItem(draw);
        if (picked == null) return;

        var rarity = picked.Rarity;
        await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
        await CardPileCmd.Add(picked, PileType.Hand);

        var sameRarity = PileType.Discard.GetPile(owner).Cards.Where(c => c.Rarity == rarity).ToList();
        if (sameRarity.Count == 0) return;

        var fromDiscard = rng.NextItem(sameRarity);
        if (fromDiscard == null) return;
        await CardPileCmd.Add(fromDiscard, PileType.Draw, CardPilePosition.Random);
    }

    private async Task PunishCocoworry(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        await PlayerCmd.GainEnergy(1m, owner);

        if (owner.Creature.CombatState is not { } combatState) return;
        var allies = combatState.Players.Where(p => p.Creature.IsAlive).ToList();
        if (allies.Count == 0) return;

        var ally = await ChooseAllyPlayer(choiceContext, owner, allies);
        if (ally == null) return;

        var allyHand = PileType.Hand.GetPile(ally).Cards.ToList();
        if (allyHand.Count == 0) return;

        var maxRarity = allyHand.Max(c => (int)c.Rarity);
        await GiveRandomRarityCard(combatState, owner, (CardRarity)maxRarity);
        await GiveRandomRarityCard(combatState, ally, (CardRarity)maxRarity);
    }

    private async Task ForgiveArisaGuilt(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;

        card.DynamicVars["MagicAmount"].BaseValue += 1m;
        await PowerCmd.Apply<OriginalsinArisaMagicBurstPower>(
            choiceContext, owner.Creature, 1m, owner.Creature, card);
    }

    private async Task PunishArisaGuilt(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var maxHp = owner.Creature.MaxHp;
        if (maxHp <= 0) return;

        await PowerCmd.Apply<YlsmPower>(choiceContext, owner.Creature, maxHp, owner.Creature, card);
        var energy = (int)(maxHp / 10);
        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, owner);
    }

    private async Task ForgiveMiliaLost(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var canonical = card.CanonicalInstance;

        await CardPileCmd.RemoveFromCombat(card);

        var power = owner.Creature.GetPower<OriginalsinMiliaReturnPower>();
        if (power is null)
            power = await PowerCmd.Apply<OriginalsinMiliaReturnPower>(
                choiceContext, owner.Creature, 1m, owner.Creature, card);
        power?.Store(canonical);
    }

    private async Task PunishMiliaLost(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        if (owner.Creature.CombatState is not { } combatState) return;

        var allies = combatState.Players.Where(p => p.Creature.IsAlive && p != owner).ToList();
        if (allies.Count == 0)
            allies = combatState.Players.Where(p => p.Creature.IsAlive).ToList();
        if (allies.Count == 0) return;

        var ally = await ChooseAllyPlayer(choiceContext, owner, allies);
        if (ally == null) return;

        await CardPileCmd.GiveToAnotherPlayer(card, ally, PileType.Hand);

        var myHand = PileType.Hand.GetPile(owner).Cards.Where(c => !ReferenceEquals(c, card)).ToList();
        var theirHand = PileType.Hand.GetPile(ally).Cards.Where(c => !ReferenceEquals(c, card)).ToList();
        if (myHand.Count == 0 || theirHand.Count == 0) return;

        var myPick = (await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1),
            c => !ReferenceEquals(c, card),
            card)).FirstOrDefault() ?? owner.RunState.Rng.CombatCardGeneration.NextItem(myHand);

        var theirPick = (await CardSelectCmd.FromHand(
            choiceContext,
            ally,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1),
            c => !ReferenceEquals(c, card),
            card)).FirstOrDefault() ?? owner.RunState.Rng.CombatCardGeneration.NextItem(theirHand);

        if (myPick != null)
        {
            var copy = combatState.CreateCard(myPick.CanonicalInstance, ally);
            copy.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, ally);
        }

        if (theirPick != null)
        {
            var copy = combatState.CreateCard(theirPick.CanonicalInstance, owner);
            copy.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, owner);
        }
    }

    private static async Task<Player?> ChooseAllyPlayer(
        PlayerChoiceContext choiceContext,
        Player owner,
        List<Player> allies)
    {
        if (allies.Count == 0) return null;
        if (allies.Count == 1) return allies[0];

        var handCards = allies.SelectMany(p => PileType.Hand.GetPile(p).Cards).ToList();
        if (handCards.Count > 0)
        {
            var selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                handCards,
                owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1, 1))).FirstOrDefault();
            if (selected?.Owner != null)
                return selected.Owner;
        }

        return owner.RunState.Rng.CombatCardGeneration.NextItem(allies);
    }

    private static async Task GiveRandomRarityCard(ICombatState combatState, Player player, CardRarity rarity)
    {
        var pool = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == rarity && c.CanBeGeneratedInCombat)
            .ToList();
        if (pool.Count == 0)
        {
            pool = player.UnlockState.CharacterCardPools
                .SelectMany(p => p.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
                .Where(c => c.Rarity == rarity && c.CanBeGeneratedInCombat)
                .ToList();
        }
        if (pool.Count == 0) return;

        var template = player.RunState.Rng.CombatCardGeneration.NextItem(pool);
        if (template == null) return;

        var generated = combatState.CreateCard(template, player);
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, player);
    }

    private async Task ForgiveWitchificationCurse(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var me = owner.Creature;

        // 宽恕的代价是「<b>下回合开始时</b>失去 10 魔女化」——卡面写死了这个时点，
        // 所以这里<b>不立刻扣</b>，只登记一次延迟效果（多次宽恕会叠加层数），
        // 真正的扣减由 OriginalsinWitchificationLossPower 在自己回合开始时执行。
        await PowerCmd.Apply<OriginalsinWitchificationLossPower>(
            choiceContext, me, 10m, me, card);

        // 宽恕的成长：此卡下次打出获得的魔女化 +10（这个是当场生效的数值增长）
        if (card.DynamicVars.TryGetValue("WitchAmount", out var witchAmount))
            witchAmount.BaseValue += 10m;
    }

    private async Task PunishWitchificationCurse(PlayerChoiceContext choiceContext)
    {
        var card = Card!;
        var owner = card.Owner!;
        var me = owner.Creature;
        if (me.CombatState is not { } combatState) return;

        // 当前每有 50 魔女化：随机减少任意友方 20 魔女化，自己抽 1 张卡
        var currentWith = me.GetPower<WithPower>()?.Amount ?? 0m;
        var times = (int)(currentWith / 50m);
        if (times <= 0) return;

        var allies = combatState.Players
            .Where(p => p.Creature.IsAlive)
            .Select(p => p.Creature)
            .ToList();
        if (allies.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        for (var i = 0; i < times; i++)
        {
            var ally = rng.NextItem(allies);
            if (ally == null) continue;

            var allyWith = ally.GetPower<WithPower>()?.Amount ?? 0m;
            if (allyWith > 0m)
            {
                var loss = Math.Min(20m, allyWith);
                await PowerCmd.Apply<WithPower>(choiceContext, ally, -loss, me, card, false);
            }

            await CardPileCmd.Draw(choiceContext, 1, owner);
        }
    }

    private static decimal Half(decimal value) => Math.Max(1m, value / 2m);
}
