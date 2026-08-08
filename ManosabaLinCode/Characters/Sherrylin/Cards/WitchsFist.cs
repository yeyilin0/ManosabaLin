using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class WitchsFist() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const string TheFoolEffectHoverLocEntry = "MANOSABA_LIN_CARD_THE_FOOL_EFFECT";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<SuperStrength>();
            yield return HoverTipFactory.FromCard<TheFool>();
            yield return CardEffectHoverTipFactory.FromCard(
                ModelDb.Card<TheFool>(),
                TheFoolEffectHoverLocEntry);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var player = source.Owner;
        var cardRemoved = false;
        var powerRemoved = false;

        // 浠庢墜鐗屻€佸純鐗屽爢銆佹娊鐗屽爢涓€?SuperStrength 绉婚櫎
        var combatSuperStrengths = new[] { PileType.Hand, PileType.Discard, PileType.Draw }
            .SelectMany(p => p.GetPile(player).Cards)
            .Where(c => c is SuperStrength)
            .ToList();

        if (combatSuperStrengths.Count > 0)
        {
            var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 0, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext, combatSuperStrengths, player, prefs
            );

            var card = selected.FirstOrDefault();
            if (card != null)
            {
                await CardPileCmd.RemoveFromCombat(card);
                cardRemoved = true;
            }
        }

        // 绉婚櫎鑷韩 SuperStrengthPower
        if (source.Owner.Creature.GetPower<SuperStrengthPower>() != null)
        {
            await PowerCmd.Remove<SuperStrengthPower>(source.Owner.Creature);
            powerRemoved = true;
        }

        // 鎴樻枟缁撴潫鍚庣Щ闄ょ墝缁勪腑鎵€鏈?SuperStrength
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        async void OnCombatEnded(CombatRoom room)
        {
            CombatManager.Instance.CombatEnded -= OnCombatEnded;

            var deckCards = PileType.Deck.GetPile(player).Cards
                .Where(c => c is SuperStrength).ToList();
            foreach (var c in deckCards)
                await CardPileCmd.RemoveFromDeck(c, showPreview: false);
        }

        // 濡傛灉绉婚櫎浜嗗崱鎴栬兘鍔涳紝鑾峰緱甯︿繚鐣欑殑鎰氳€?
        if (cardRemoved || powerRemoved)
        {
            var fool = source.CombatState.CreateCard<TheFool>(source.Owner);
            fool.AddKeyword(CardKeyword.Retain);
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(fool, PileType.Hand, source.Owner)
            );
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
