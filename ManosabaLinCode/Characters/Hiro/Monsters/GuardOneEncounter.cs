using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterActEncounter(typeof(DeprecatedAct))]
public sealed class GuardOneEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<GuardOneMonster>()];

    protected override bool UseActCombatBackground => false;

    public override string CustomBackgroundScenePath => this.BackgroungScenePath;

    public override string CustomBackgroundLayersDirectoryPath => this.BackgroundLayersDirectoryPath;

    public override string? CustomRunHistoryIconPath => this.RunHistoryIconPath;

    public override string? CustomRunHistoryIconOutlinePath => this.RunHistoryIconOutlinePath;

    public override string BossNodePath => this.BossMapNodePath;

    public override MegaSkeletonDataResource? BossNodeSpineResource => null;

    public override RoomType RoomType => RoomType.Boss;

    public override string CustomBgm => "event:/ManosabaLin/music/GuardOne";

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<GuardOneMonster>().ToMutable(), null)
    ];
}
