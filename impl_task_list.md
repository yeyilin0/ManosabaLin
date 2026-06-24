# Heidemarie Implementation Tracker

Source of truth:
- Design source: `/home/magicalastrogy/workspace/cards_brief.csv`
- Clarification source: `/home/magicalastrogy/workspace/cards_clearify_2.txt`
- Repo root: `/home/magicalastrogy/workspace/ManosabaLin`
- Integration branch: `mag/heidemarie`
- Worktree root: `/home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees`
- STS2 source: `/home/magicalastrogy/workspace/Sts2`
- MinionLib source: `/home/magicalastrogy/workspace/MinionLib`
- RitsuLib source: `/home/magicalastrogy/workspace/STS2-RitsuLib`
- STS2 runtime: `/home/magicalastrogy/workspace/SlayTheSpire2`

Validation:
- Build: `STS2_SKIP_PCK_EXPORT=1 STS2_SKIP_FMOD_BUILD=1 dotnet build ManosabaLin.sln -p:Sts2Path=/home/magicalastrogy/workspace/SlayTheSpire2 -p:GodotPath= -v:minimal`
- List tests: `STS2_SKIP_PCK_EXPORT=1 STS2_SKIP_FMOD_BUILD=1 dotnet msbuild ManosabaLin.Tests/ManosabaLin.Tests.csproj -restore -t:ListSts2Tests -p:Sts2Path=/home/magicalastrogy/workspace/SlayTheSpire2 -p:GodotPath=`
- Focused tests: `STS2_SKIP_PCK_EXPORT=1 STS2_SKIP_FMOD_BUILD=1 dotnet msbuild ManosabaLin.Tests/ManosabaLin.Tests.csproj -restore -t:RunSts2Tests -p:Sts2Path=/home/magicalastrogy/workspace/SlayTheSpire2 -p:GodotPath= -p:Sts2TestArgs=--sts2-test-filter=<filter>`
- Test guidance: 测试优先测试机制，不绑定具体数值；占位数值 `1(2)` 可用 1/2 实现，但测试应关注状态转换、组件、牌堆移动、触发源和不软锁。
- Baseline smoke: `CharacterSmokeTests.Hiro_loads_and_can_play_starter_attack` passed with `SUMMARY total=1 passed=1 failed=0 skipped=0`.

Coordination rules:
- Only Main edits this tracker.
- Workers must not revert unrelated files and must not edit this tracker.
- Localized text is preallocated by assigned lane. A worker may only fill the localization placeholders for its lane or its exact assigned card keys.
- Shared mechanics must be implemented before dependent cards. Public card traits should use MinionLib `CardComponent` where practical.
- Single-card workers stop and report `blocked` if a required shared mechanic is missing.

Status values: `todo`, `in_progress`, `ready`, `done`, `blocked`.
Review values: `pending`, `pass`, `fail`, `n/a`.

## Tasks

