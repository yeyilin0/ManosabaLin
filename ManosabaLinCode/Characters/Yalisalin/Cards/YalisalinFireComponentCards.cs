using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Characters.Yalisalin.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Unwantedkindness()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self),
        IYalisalinFireComponentModifier
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    public async Task AfterFireComponentBurned(PlayerChoiceContext choiceContext, YalisalinFireComponentContext context)
    {
        if (context.SourceCard == this)
            await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Returnedhairribbon()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new DamageVar(5, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        var selected = await YalisalinFireComponentRules.SelectDiscardCardWithoutFireComponent(
            choiceContext,
            Owner,
            SelectionScreenPrompt);

        if (selected == null)
            return;

        await CardPileCmd.Add(selected, PileType.Hand);
        YalisalinFireComponentRules.TryAddFireComponent(selected);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(2);
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Dontlookatme()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var selected = await YalisalinFireComponentRules.SelectHandCardWithoutFireComponent(
            choiceContext,
            Owner,
            SelectionScreenPrompt,
            this);

        if (selected == null)
            return;

        if (YalisalinFireComponentRules.TryAddFireComponent(selected)
            && YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.TrackDontLookAtMe(selected, IsUpgraded);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Brokentrust()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        var selected = await YalisalinFireComponentRules.SelectHandCardWithoutFireComponent(
            choiceContext,
            Owner,
            SelectionScreenPrompt,
            this);

        if (selected == null)
            return;

        if (YalisalinFireComponentRules.TryAddFireComponent(selected))
            selected.GetOrCreateCapability<YalisalinBrokenTrustFireComponentCapability>();
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Holdmypain()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Common, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnablePainKeeper(IsUpgraded ? 2 : 1);

        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Dazzlingtolerance()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnableDazzlingTolerance();

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Burnedapology()
    : ManosabaCardTemplate(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnableBurnedApology();

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Unneededgoodchild()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.QueueUnneededGoodChild(DynamicVars.Energy.IntValue);

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Energy.UpgradeValueBy(2);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Twoseparatedends()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnableSeparatedEnds(DynamicVars.Block.IntValue, DynamicVars.Cards.IntValue);

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Beforeforgiven()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy),
        IYalisalinFireComponentModifier
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12, ValueProp.Move),
        new DynamicVar("BurnDamage", 8)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target == null)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    public async Task AfterFireComponentBurned(PlayerChoiceContext choiceContext, YalisalinFireComponentContext context)
    {
        if (context.SourceCard != this || context.BurnedCard == null)
            return;

        if (context.Target != null)
        {
            await DamageCmd.Attack(DynamicVars["BurnDamage"].BaseValue)
                .FromCard(this, context.CardPlay)
                .Targeting(context.Target)
                .Execute(choiceContext);
        }

        if (context.GetEffectiveCost(context.BurnedCard) <= 2)
            return;

        if (context.BurnedCard.Pile?.Type != PileType.Exhaust)
            return;

        var returned = context.BurnedCard.CreateClone();
        returned.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
        await CardPileCmd.AddGeneratedCardToCombat(returned, PileType.Hand, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Glasshug()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self),
        IYalisalinFireComponentModifier
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    public Task AfterFireComponentBurned(PlayerChoiceContext choiceContext, YalisalinFireComponentContext context)
    {
        if (context.BurnedCard == this && YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.QueueGlassReturn(this, DynamicVars.Block.IntValue);

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Stayingstillhurts()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            card => card != this,
            this)).FirstOrDefault();

        if (selected == null)
            return;

        await CardCmd.Exhaust(choiceContext, selected);

        if (!YalisalinFireComponentRules.HasFireComponent(selected))
        {
            await CardPileCmd.Draw(choiceContext, 1, Owner);
            return;
        }

        var fireCards = YalisalinFireComponentRules.AllCombatCards(Owner)
            .Where(YalisalinFireComponentRules.HasFireComponent)
            .ToArray();

        var source = Owner.RunState.Rng.CombatCardSelection.NextItem(fireCards);
        if (source == null)
            return;

        await YalisalinFireComponentResolver.ResolveFromCard(
            choiceContext,
            source,
            cardPlay.Target,
            countsAsManualUse: true,
            temporarySourceCostReduction: IsUpgraded ? 2 : 1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Dontbringmehome()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(10, ValueProp.Move),
        new EnergyVar(1),
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var selected = await YalisalinFireComponentRules.SelectHandCardWithoutFireComponent(
            choiceContext,
            Owner,
            SelectionScreenPrompt,
            this);

        if (selected == null)
            return;

        if (!YalisalinFireComponentRules.TryAddFireComponent(selected))
            return;

        selected.GiveSingleTurnRetain();
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.TrackBringHome(selected, DynamicVars.Energy.IntValue, DynamicVars.Cards.IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(4);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Warmthshouldnotstay()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.EnableWarmthShouldNotStay();

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
        AddKeyword(CardKeyword.Innate);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Burnedecho()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new DynamicVar("BurnBonus", 3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target != null)
        {
            var burns = YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin)
                ? hairpin.FireComponentBurnsThisTurn
                : 0;
            var damage = DynamicVars.Damage.BaseValue + burns * DynamicVars["BurnBonus"].BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        EnergyCost.AddThisTurn(1);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["BurnBonus"].UpgradeValueBy(1);
    }
}

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Fifthselfproof()
    : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy),
        IYalisalinFireComponentModifier
{
    [SavedProperty] public int ManualFireUseProgress { get; private set; }
    [SavedProperty] public int FireUseCount { get; set; }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("FireCount", 0)];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        DynamicVars["FireCount"].BaseValue = FireUseCount;
        description.Add(DynamicVars["FireCount"]);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target == null || FireUseCount <= 0)
            return;

        await DamageCmd.Attack(1)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(FireUseCount)
            .Execute(choiceContext);
    }

    public Task AfterFireComponentChoiceCompleted(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (!context.CountsAsManualUse || !context.ChoiceCompleted)
            return Task.CompletedTask;

        ManualFireUseProgress++;
        if (ManualFireUseProgress >= 5)
        {
            ManualFireUseProgress -= 5;
            FireUseCount++;
            DynamicVars["FireCount"].BaseValue = FireUseCount;
        }

        return Task.CompletedTask;
    }

    public void ModifyFireComponentRightClickQueue(YalisalinFireComponentContext context)
    {
        for (var i = 0; i < FireUseCount; i++)
        {
            context.AddRightClickRequest(new YalisalinFireRightClickRequest(
                YalisalinFireRightClickKind.FifthSelfProof,
                YalisalinFireComponentContext.Text("rightClick.fifthSelfProof.prompt"),
                this));
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
