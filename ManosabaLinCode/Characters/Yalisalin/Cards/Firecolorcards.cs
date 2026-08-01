using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Reversecalculation()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin)
            && hairpin.TryDowngradeFireColor(target, out var color, this))
            hairpin.GrantSealedFire(color);

        if (IsUpgraded)
            await YalisalinFireColorSystem.ConsumeFireColor(choiceContext, Owner, target, 1, this);

        YalisalinFireColorCardHelpers.ApplyHeat(Owner, target, this);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Twodifferenttestimonies()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("Hits", 2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        YalisalinFireColor? previousColor = null;
        var hits = IsUpgraded ? 3 : 2;
        DynamicVars["Hits"].BaseValue = hits;

        for (var i = 0; i < hits; i++)
        {
            await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
            var result = await YalisalinFireColorSystem.ConsumeFireColorDetailed(choiceContext, Owner, target, 1, this);
            var currentColor = result.Consumed.LastOrDefault().Color;
            if (result.Consumed.Count == 0)
                continue;

            if (previousColor != null && previousColor.Value != currentColor)
                await YalisalinFireColorSystem.ResolveExtraFireColorReward(choiceContext, Owner, currentColor, this);

            previousColor = currentColor;
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Hits"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Unusedconclusion()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnableMixedConclusion();

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Innate);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Unseenkindling()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        var hadFireColor = hairpin.TargetHasFireColor(target);
        hairpin.TryAddFireColor(target, hadFireColor ? 1 : 2, this);

        if (hadFireColor)
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        if (IsUpgraded)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        YalisalinFireColorCardHelpers.ApplyHeat(Owner, target, this);
    }

}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Afterschooltestburn()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
        await YalisalinFireColorSystem.ConsumeFireColor(choiceContext, Owner, target, 1, this);

        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin)
            && hairpin.TargetHasFireColor(target)
            && hairpin.TryDowngradeFireColor(target, out var color, this))
            await hairpin.ResolveExtraFireColorReward(choiceContext, color, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Ashinpages()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        if (hairpin.HasAnySealedFire())
        {
            hairpin.TryCopySealedFire();
            return;
        }

        if (hairpin.TryGetEarliestFireColor(target, out var color))
            hairpin.GrantSealedFire(color);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Burntthermometerpaper()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Life", 2), new DynamicVar("Consume", 3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars["Life"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            this,
            cardPlay);

        if (hairpin.IsFireColorFull(target))
        {
            await hairpin.ConsumeFireColor(choiceContext, target, DynamicVars["Consume"].IntValue, this);
        }
        else
        {
            while (!hairpin.IsFireColorFull(target) && hairpin.TryAddFireColor(target, 1, this))
            {
            }
        }

        YalisalinFireColorCardHelpers.ApplyHeat(Owner, target, this, strong: true);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Dontcooldown()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
        {
            hairpin.EnablePreserveHighestAtTurnStart();
            hairpin.EnablePreserveHighestRewriteEnergy();
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Grazingcritical()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        var wasFull = hairpin.IsFireColorFull(target);
        await hairpin.ConsumeFireColor(choiceContext, target, 1, this);
        if (wasFull && !hairpin.IsFireColorFull(target))
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            hairpin.TryAddFireColor(target, 1, this);
        }

        YalisalinFireColorCardHelpers.ApplyHeat(Owner, target, this, hairpin.IsFireColorFull(target));
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Deadlinehandoff()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("Repeats", 3),
        new DynamicVar("Consume", 2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        var consume = IsUpgraded ? 3 : 2;
        DynamicVars["Consume"].BaseValue = consume;

        for (var i = 0; i < DynamicVars["Repeats"].IntValue; i++)
        {
            if (!hairpin.IsFireColorFull(target))
            {
                await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
                hairpin.TryStrongConvertFireColor(target, this);
                continue;
            }

            await hairpin.ConsumeFireColor(choiceContext, target, consume, this);
        }

        YalisalinFireColorCardHelpers.ApplyHeat(Owner, target, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Consume"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Temperatureproof()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7, ValueProp.Move),
        new EnergyVar(1),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        var full = hairpin.IsFireColorFull(target);
        var fullChanged = false;
        void TrackFullChange()
        {
            var now = hairpin.IsFireColorFull(target);
            if (now == full)
                return;

            fullChanged = true;
            full = now;
        }

        await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
        TrackFullChange();

        var downgraded = hairpin.TryDowngradeFireColor(target, out var downgradedColor, this);
        TrackFullChange();

        var consumed = await hairpin.ConsumeFireColorDetailed(choiceContext, target, 1, this);
        TrackFullChange();

        if (downgraded
            && consumed.Consumed.Count > 0
            && downgradedColor != consumed.Consumed.Last().Color)
        {
            hairpin.TryAddFireColor(target, 1, this);
            TrackFullChange();
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        if (IsUpgraded)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        YalisalinFireColorCardHelpers.ApplyHeat(Owner, target, this, fullChanged);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Ticketonwindow()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnableFullRefillPreserve();

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Samewrongproblem()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin)
            || !hairpin.TryGetEarliestFireColor(target, out var earliest))
            return;

        if (hairpin.TryGetLastConsumedFireColorThisTurn(out var last) && last != earliest)
        {
            await hairpin.ConsumeFireColor(choiceContext, target, 1, this);
            return;
        }

        if (hairpin.TryDowngradeFireColor(target, out _, this))
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Tomorrowburn()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Preserve", 2),
        new DynamicVar("Repeats", 2),
        new CardsVar(1),
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        hairpin.GainPreserveHighestFireColor(DynamicVars["Preserve"].IntValue);
        for (var i = 0; i < DynamicVars["Repeats"].IntValue; i++)
        {
            var result = await hairpin.ConsumeFireColorDetailed(choiceContext, target, 1, this);
            if (result.PreservedHighest.Count == 0)
                continue;

            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }

        YalisalinFireColorCardHelpers.ApplyHeat(Owner, target, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Thirteenthlistener()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnableThirteenthListening();

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Innate);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Pocketmatchbox()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new BlockVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        if (hairpin.TryMoveLastFireColorToFront(target, out var movedColor, this)
            && (!hairpin.TryGetLastConsumedFireColorThisTurn(out var last) || last != movedColor))
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        if (IsUpgraded)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
}

internal static class YalisalinFireColorCardHelpers
{
    public static async Task Attack(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        CardModel source,
        Creature target,
        decimal damage)
    {
        await DamageCmd.Attack(damage)
            .FromCard(source, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public static void ApplyHeat(Player owner, Creature target, CardModel source, bool strong = false)
    {
        if (strong)
        {
            YalisalinFireColorSystem.TryStrongConvertFireColor(owner, target, source);
            return;
        }

        YalisalinFireColorSystem.TryConvertFireColor(owner, target, source);
    }
}
