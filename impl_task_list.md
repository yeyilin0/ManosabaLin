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
| MECH-001 | Heidemarie character, pool, localization placeholders | shared | done | mag/heidemarie-mech-character | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-character | Dewey 019efa2f-0503-7603-bf6a-01843197bbca | integration Heidemarie_loads_basic_combat_setup pass; commits 37c0ccb, 9859098 | pass |  |
| MECH-002 | Rest, Link, Bounce, Sword Grave components | shared | done | mag/heidemarie-mech-components | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-components | Hypatia 019efa2f-6137-7f72-89ca-0fffcc87e48e | integration HeidemarieMechanicComponentTests 5/5 pass; commit 38a4172 | pass |  |
| MECH-003 | Aurora Chain and Mark powers/hooks | shared | done | mag/heidemarie-mech-powers-generation | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-powers-generation | Singer 019efa2f-c0ec-74c0-a7c8-6db98b58d101 | integration HeidemarieMechanicTests 3/3 pass; commit 0fe7b66 | pass |  |
| MECH-004 | Sword generation helpers and generation replacement events | shared | done | mag/heidemarie-mech-powers-generation | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/mech-powers-generation | Singer 019efa2f-c0ec-74c0-a7c8-6db98b58d101 | integration HeidemarieMechanicTests 3/3 pass; commit 0fe7b66 | pass |  |
| TOK-001 | Aurora Sword | token | done | mag/heidemarie-token-aurora-sword | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/token-aurora-sword | Kierkegaard 019efa4e-b0a5-7b21-803e-7789a8ba291d | integration HeidemarieAuroraSwordTests 7/7 pass; commit 521c209 | pass |  |
| TOK-002 | Crimson Sword | token | done | mag/heidemarie-token-crimson-sword | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/token-crimson-sword | Huygens 019efa4e-bb58-7841-93aa-565d41940885 | integration HeidemarieCrimsonSwordTests 5/5 pass; commit 496f4f7 | pass |  |
| 001 | Eternal Rest Forging | card | done | mag/heidemarie-card-001-eternal-rest-forging | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-001-eternal-rest-forging | Dirac 019efa6a-3c8b-70b2-8545-391c9d721fef | integration HeidemarieEternalRestForgingTests 6/6 pass; commit 4e1b6d9 | pass |  |
| 002 | Unfold Aurora | card | done | mag/heidemarie-card-002-unfold-aurora | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-002-unfold-aurora | Zeno 019efa6a-8b41-71e0-aa7a-af8a16008d86 | integration HeidemarieUnfoldAuroraTests 4/4 pass; commit 3f3e28d | pass |  |
| 003 | Battlemark Bond | card | done | mag/heidemarie-card-003-battlemark-bond | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-003-battlemark-bond | Feynman 019efa51-2733-7bb1-9916-bc4d73407a9b | integration HeidemarieBattlemarkBondTests 3/3 pass; commits fa4f655, 4518eed | pass |  |
| 004 | Aurora Crimson Twinbirth | card | in_progress | mag/heidemarie-card-004-aurora-crimson-twinbirth | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-004-aurora-crimson-twinbirth | Nietzsche 019efa6a-e680-7580-8335-3c0a6b119a3e |  | pending |  |
| 005 | Sword Rain | card | blocked |  |  |  |  | n/a | Clarification still asks review for non-draw hand entry trigger. |
| 006 | Swordlight | card | todo |  |  |  |  | pending |  |
| 007 | Chain Sigil Ignition | card | todo |  |  |  |  | pending |  |
| 008 | Linked Edge | card | done | mag/heidemarie-card-008-linked-edge | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-008-linked-edge | Bohr 019efa51-306b-73c1-be7f-1db5116914d3 | integration HeidemarieLinkedEdgeTests 4/4 pass; commit 29904c0 | pass |  |
| 009 | Crimson Edge Return Pact | card | done | mag/heidemarie-card-009-crimson-edge-return-pact | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-009-crimson-edge-return-pact | Darwin 019efa6b-2eb0-7eb0-8fdb-f93bdc85be21 | integration HeidemarieCrimsonEdgeReturnPactTests 5/5 pass; commit 05e4002 | pass |  |
| 010 | Glimmer Harvest | card | todo |  |  |  |  | pending |  |
| 011 | Ember Tether Draw | card | todo |  |  |  |  | pending |  |
| 012 | Hero of the Many | card | todo |  |  |  |  | pending |  |
| 013 | Lingering Aurora Link | card | todo |  |  |  |  | pending |  |
| 014 | Old Blade Binding | card | todo |  |  |  |  | pending |  |
| 015 | Thousand Aurora Shatterstrike | card | todo |  |  |  |  | pending |  |
| 016 | Twin Edge Slumber | card | in_progress | mag/heidemarie-card-016-twin-edge-slumber | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-016-twin-edge-slumber | Gauss 019efa6b-7b6d-75f3-b544-3a6a1dfb766a |  | pending |  |
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
| 033 | Unnamed Card 33 | card | done | mag/heidemarie-card-033-unnamed-card-33 | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-033-unnamed-card-33 | Descartes 019efa6b-c265-76c0-9f21-bd8ec95dacd9 | integration HeidemarieUnnamedCard33Tests 6/6 pass; commit b1aed17 | pass |  |
| 034 | Unnamed Card 34 | card | done | mag/heidemarie-card-034-unnamed-card-34 | /home/magicalastrogy/workspace/manosabalin-heidemarie-worktrees/card-034-unnamed-card-34 | Carson 019efa51-3a30-7a50-9342-3323ae89dc1a | integration HeidemarieUnnamedCard34Tests 3/3 pass; commit 74657d0 | pass |  |
| 035 | Unnamed Card 35 | card | todo |  |  |  |  | pending |  |
| 036 | Unnamed Card 36 | card | todo |  |  |  |  | pending |  |
| 037 | Unnamed Card 37 | card | todo |  |  |  |  | pending |  |
| 038 | Unnamed Card 38 | card | todo |  |  |  |  | pending |  |
| 039 | Unnamed Card 39 | card | todo |  |  |  |  | pending |  |
| 040 | Unnamed Card 40 | card | todo |  |  |  |  | pending |  |
