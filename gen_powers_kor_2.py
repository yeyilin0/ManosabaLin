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

kor_2 = [
    "카드를 내면 무작위 적이 [color=#ff99cc]【마녀 인자】[/color] 3배 스택 획득",
    "대수사",
    "대마녀의 힘",
    "[color=#CC6666]【위증】[/color]이 [color=#CC6666]【정의】[/color]로 전환될 때, [color=#CC6666]카드[/color] 1장 드로우 및 에너지 1 획득",
    "카드에 [color=#CC6666]【환생】[/color]을 부여할 때, [color=#ffcc66]【진실】[/color] 자동 추가.\n[color=#ffcc66]【진실】[/color]이 자동으로 사용될 때 획득하는 에너지 1 무효.\n[color=#ffcc66]【진실】[/color] 구성 요소가 붙은 카드 3장이 자동 사용될 때마다 에너지 1 획득 및 [color=#CC6666]카드[/color] 2장 드로우.",
    "우클릭 또는 두 손가락 탭으로 [color=#6666cc]【침묵】[/color]을 사용해 몬스터를 즉시 행동시키면, 모든 [color=#6666cc]【침묵】[/color]과 [color=#9999ff]【거짓말】[/color] 제거, [color=#9999ff]【거짓말】[/color] 1스택마다 [b]활력[/b] {VigorPerLie:diff()}스택 획득,\n그리고 [color=#6666cc]【안안의 스케치북】[/color] 기록 풀에서 무작위 생성된, 현재 사용 가능한 0비용 안정 다단 [b]공격[/b] 카드 1장 획득.",
    "[color=#33ccff]이번 턴[/color] 피해를 입힐 때 스택 수만큼 추가 피해",
    "위증이 정의로 돌아오면, 새 생명을 얻는다",
    "본 능력을 가진 대상이 [b]공격 카드[/b] 피해를 받을 때, 해당 피해 수치만큼 1회 추가 피해. 공격 카드마다 최대 1스택 제거.",
    "우클릭 또는 두 손가락 탭으로 [color=#6666cc]【침묵】[/color] 13스택을 소모해 의도를 재작성한 후, 방어도 {Amount} 획득 및 [color=#6666cc]【빈 페이지】[/color] 1장을 손패에 추가.\n출처 카드가 강화되었다면 [color=#6666cc]【빈 페이지+】[/color]로 변경.",
    "카운트다운 쓰리",
    "지뢰",
    "같은 기록 의도로 인한 체력 손실 2번째에는 면역 및 제거.",
    "1번째 획득: 현재 [color=#99ccff]【안심】[/color] 1 감소.\n2번째 획득: [color=#6666cc]【빈 페이지】[/color] 1장을 드로우 더미에 추가.\n3번째 획득: 모든 [color=#99ccff]【안심】[/color] 상실, [b]【마녀화】[/b] {WithPower:diff()}스택 획득, 본 능력이 3스택 이상일 때 [color=#6666cc]【세뇌】[/color]가 [color=#6666cc]【침묵】[/color] 소모를 요구하게 됨.\n현재 [color=#6666cc]【세뇌】[/color]는 [color=#6666cc]【침묵】[/color] {NextBrainwashSilenceCost:diff()}스택 소모 필요\n이후 [color=#6666cc]【침묵】[/color]을 누적 {SilenceTaxThreshold:diff()}스택 획득할 때마다 본 능력 스택만큼의 [color=#6666cc]【침묵】[/color] 상실.",
    "교도소장의 조각상",
    "클릭하여 현재 합성 가능한 [rainbow][b]【복잡한 감정】[/b][/rainbow] 1장을 복사해 [b]【타인의 감정】[/b]에 추가하거나 취소할 수 있음. 선택 후 재료와 1스택 소모.",
    "영화 약속",
    "5스택 쌓으면 [color=#CC6666]【정의】[/color] 1스택으로 전환",
    "누적 죄",
    "나비 부적",
    "모든 손패 버리고 동일하게 26 획득",
    "멀티 루프의 재연",
    "악의의 힘",
    "니카이도 히로의 마법",
    "2회차의 정의",
    "봉인 페이지",
    "복잡한 감정",
    "이 적의 의도는 같은 효과 유형의 다른 의도로 강제 유도됨",
    "공범",
    "구상 소설가",
    "고립 무원",
    "괴력",
    "이상한 냄새",
    "생각 소모 카드 드로우",
    "쿠로베 나오카의 마법",
    "호흡 정렬",
    "나비",
    "환각",
    "문득 깨달음",
    "거짓말",
    "턴 종료 시 손패의 [color=gray][변신 마법][/color] 수가 모든 플레이어 중 최대이면, 모든 플레이어 손패의 [color=gray][변신 마법][/color] 합만큼 체력 상실.",
    "턴 종료 시 현재 손패의 3분의 1을 무작위로 사용.",
    "턴 종료 시 제거",
    "턴 종료 시 제거",
    "턴 시작 시 모든 적에게 [color=#ff99cc]【마녀 인자】[/color] 10스택 부여",
    "턴 시작 시 「충격파의 주먹바람」 1장 획득.",
    "턴 시작 시 본 능력 스택만큼 에너지 감소",
    "턴 시작 시 소모 더미에 「사자갈기 해파리」가 있으면 손패로 이동 후 자동 사용.",
    "턴 시작 시 모든 적 [color=#ff99cc]【마녀 인자】[/color] 10스택, 모든 아군 1스택 획득.\n적은 피해량만큼, 아군은 4분의 1만큼 스택 증가",
    "턴 시작 시 {energyPrefix:energyIcons(1)} 획득",
    "턴 시작 시 감정 스택 획득.",
    "턴 시작 시 스택만큼 체력 상실, 보유자가 공격받을 때 스택 절반의 추가 피해",
    "턴 시작 시 손패 1장을 선택해 소모, 드로우.",
    "턴 시작 시 드로우 더미에서 카드 1장을 선택해 소모, 비용만큼의 임시 힘 획득, {energyPrefix:energyIcons(1)} 획득.",
    "턴 시작 시 【왼손 주먹】 또는 【오른손 주먹】 무작위 획득",
    "인연",
    "[blue]이번 턴[/blue] 받은 피해 기록.\n[blue]턴 시작[/blue] 시 기록값만큼 체력 회복 후 1스택 감소",
    "[color=#ff99cc][b]【친밀】[/b][/color]과 [color=#CC6666][b]【소원】[/b][/color]의 수치 기록\n[b]「친밀」[/b] 카드를 내면 [color=#ff99cc]친밀[/color] +1\n[b]「소원」[/b] 카드를 내면 [color=#CC6666]【소원】[/color] +1",
    "부여 시 모든 적의 현재 의도 기록.\n기록된 의도의 효과를 받은 후, 같은 기록 의도의 효과 2번째에는 면역 및 이 마법 제거",
    "침묵",
    "반동 경감",
    "다음 {Amount}회 공격의 피해 50% 감소.",
    "대출 기한",
    "타치바나 셰리의 마법",
    "카드 더미 이동 기록",
    "고난의 연인",
    "감방",
    "감방이 집이다",
    "같은 종류 카드 3장 연속 사용 시 능력 획득(1회만)\n공격: 생각 소모 카드 드로우\n스킬: 춤추는 인형\n파워: 마지막 인사",
    "하스미 레이아의 마법",
    "임모",
    "임시 힘 감소",
    "임시 힘",
    "임시 민첩",
    "발각되지 않음",
    "{Amount}턴마다 【복잡한 감정】 1스택 및 무작위 기본 [rainbow][b]【감정】[/b][/rainbow] 1장 획득",
    "카드 13장을 낼 때마다 무작위 [rainbow][b]【기본 감정】[/b][/rainbow] 1장을 획득해 [b]【타인의 감정】[/b]에 추가, 이 능력을 클릭해 [rainbow][b]【감정】[/b][/rainbow] 카드를 획득할 수 있음.\n한 게임에서 [rainbow][b]【기본 감정】[/b][/rainbow] 3장 획득 시 [rainbow][b]【복잡한 감정】[/b][/rainbow] 1스택 획득해 [rainbow][b]【기본 감정】[/b][/rainbow] 융합",
    "손패 5장을 낼 때마다 손패 1장 무작위 강화.",
    "충분한 수의 카드를 낼 때마다 손패 1장과 드로우 더미 1장 무작위 강화.",
    "[color=#6666cc]지난 턴[/color] [b]공격 카드[/b]를 내지 않았다면 [color=#6666cc]이번 턴 시작[/color] 시 [color=#6666cc]【감방】[/color] 진입.\n[color=#6666cc]【감방】[/color] 진입 시 에너지 {Amount} 획득.",
    "[color=#6699cc]동의[/color] 카드를 낼 때마다 [color=#ff99cc]방어도[/color] 1 획득. 동료는 [color=#ff99cc]방어도[/color] 2 획득",
    "[color=#CC6666]【환생】[/color] 카드 10장을 낼 때마다 에너지 1 획득",
    "카드 1장을 낼 때마다 활력 1스택 획득",
    "적에게 공격 피해를 입힐 때마다 임시 힘 1스택 획득",
    "몬스터 의도를 {Rewrites:diff()}회 교체할 때마다 이번 전투 침묵 교체 의도 풀 강화",
    "[color=#99ccff]【안심】[/color]을 잃을 때마다 다음 [b]공격 카드[/b]가 2회 사용됨.\n[color=#6666cc]턴당[/color] 1회",
    "카드 소모 시 방어도 {Amount} 획득.",
    "동료가 자신의 턴에 처음으로 카드를 낼 때마다 무작위 적에게 [color=#6666cc]【이상한 냄새】[/color] {Amount}스택 부여.\n동료마다 턴당 1회씩 발동.",
    "턴 시작 시 [color=#99ccff]【안심】[/color] 스택에 따라 무작위 [color=#cc99ff]【오디션】[/color] 효과 발동.\n현재 [color=#6666cc]【세뇌】[/color] 재작성 발동 시 [color=#6666cc]【침묵】[/color] {RequiredSilenceCost:diff()}스택 소모 필요.",
    "턴마다 기록된 풀의 처음 {Amount}장 카드는 사용 후 소모되고 [color=#6666cc]카드[/color] 1장 드로우",
    "몬스터 의도를 {Rewrites:diff()}회 교체할 때마다 이번 전투 침묵 교체 의도 풀 강화",
    "{energyPrefix:energyIcons(1)} 누적 4 소모마다 [color=#6666cc]【빈 페이지】[/color] 1장을 손패에 추가.\n출처 카드가 강화되었다면 [color=#6666cc]【빈 페이지+】[/color]로 변경.",
    "이 능력 1스택마다 손패 상한 1 감소\n침식으로 전투에서 제외된 카드가 여기에 임시 보관됨. 이 능력 제거 시 이 카드들을 손패로 이동",
    "[color=#ffcc66]【진실】[/color] 구성 요소가 붙은 카드 3장이 자동 사용될 때마다 에너지 1 획득 및 [color=#CC6666]카드[/color] 2장 드로우.",
    "문틈 방어도",
    "문은 아직 잠기지 않음",
    "마녀 재판",
    "마녀화",
    "마녀 감옥",
    "마녀 킬러",
    "마녀 의식",
    "마녀 인자",
    "마녀의 힘",
    "무대 뒤의 억울함",
    "에너지 회복",
    "에너지 소모",
    "당신의 턴 시작 시, 무작위로 총 {Amount}회 버프 획득:\n에너지, 버퍼, [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n[color=ff99cc]모든 카드 소모[/color] 1장 선택 가능\n무작위 카드 1장을 [color=#ffcc99]이번 턴 무료로 설정하고 손패로 이동.[/color]",
    "당신의 턴 시작 시, 무작위로 총 {Amount}회 버프 획득:\n에너지, 버퍼, [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n[color=ff99cc]모든 카드 소모[/color] 1장 선택 가능\n무작위 카드 1장을 [color=#ffcc99]이번 턴 무료로 설정하고 손패로 이동.[/color]\n공격받을 때 방어도 무시, 적 공격 시 방어도와 피해 제한 무시.\n이번 전투 풀피로 1회 부활 가능.",
    "당신의 턴 시작 시, 무작위로 총 {Amount}회 버프 획득:\n에너지, 버퍼, [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n[color=ff99cc]모든 카드 소모[/color] 1장 선택 가능\n무작위 카드 1장을 [color=#ffcc99]이번 턴 무료로 설정하고 손패로 이동.[/color]\n스택이 높아지면 적의 긍정 효과가 계속 강화됨.",
    "당신의 턴 시작 시, 무작위로 총 {Amount}회 버프 획득:\n에너지, 버퍼, [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n[color=ff99cc]모든 카드 소모[/color] 1장 선택 가능\n무작위 카드 1장을 [color=#ffcc99]이번 턴 무료로 설정하고 손패로 이동.[/color]\n적 턴 시작 시 적이 {Mllm}스택의 [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n스택이 높아지면 적의 긍정 효과가 계속 강화됨.",
    "당신의 턴 시작 시, 무작위로 총 {Amount}회 버프 획득:\n에너지, 버퍼, [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n[color=ff99cc]모든 카드 소모[/color] 1장 선택 가능\n무작위 카드 1장을 [color=#ffcc99]이번 턴 무료로 설정하고 손패로 이동.[/color]\n적 턴 시작 시 적이 {Mllm}스택의 [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득. 매 공격마다 [color=#ff99cc]사쿠라바 에마[/color]에게 {Amount} 추가 피해, 플레이어 공격은 {MisdirectChance}% 확률로 [color=#ff99cc]사쿠라바 에마[/color]를 오인 공격, 이 피해는 치명적이지 않음.\n스택이 높아지면 적의 긍정 효과가 계속 강화됨.",
    "당신의 턴 시작 시, 무작위로 총 {Amount}회 버프 획득:\n에너지, 버퍼, [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n[color=ff99cc]모든 카드 소모[/color] 1장 선택 가능\n무작위 카드 1장을 [color=#ffcc99]이번 턴 무료로 설정하고 손패로 이동.[/color]\n적 턴 시작 시 적이 {Mllm}스택의 [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득. 매 공격마다 [color=#ff99cc]사쿠라바 에마[/color]에게 {Amount} 추가 피해, 플레이어 공격은 {MisdirectChance}% 확률로 [color=#ff99cc]사쿠라바 에마[/color]를 오인 공격, 이 피해는 치명적이지 않음.\n이 캐릭터의 현재 체력을 1로 감소; 이후 이 캐릭터가 사망하려 하면 스택이 {MaxStacks}로 변경.\n스택이 높아지면 효과가 변경됨.",
    "당신의 턴 시작 시, 무작위로 총 {Amount}회 버프 획득:\n에너지, 버퍼, [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득.\n[color=ff99cc]모든 카드 소모[/color] 1장 선택 가능\n무작위 카드 1장을 [color=#ffcc99]이번 턴 무료로 설정하고 손패로 이동.[/color]\n적 턴 시작 시 적이 {Mllm}스택의 [color=#ffcc99]【얼음 위의 메루루의 마법】[/color] 획득. 매 공격마다 [color=#ff99cc]사쿠라바 에마[/color]에게 {Amount} 추가 피해.\n스택이 높아지면 적의 긍정 효과가 계속 강화됨.",
]

assert len(kor_2) == 113, len(kor_2)
mapping = {texts[i+88].replace('\n', '\\n'): kor_2[i].replace('\n', '\\n') for i in range(113)}
with io.open(r'D:\ManosabaLin\map_powers_kor_2.json', 'w', encoding='utf-8') as f:
    json.dump({"kor/powers.json": mapping}, f, ensure_ascii=False, indent=2)
print("kor powers 批次2 完成: 113 条 (89-201)")
