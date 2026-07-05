using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterActEncounter(typeof(DeprecatedAct))]
public sealed class GuardThreeEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<GuardThreeMonster>()];

    protected override bool UseActCombatBackground => false;

    public override string CustomBackgroundScenePath => this.BackgroungScenePath;

    public override string CustomBackgroundLayersDirectoryPath => this.BackgroundLayersDirectoryPath;

    public override string? CustomRunHistoryIconPath => this.RunHistoryIconPath;

    public override string? CustomRunHistoryIconOutlinePath => this.RunHistoryIconOutlinePath;

    public override IEnumerable<string>? CustomExtraAssetPaths => [GuardThreePhaseTransitionOverlay.PhaseTwoBgPath];

    public override string BossNodePath => this.BossMapNodePath;

    public override MegaSkeletonDataResource? BossNodeSpineResource => null;

    public override RoomType RoomType => RoomType.Boss;

    public override string CustomBgm => "event:/ManosabaLin/music/GuardThree";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<GuardThreeMonster>().ToMutable(), null)
    ];
}
