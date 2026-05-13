using Godot;
using ManosabaLin.Characters.Hiro.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Acts;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Events;

[RegisterActAncient(typeof(Overgrowth))]
public class WitchoftheIsland : ManosabaAncientEventTemplate
{
    public override Color ButtonColor => new(0.6f, 0.1f, 0.2f, 0.5f);
    public override Color DialogueColor => new("CC3344");

    private IReadOnlyList<EventOption> Pool1 => [
        CreateModRelicOption<Safe>(),
        CreateModRelicOption<Bloodiedclothing>(),
        CreateModRelicOption<Fruitplate>(),
    ];

    private IReadOnlyList<EventOption> Pool2 => [
        CreateModRelicOption<Ritualsword>(),
        CreateModRelicOption<Magicsniperrifle>(),
        CreateModRelicOption<Dismantledparts>(),
    ];

    private IReadOnlyList<EventOption> Pool3 => [
        CreateModRelicOption<Externalinformation>(),
        CreateModRelicOption<Magicalsketchbook>(),
        CreateModRelicOption<Witchgrimoire>(),
    ];

    public override IEnumerable<EventOption> AllPossibleOptions => [.. Pool1, .. Pool2, .. Pool3];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            Rng.NextItem(Pool3)!,
        ];
    }
}
