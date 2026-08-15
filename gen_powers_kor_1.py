# -*- coding: utf-8 -*-
import json, io, re

texts = []
with io.open(r'D:\ManosabaLin\uniq_powers.txt', encoding='utf-8-sig') as f:
    content = f.read()
blocks = re.split(r'### \[\d+\] ', content)
for b in blocks:
    if not b.strip():
        continue
    lines = b.split('\n')
    zh_lines = []
    for ln in lines:
        if ln.startswith('  keys:'):
            break
        zh_lines.append(ln)
    zh = '\n'.join(zh_lines).strip()
    if zh:
        texts.append(zh)
assert len(texts) == 316, len(texts)

kor_1 = [
    "[b]턴 시작[/b] 시 [b]【마녀화】[/b] 40스택 획득",
    "[b]매 턴[/b] [b]【혐의】[/b]가 있는 적에게 카드를 낼 때, 다른 무작위 적 1명에게 [b]【혐의】[/b] 1스택 부여. 턴당 최대 2회 발동",
    "[b]마녀 재판, 개정한다![/b]\n본 능력 2스택마다 [b]힘[/b] 1 감소.\n아군이 12스택 보유 시 [b]【배드엔딩】[/b] 획득.\n적이 12스택 보유 시 긍정 능력 상실, 턴 종료 시 반환",
    "[color=#6666cc]【빈 페이지】[/color]가 카드를 생성할 때, 선택지 {Amount}개 추가 등장",
    "[color=#6666cc]이번 턴[/color] 3번째 카운트 카드를 낸 후 손패로 돌아오고, 비용이 0이 되며, 다음 사용 시 소모.",
    "[color=#6666cc]이번 턴[/color] 카운트 카드 3장을 낼 때마다 [color=#6666cc]카드[/color] 1장 드로우.\n이 능력 획득 시 [color=#6666cc]안심[/color] 1스택마다, 카운트 카드 2장을 낼 때마다 다음 카운트 카드 비용이 해당 수치만큼 감소",
    "[color=#6666cc]이번 턴[/color] 다음 비[b]공격[/b] 카드를 낸 후, 적의 공격 의도가 없으면 [color=#99ccff]【안심】[/color] 1스택 획득 및 카드 드로우",
    "[color=#6666cc]이번 턴[/color] 다음에 지정된 장수의 같은 종류 카드를 연속으로 내면, [color=#99ccff]【안심】[/color] 1스택 획득, 카드 더미에서 다른 종류의 카드를 가져올 수 있음.",
    "[color=#6666cc]이번 턴[/color] 다음에 치명적 피해를 받으면, 대신 체력 1로 생존하고 페이지 획득.\n발동하지 않으면 [color=#6666cc]턴 종료[/color] 시 [color=#6666cc]【빈 페이지】[/color] 1장 획득.",
    "[color=#6666cc]턴 종료[/color] 시, 이번 [color=#6666cc]턴[/color] [b]공격 카드[/b]를 내지 않았다면 [color=#6666cc]【침묵】[/color] {Amount}스택 획득",
    "[color=#6666cc]턴 시작[/color] 시, 손패 1장을 선택할 수 있음; 이후 무작위 캐릭터 카드 풀이 지정되고, 그 풀에서 카드 1장을 골라 손패 카드를 변형할 수 있음. 해당 풀이 [color=#6666cc]안안의 스케치북[/color]에 기록되었거나 이 효과로 기록에 성공하면, 변형된 카드를 1장 복사해 덱에 추가. 스케치북이 이미 3개 풀을 기록했다면, 먼저 기록된 풀에 속한 덱의 카드 1장을 제거하고 그 풀을 잊을 수 있음.",
    "[color=#6666cc]턴 시작[/color] 시, 무작위 캐릭터 카드 풀이 지정되고, 손패 1장을 다시 쓸 수 있음; 해당 풀이 기록되었거나 기록에 성공하면 덱에 복사.",
    "[color=#6666cc]턴 시작[/color] 시 [color=#cc99ff]【오디션】[/color] 발동, 현재 [color=#99ccff]【안심】[/color] 스택에 따라 무작위 효과 발동.\n1: 기록된 카드 풀의 임시 0비용 소모 카드 1장 생성\n2: [color=#6666cc]【빈 페이지+】[/color] 획득\n3: [color=#6666cc]【침묵】[/color] {RequiredSilenceCost:diff()}스택 소모 후 [color=#6666cc]【세뇌】[/color]의 재작성 발동, 침묵 부족 또는 재작성 불가 시 [color=#6666cc]【빈 페이지】[/color] 획득\n4: [color=#99ccff]카드[/color] 1장 드로우 및 무작위 손패 카드가 사용 후 [color=#99ccff]【안심】[/color] 1스택 획득\n출처 카드가 강화되었다면, 처음 랜덤으로 나온 효과 1회 추가 발동",
    "[color=#6666cc]매 턴[/color] [color=#6666cc]【침묵】[/color] 첫 소모 시 {energyPrefix:energyIcons(1)} 획득, 이후에는 [color=#6666cc]카드[/color] 1장 드로우로 변경.\n4회마다 반복",
    "[color=#6666cc]매 턴[/color] 첫 [b]스킬 카드[/b]는 드로우 더미 맨 위에 놓이고, 사용 전까지 비용 -1.\n[color=#6666cc]턴 종료[/color] 시 [color=#6666cc]【침묵】[/color] {Amount}스택 획득 및 손패 최대 1장 유지.\n[b]공격 카드[/b]를 내면 [color=#6666cc]【감방】[/color]에서 나감.",
    "[color=#6666cc]매 턴[/color] 각 동료가 처음으로 [b]희귀[/b] 카드를 내면, 비용 1의 임시 복사본 1장을 손패에 추가. 그것은 [b]소모[/b]를 획득.\n이 능력이 강화 카드에서 왔다면, 복사본 비용은 0.\n구상 복사본 카드를 낸 후 [b]힘[/b] 1스택과 [b]민첩[/b] 1스택 획득.",
    "[color=#6666cc]다음 턴 시작[/color] 시, 약속된 카드 {Amount}장을 손패로 돌리고 이번 [color=#6666cc]턴[/color] 무료로 1회 사용 가능하게 함; 이후 그 카드 풀에서 이번 [color=#6666cc]턴[/color] 무료 1회 사용 가능하고 [b]허무[/b]와 [b]소모[/b]를 가진 카드 1장 생성.",
    "[color=#6666cc]다음 턴 시작[/color] 시, 임시 보관 카드 {Amount}장을 손패로 반환.",
    "[color=#999999]턴 시작[/color] 시 버린 카드 더미에서 무작위 카드 1장 획득 후 1스택 감소",
    "[color=#CC6666][b]선택[/b][/color]",
    "[color=#CC6666]턴 종료[/color] 시 체력 {Amount} 회복.",
    "[color=#CC6666]턴 종료[/color] 시\n[color=#CC6666]【위증】[/color] 1스택 소모 후 [color=#CC6666]【정의】[/color] 1스택 획득\n[b]【혐의】[/b] 1스택 소모 후 [b]【마녀화】[/b] 2스택 획득",
    "[color=#CC6666]턴 종료[/color] 시 [color=#CC6666]【정의】[/color]의 회복 효과 1회 추가 발동",
    "[color=#CC6666]턴 종료[/color] 시 [color=#CC6666]체력[/color] 회복 후 1스택 감소.",
    "[color=#CC6666]턴 시작[/color] 시 [color=#CC6666]【위증】[/color] 1스택 획득",
    "[color=#CC6666]턴 시작[/color] 시 [color=#CC6666][b]【소원】[/b][/color]＋1, 손패 1장을 무작위 [color=#CC6666][b]【소원】[/b][/color] 카드로 변형",
    "[color=#CC6666]턴 시작[/color] 시 {energyPrefix:energyIcons(1)} 획득",
    "[color=#CC6666]턴 시작[/color] 시 [color=#CC6666]【정의】[/color] 2스택 획득",
    "[color=#CC6666]턴 시작[/color] 시 [color=#CC6666]【정의】[/color] 1스택 상실, {energyPrefix:energyIcons(1)} 획득",
    "[color=#cc9966]카드 사용 금지[/color] 손패 교환 종료까지",
    "[color=#cccccc]턴 시작[/color] 시 [color=#cccccc]힘[/color] 1, [color=#CC6666]에너지[/color] 1, [color=#ff99cc]카드[/color] 1장 획득",
    "[color=#cccccc]턴 시작[/color] 시 [color=#ff99cc][b]【친밀】[/b][/color]과 [color=#CC6666][b]【소원】[/b][/color]의 수치 교환",
    "[color=#cccccc]다음 턴 시작[/color] 시 [color=#CC6666]【정의】[/color] 1스택 획득",
    "[color=#ff0000][b]【친밀】[/b][/color] 증가 시 [color=#ff99cc]동일한 에너지[/color]와 [color=#ff0000]【후지 아리사의 마법】[/color] 획득\n[color=#ff0000][b]【소원】[/b][/color] 증가 시 동일한 [color=#ff0000]【후지 아리사의 마법】[/color] 소모 후 동일한 수만큼 [color=#ff99cc]카드[/color] 드로우",
    "[color=#ff0000]턴 종료[/color] 시 체력 1 상실 후 제거",
    "[color=#ff9966]턴 시작[/color] 시, 필드 적의 의도에 따라 대응 버프 획득\n공격: 방어도 5 획득\n방어/버프: 힘 1 획득\n디버프: 민첩 1 획득\n기타: 무작위 적에게 약화 1스택",
    "[color=#ff99cc][b]「친[/b][/color][color=#CC6666][b]밀」[/b][/color]\n같은 카드를 2번째로 내면, 그 카드는 「마녀의 기억」 표식을 획득하고 [color=#ff99cc]재생[/color]과 [color=#CC6666]사용 시 소모[/color]를 획득. 카드 이름마다 [color=#CC6666]1회만 발동[/color]\n카드 13장 제거 시(우클릭 또는 두 손가락 탭으로 확인 가능), 덱의 【엔딩】에 [color=#ff99cc]「히로 본인이 울면서 나를 탓하다니」[/color] 1스택 부여",
    "[color=#ff99cc][b]선택[/b][/color]",
    "[color=#ff99cc]수동 카드 사용 불가[/color]\n풀피 상태에서 본 능력과 버퍼 제거, 3턴 후 풀피가 아니면 다시 사망",
    "[color=#ff99cc]클릭하여 선택[/color]한 적이 [color=#ff99cc]체력 4분의 1 상실[/color], 1스택 상실 및 덱에서 [color=#CC6666]「어쩔 수 없지, 곁에 있어 줄게」[/color] 1스택 제거",
    "[color=#ff99cc]턴 시작[/color] 시, 덱 내 모든 [color=#339966]【[/color][color=#cc9966]심[/color][color=#6699cc]판[/color][color=#339966]】[/color] 부여 제거, 제거 수만큼 무작위 부여 1종을 골라 동일한 수의 무부여 카드에 부여, 해당 키워드 카드 1장을 생성해 그 부여를 붙여 손패에 추가",
    "[color=#ff99cc]턴 시작[/color] 시, [color=#CC6666]【소원】[/color] 2스택 소모 후 손패 카드를 [color=#CC6666]【소원】[/color] 카드로 변형, 또는 [color=#ff99cc]【친밀】[/color] 2스택 소모 후 [color=#ff99cc]【친밀】[/color] 카드 생성, 모두 무료 1회 사용 가능",
    "[color=#ffcc99]턴 시작[/color] 시 체력 30 회복",
    "[color=CC6666]사망[/color] 시 부활하며 체력 40 회복, 이후 1스택 상실",
    "[color=ff99cc]매 턴[/color] [color=#339966]【[/color][color=#cc9966]심[/color][color=#6699cc]판[/color][color=#339966]】[/color] 부여 카드를 5의 배수만큼 내면, 체력이 가장 낮은 적에게 [color=#ff99cc]피해[/color] 15",
    "[color=ff99cc]매 턴 시작[/color] 시\n[color=#339966]【[/color][color=#cc9966]심[/color][color=#6699cc]판[/color][color=#339966]】[/color] 부여 카운터 +1",
    "[color=ff99cc]매 턴 시작[/color] 시, [color=#ff99cc]드로우 더미[/color] 맨 위 {Stacks:diff()}장을 확인, 버리거나 되돌릴 수 있음",
    "[color=ff99cc]선택[/color]",
    "[jitter][color=#CC6666]「이 세계에서 나는 감옥을 떠도는 잔해가 되었다————」[/color][/jitter]\n의도를 실행할 때마다, 스택마다 20% 확률로 같은 의도를 1회 추가 실행",
    "[jitter][color=#cccccc]「추함의 화신——불멸의 괴물 【마녀】가 되어, 끝없는 절망에 빠진다」[/color][/jitter]\n[color=gray]세 번째 의도[/color]의 공격 후 모든 플레이어가 [color=gray]【변신 마법】[/color] 카드를 플레이어 수의 3배만큼 보유하면, 다음 턴 [color=gray]추가[/color]로 1회 더 공격\n턴마다 첫 사망 시 사망을 방지하고, 체력이 무한이 되며 마녀화 3배만큼의 [b]방어도[/b] 획득, 의도 전환 [color=gray](1턴 1회)[/color]; 방어도가 깨지지 않았다면, 다음 턴 시작 시 최대 체력이 전투 시작 수치로 회복되고 체력 절반 회복.",
    "[jitter][color=#cccccc]「제2막 개막」[/color][/jitter]\n체력이 절반으로 떨어지면, 마녀화 2배만큼의 방어도 획득 후 의도 전환.",
    "[jitter][color=#cccccc]「마녀의 섬이 다시 한번 하늘을 난다」[/color][/jitter]\n체력이 절반으로 떨어지면 마녀화 2배의 보호막 획득, 의도 전환",
    "[jitter][color=#cccccc]「이성을 잃고 잔해가 된 너는, 과거로 돌아가도 모두를 죽일 뿐이다」[/color][/jitter]\n사망 시 부활: 모든 플레이어 풀피 회복 및 부정 능력 제거; 자신은 다른 부정 능력 제거, 최대 체력 증가, 풀피 회복 및 방어도 50 획득\n공격받아 체력 손실 시 20% 확률로 공격자에게 [b]정보[/b] 1스택 부여, 사망할 때마다 확률 10% 증가, 턴당 최대 5회 발동",
    "[jitter][color=#cccccc]「【마녀】를 죽이기 위해, 같은 효과를 가진 약을 개발한 모양이다......」[/color][/jitter]\n13스택 달성 시 이 능력 제거 및 아군이 [b]서틴 워터즈[/b] 1장 획득",
    "[purple]이번 턴[/purple] 동료가 받는 피해를 대신 받음.\n[purple]다음 턴[/purple] 보호한 동료에게 받은 피해만큼의 보호막 부여. 친밀 > 소원이면, 당신도 받은 피해만큼의 보호막 획득",
    "[purple]턴 시작[/purple] 시 사용 가능한 손패 1장을 무작위로 사용 후 1스택 제거",
    "「가짜 사망」",
    "「히로 본인이 울면서 나를 탓하다니」",
    "「진 엔딩」",
    "【마녀화】 증가 시 능력의 영향을 받을 수 있는 무작위 대상에게 동일한 피해",
    "100스택 시 마법 획득",
    "Boss가 의도를 실행할 때, 환영이 의도 1개 추가 실행",
    "에마의 협력",
    "안심",
    "마그의 마법",
    "표식이 있는 적이 다음에 당신에게 피해를 주면, [color=#6666cc]피해[/color] 최대 {Amount}만큼 [blue]【붉은 나비】[/blue] 치유로 기록. 이미 [blue]【붉은 나비】[/blue]가 있다면 추가되고, 없으면 [color=#6666cc]다음 턴 시작[/color] 시 치유.",
    "기록된 카드를 내면, 추가 방어도 획득.",
    "기록된 카드를 [color=#6666cc]다음 턴[/color]에 내면, [color=#99ccff]【안심】[/color] 1스택 획득.",
    "이번 전투 마녀화 획득 불가, 대신 폭풍 기록. 턴 시작 시 폭풍 40 정리마다 에너지 1 + 카드 1장 + 무작위 기본 감정 획득",
    "이번 전투에서 이미 [color=#6666cc]【세뇌 반동】[/color] 감소됨",
    "이번 전투에서 적의 의도가 재작성될 때마다 1회 기록.\n[color=#6666cc]턴 시작[/color] 시 최소 3회 기록되었다면, 기록 최대 {Amount}회 소모: 동일한 수만큼 [color=#6666cc]카드[/color] 드로우 및 {energyPrefix:energyIcons(1)} 획득",
    "이번 턴 [color=#6666cc]【침묵】[/color] 또는 [color=#6666cc]【세뇌】[/color]를 우클릭 또는 두 손가락 탭으로 무료 사용 가능.[blue]턴 종료[/blue] 시 이번 무료 사용으로 재작성된 적에게 {RewrittenNymPower:diff()}스택의 [blue]【키노사키 노아의 마법】[/blue] 부여; 재작성한 적이 없으면 모든 적에게 {NoRewriteNymPower:diff()}스택 부여.",
    "이번 턴 카드 1장을 낼 때마다 감정 카운트 추가 +1.",
    "이번 턴 아군이 카드 1장을 드로우하고 {energyPrefix:energyIcons(1)}을 회복할 때마다 그에게 정의 1스택 부여",
    "이번 턴 내 임의 심판 부여 카운트가 5에 도달하면 고정, 이후 이 부여 카운트의 추가 효과를 아군도 발동 가능",
    "이번 턴 {DamageTarget} 이상의 피해를 입히고 {BlockTarget} 이상의 방어도를 획득.\n성공: 방어도 20 획득.\n실패: Boss가 [b]마녀화[/b] 10스택과 방어도 20 획득, 임무 실패 1회 기록(다음 공격은 실패 1회마다 추가 피해)",
    "이번 턴 [b]활력[/b]이 감소하려는 다음 {Amount}회는 감소하지 않음.",
    "얼음 위의 메루루의 마법",
    "더 이상 회피하지 않는다",
    "무응답",
    "잔향",
    "초안 재작성",
    "폭탄 해체 지지",
    "적에게서 받는 피해 2배. 적의 피해가 아군에게 절반\n누적 공격 피해 20마다 자신은 다음 턴 에너지 1스택, 다른 동료는 각각 다음 턴 드로우 1스택과 마녀화 10스택 획득",
    "키노사키 노아의 마법",
    "충격파",
    "초생 감정",
    "뒤틀린 기억",
]

assert len(kor_1) == 88, len(kor_1)
mapping = {texts[i].replace('\n', '\\n'): kor_1[i].replace('\n', '\\n') for i in range(88)}
with io.open(r'D:\ManosabaLin\map_powers_kor_1.json', 'w', encoding='utf-8') as f:
    json.dump({"kor/powers.json": mapping}, f, ensure_ascii=False, indent=2)
print("kor powers 批次1 完成: 88 条")