| ID | Name | Type | Status | Branch | Worktree | Agent | Test result | Review | Blocked reason |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| MECH-001 | Heidemarie character, pool, localization placeholders | shared | ready | mag/heidemarie-mech-character | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-character | Dewey 019efa2f-0503-7603-bf6a-01843197bbca | build pass; Heidemarie_loads_basic_combat_setup pass; commit 37c0ccb | pending |  |
| MECH-002 | Rest, Link, Bounce, Sword Grave components | shared | ready | mag/heidemarie-mech-components | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-components | Hypatia 019efa2f-6137-7f72-89ca-0fffcc87e48e | build pass; HeidemarieMechanicComponentTests 5/5 pass; commit 38a4172 | pending |  |
| MECH-003 | Aurora Chain and Mark powers/hooks | shared | in_progress | mag/heidemarie-mech-powers-generation | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-powers-generation | Singer 019efa2f-c0ec-74c0-a7c8-6db98b58d101 |  | pending |  |
| MECH-004 | Sword generation helpers and generation replacement events | shared | in_progress | mag/heidemarie-mech-powers-generation | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-powers-generation | Singer 019efa2f-c0ec-74c0-a7c8-6db98b58d101 |  | pending |  |
| TOK-001 | Aurora Sword | token | todo |  |  |  |  | pending |  |
| TOK-002 | Crimson Sword | token | todo |  |  |  |  | pending |  |
| 001 | Eternal Rest Forging | card | todo |  |  |  |  | pending |  |
| 002 | Unfold Aurora | card | todo |  |  |  |  | pending |  |
| 003 | Battlemark Bond | card | todo |  |  |  |  | pending |  |
| 004 | Aurora Crimson Twinbirth | card | todo |  |  |  |  | pending |  |
| 005 | Sword Rain | card | blocked |  |  |  |  | n/a | Clarification still asks review for non-draw hand entry trigger. |
| 006 | Swordlight | card | todo |  |  |  |  | pending |  |
| 007 | Chain Sigil Ignition | card | todo |  |  |  |  | pending |  |
| 008 | Linked Edge | card | todo |  |  |  |  | pending |  |
| 009 | Crimson Edge Return Pact | card | todo |  |  |  |  | pending |  |
| 010 | Glimmer Harvest | card | todo |  |  |  |  | pending |  |
| 011 | Ember Tether Draw | card | todo |  |  |  |  | pending |  |
| 012 | Hero of the Many | card | todo |  |  |  |  | pending |  |
| 013 | Lingering Aurora Link | card | todo |  |  |  |  | pending |  |
| 014 | Old Blade Binding | card | todo |  |  |  |  | pending |  |
| 015 | Thousand Aurora Shatterstrike | card | todo |  |  |  |  | pending |  |
| 016 | Twin Edge Slumber | card | todo |  |  |  |  | pending |  |
| 017 | Ray of Light | card | todo |  |  |  |  | pending |  |
| 018 | Condensed Aurora | card | todo |  |  |  |  | pending |  |
| 019 | Liberated Aurora | card | todo |  |  |  |  | pending |  |
| 020 | World Purging Aurora | card | todo |  |  |  |  | pending |  |
| 021 | Shattered Aurora | card | todo |  |  |  |  | pending |  |
| 022 | Sword Curtain | card | todo |  |  |  |  | pending |  |
| 023 | Prism Chain | card | todo |  |  |  |  | pending |  |
| 024 | Slumber Brand | card | todo |  |  |  |  | pending |  |
| 025 | Ember Recall | card | todo |  |  |  |  | pending |  |
| 026 | Restlight Bulwark | card | todo |  |  |  |  | pending |  |
| 027 | Reforge | card | todo |  |  |  |  | pending |  |
| 028 | Link Pattern Reweaver | card | todo |  |  |  |  | pending |  |
| 029 | Citadel Aurora Release | card | todo |  |  |  |  | pending |  |
| 030 | Crimson Edge Sleep Pact | card | blocked |  |  |  |  | n/a | Clarification still conflicts on whether normal play generates Crimson Sword. |
| 031 | Returning Edge Aurora | card | blocked |  |  |  |  | n/a | Clarification still conflicts on non-bounce return trigger. |
| 032 | Formless Emberlight | card | todo |  |  |  |  | pending |  |
| 033 | Unnamed Card 33 | card | todo |  |  |  |  | pending |  |
| 034 | Unnamed Card 34 | card | todo |  |  |  |  | pending |  |
| 035 | Unnamed Card 35 | card | todo |  |  |  |  | pending |  |
| 036 | Unnamed Card 36 | card | todo |  |  |  |  | pending |  |
| 037 | Unnamed Card 37 | card | todo |  |  |  |  | pending |  |
| 038 | Unnamed Card 38 | card | todo |  |  |  |  | pending |  |
| 039 | Unnamed Card 39 | card | todo |  |  |  |  | pending |  |
| 040 | Unnamed Card 40 | card | todo |  |  |  |  | pending |  |
