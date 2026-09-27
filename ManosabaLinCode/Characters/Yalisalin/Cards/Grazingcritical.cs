using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Grazingcritical()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
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
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        // 升级后去掉「因此不再满格」的条件
        if (IsUpgraded || (wasFull && !hairpin.IsFireColorFull(target)))
            await hairpin.GiveFireColor(choiceContext, target, 1, this);
    }
}
