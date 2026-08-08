using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Revokation() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int MaxHpLoss = 3;
    private const string JusticeEffectHoverLocEntry = "MANOSABA_LIN_CARD_JUSTICE_EFFECT";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new CardsVar(1)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<DeathRewind>();
            yield return HoverTipFactory.FromPower<DeathRewindPower>();
            yield return HoverTipFactory.FromCard<Justice>();
            yield return CardEffectHoverTipFactory.FromCard(
                ModelDb.Card<Justice>(),
                JusticeEffectHoverLocEntry);
            yield return HoverTipFactory.FromCard<Save>();
            yield return HoverTipFactory.FromPower<JusticePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        // 降低 3 点血量上限
        await CreatureCmd.LoseMaxHp(choiceContext, source.Owner.Creature, MaxHpLoss, true);

        // 攻击目标
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var player = source.Owner;
        var cardRemoved = false;
        var powerRemoved = false;

        // 从手牌、弃牌堆、抽牌堆中选 DeathRewind 移除
        var combatDeathRewinds = new[] { PileType.Hand, PileType.Discard, PileType.Draw }
            .SelectMany(p => p.GetPile(player).Cards)
            .Where(c => c is DeathRewind)
            .ToList();

        if (combatDeathRewinds.Count > 0)
        {
            var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 0, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext, combatDeathRewinds, player, prefs
            );

            var card = selected.FirstOrDefault();
            if (card != null)
            {
                await CardPileCmd.RemoveFromCombat(card);
                cardRemoved = true;
            }
        }

        // 移除自身 DeathRewindPower
        if (source.Owner.Creature.GetPower<DeathRewindPower>() != null)
        {
            await PowerCmd.Remove<DeathRewindPower>(source.Owner.Creature);
            powerRemoved = true;
        }

        // 战斗结束后移除牌组中所有 DeathRewind
        CombatManager.Instance.CombatEnded += OnCombatEnded;
        async void OnCombatEnded(CombatRoom room)
        {
            CombatManager.Instance.CombatEnded -= OnCombatEnded;

            var deckCards = PileType.Deck.GetPile(player).Cards
                .Where(c => c is DeathRewind).ToList();
            foreach (var c in deckCards)
                await CardPileCmd.RemoveFromDeck(c, showPreview: false);
        }

        // 如果移除了卡或能力，获得 Justice
        if (cardRemoved || powerRemoved)
        {
            var justice = source.CombatState.CreateCard<Justice>(source.Owner);
            justice.SetToFreeThisTurn();
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(justice, PileType.Hand, source.Owner)
            );
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
