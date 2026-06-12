using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainAmplify() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RetainCounterComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("CardCount", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var retainCards = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c != source && c.HasComponent<RetainCounterComponent>()).ToList();

        var rng = source.Owner.RunState.Rng.CombatCardSelection;
        var count = System.Math.Min(source.DynamicVars["CardCount"].IntValue, retainCards.Count);

        for (int i = 0; i < count; i++)
        {
            var idx = rng.NextInt(retainCards.Count);
            var target = retainCards[idx];
            retainCards.RemoveAt(idx);

            if (target is IComponentsCardModel ccm)
            {
                var component = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
                if (component != null)
                {
                    var counterField = typeof(RetainCounterComponent).GetField("_counter",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (counterField != null)
                    {
                        var current = (int)counterField.GetValue(component);
                        counterField.SetValue(component, current + (IsUpgraded ? 2 : 1));
                        component.GetType().GetMethod("UpdateCardValues",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.Invoke(component, null);
                    }
                }
            }
        }

        if (!IsUpgraded)
            source.TryAddComponent(new RemoveOnPlayComponent());
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    
    }
}