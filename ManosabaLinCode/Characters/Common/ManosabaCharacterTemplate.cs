using STS2RitsuLib.Scaffolding.Characters;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaCharacterTemplate<TCardPool, TRelicPool, TPotionPool> :
    ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>
    where TCardPool : CardPoolModel
    where TRelicPool : RelicPoolModel
    where TPotionPool : PotionPoolModel
{
    public override string CharacterSelectSfx => $"event:/{ModId}/sfx/characters/{GetType().Name.ToLowerInvariant()}/select";
}
