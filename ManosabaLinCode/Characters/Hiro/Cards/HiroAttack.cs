using MinionLib.Component.Core;
using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
[RegisterCharacterStarterCard(typeof(Hiro), 4)]
public class HiroAttack() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
	protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

	protected override IEnumerable<DynamicVar> CanonicalVars => new[]
	{
		new DamageVar(6, ValueProp.Move)
	};

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target);

		ManosabaAudio.TryPlayOneShot("hiro_attack.wav".CardsAudioPath(), 0.8f);

		await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
			.FromCard(this)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade(ComponentContext componentContext)
	{
		DynamicVars.Damage.UpgradeValueBy(2m);
	}
}
