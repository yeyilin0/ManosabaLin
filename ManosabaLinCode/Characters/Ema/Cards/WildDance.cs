using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Emalin;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class WildDance : ManosabaCardTemplate
{
    public WildDance() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new IntVar("DamageMultiplier", 2); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var rng = owner.RunState.Rng.CombatTargets;

        var handPile = PileType.Hand.GetPile(owner);
        var attackCards = handPile.Cards
            .Where(c => c.Type == CardType.Attack && c != this && !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList();

        if (attackCards.Count == 0) return;

        await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);

        var multiplier = (int)DynamicVars["DamageMultiplier"].BaseValue;

        foreach (var card in attackCards)
        {
            var baseDamage = card.DynamicVars?.Damage?.BaseValue ?? 6m;

            await CardCmd.AutoPlay(choiceContext, card, null);

            var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
            var allies = CombatState.Allies.Where(a => a.IsAlive).ToList();

            // 随机决定打敌人还是友方
            var roll = rng.NextInt(2);
            if (roll == 0 && enemies.Count > 0)
            {
                var target = rng.NextItem(enemies);
                await CreatureCmd.Damage(choiceContext, target, baseDamage * multiplier, ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
            }
            else if (allies.Count > 0)
            {
                var target = rng.NextItem(allies);
                var suspect = target.GetPower<SuspectPower>();
                if (suspect != null && suspect.Amount > 0)
                    await PowerCmd.ModifyAmount(choiceContext, suspect, -1, target, null, false);

                var targetPlayer = target.Player;
                if (targetPlayer != null)
                {
                    var poolCards = targetPlayer.Character.CardPool
                        .GetUnlockedCards(targetPlayer.UnlockState, targetPlayer.RunState.CardMultiplayerConstraint)
                        .ToList();

                    if (poolCards.Count > 0)
                    {
                        var template = rng.NextItem(poolCards);
                        var newCard = CombatState.CreateCard(template, targetPlayer);
                        newCard.SetToFreeThisTurn();
                        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, targetPlayer, CardPilePosition.Bottom);
                    }
                }
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["DamageMultiplier"].UpgradeValueBy(1);
    }
}
