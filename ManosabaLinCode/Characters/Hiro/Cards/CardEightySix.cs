using Godot;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardEightySix() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override PileType GetResultPileTypeForCardPlayC()
    {
        return PileType.None;
    }

    protected override async Task OnPlayPhased(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        switch (componentContext.Phase)
        {
            case ComponentPhase.Core:
                ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay));

                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
                await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
                return;
            case ComponentPhase.Final:
                var drawPileCards = PileType.Draw.GetPile(Owner).Cards
                    .Where(c => c is IComponentsCardModel)
                    .ToList()
                    .StableShuffle(Owner.RunState.Rng.Shuffle);

                var success = false;
                if (drawPileCards.FirstOrDefault() is IComponentsCardModel card)
                {
                    success = true;
                    card.AddComponent(new GenerateComponent(this));
                }

                FlyToDrawOrExhaustAnimation(cardPlay.IsLastInSeries, success);
                if (cardPlay.IsLastInSeries) await CardPileCmd.RemoveFromCombat(this, skipVisuals: true);
                return;
        }
    }

    private void FlyToDrawOrExhaustAnimation(bool isLast, bool success)
    {
        if (Pile is not { IsCombatPile: true }) return;
        if (!LocalContext.IsMine(this)) return;

        var originalNode = NCard.FindOnTable(this);
        var vfxContainer = NCombatRoom.Instance?.CombatVfxContainer;

        if (originalNode is null || vfxContainer is null) return;

        NCard? nodeToAnim;
        var originalPosition = originalNode.GlobalPosition;
        if (isLast)
        {
            nodeToAnim = originalNode;
            originalNode.GetParent()?.RemoveChildSafely(nodeToAnim);
            vfxContainer.AddChildSafely(originalNode);
        }
        else
        {
            nodeToAnim = NCard.Create(this);
            vfxContainer.AddChildSafely(nodeToAnim);
        }
        if (nodeToAnim is null) return;
        nodeToAnim.GlobalPosition = originalPosition;

        Node2D? vfx;
        if (success)
        {
            var targetPosition = PileType.Draw.GetTargetPosition(nodeToAnim);
            vfx = NCardFlyVfx.Create(nodeToAnim, targetPosition, isAddingToPile: false,
                Owner.Character.TrailPath);
        }
        else
        {
            vfx = NExhaustVfx.Create(nodeToAnim);
        }

        if (vfx != null)
        {
            vfxContainer.AddChildSafely(vfx);
            if (success) return;
            var tween = NCombatRoom.Instance?.CreateTween();
            if (tween != null)
            {
                tween.TweenProperty(nodeToAnim, "modulate", StsColors.exhaustGray, 0.2f);
                tween.Chain().TweenCallback(Callable.From(() => nodeToAnim.QueueFreeSafely()));
                tween.Play();
            }
            else
            {
                nodeToAnim.QueueFreeSafely();
            }
        }
        else
        {
            nodeToAnim.QueueFreeSafely();
        }
    }
}
