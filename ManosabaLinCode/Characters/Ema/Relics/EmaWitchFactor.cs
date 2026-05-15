using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Emalin;

namespace ManosabaLin.Characters.Ema.Relics;

[RegisterRelic(typeof(EmalinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Emalin.Emalin))]
public sealed class EmaWitchFactor : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new[]
            {
                new PowerVar<WithPower>("AttackGain", 20),
                new PowerVar<WithPower>("SkillLoss", 10),
                new PowerVar<WithPower>("PowerLoss", 10)
            };
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WithPower>(); }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var relic = this;

        if (cardPlay.Card.Owner != relic.Owner)
            return;

        var amount = cardPlay.Card.Type switch
        {
            CardType.Attack => relic.DynamicVars["AttackGain"].BaseValue,
            CardType.Skill => -relic.DynamicVars["SkillLoss"].BaseValue,
            CardType.Power => -relic.DynamicVars["PowerLoss"].BaseValue,
            _ => 0
        };

        // 如果是扣减，确保不会扣到负数
        if (amount < 0)
        {
            var currentWith = relic.Owner.Creature.GetPower<WithPower>();
            if (currentWith != null)
            {
                var maxReduction = Math.Min(-amount, currentWith.Amount);
                amount = -maxReduction;
            }
            else
            {
                return;
            }
        }

        if (amount != 0)
        {
            relic.Flash();

            await PowerCmd.Apply<WithPower>(
                choiceContext, relic.Owner.Creature,
                amount,
                relic.Owner.Creature,
                cardPlay.Card,
                false
            );
        }
    }
}
