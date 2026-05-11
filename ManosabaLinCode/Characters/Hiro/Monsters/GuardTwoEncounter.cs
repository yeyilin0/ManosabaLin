using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using ManosabaLin.Extensions;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterActEncounter(typeof(Overgrowth))]
public sealed class GuardTwoEncounter : ModEncounterTemplate
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<GuardTwoMonster>()];

    protected override bool UseActCombatBackground => false;

    public override string CustomBackgroundScenePath => this.BackgroungScenePath;

    public override string CustomBackgroundLayersDirectoryPath => this.BackgroundLayersDirectoryPath;

    public override RoomType RoomType => RoomType.Boss;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<GuardTwoMonster>().ToMutable(), null)
    ];
}
