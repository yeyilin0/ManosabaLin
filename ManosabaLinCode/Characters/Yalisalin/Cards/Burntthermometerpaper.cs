using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Burntthermometerpaper()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Life", 3),
        new DynamicVar("Fire", 4),
        new DynamicVar("Consume", 2),
        new HealVar(5)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            DynamicVars["Life"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered,
            this,
            cardPlay);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        if (hairpin.IsFireColorFull(target))
        {
            await hairpin.ConsumeFireColor(choiceContext, target, DynamicVars["Consume"].IntValue, this);
            await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
            return;
        }

        await hairpin.GiveFireColor(choiceContext, target, DynamicVars["Fire"].IntValue, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
