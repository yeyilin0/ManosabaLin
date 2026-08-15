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

kor_3 = [
    "[color=#CC6666]카드[/color] 1장을 낼 때마다 [color=#CC6666]보호막[/color] 1 획득, [color=#CC6666]카드[/color] 2장을 낼 때마다 무작위 [color=#CC6666]피해[/color] 1",
    "[color=#CC6666]【환생】[/color] 카드를 1장 낼 때마다 [color=#CC6666]【위증】[/color] 2스택 획득",
    "노우드의 건축가",
    "노아의 협력",
    "방관자의 영합",
    "편집증적 정의",
    "정보",
    "감정",
    "감정의 파도",
    "완전 탐색법",
    "문틈 확인",
    "단톡방 냄새",
    "플레이어가 이번 턴 [b]공격[/b] 1장 [b]스킬[/b] 1장 [b]파워[/b] 1장을 내지 않았다면 피해 부여, 자신은 마녀화 획득",
    "[color=#6666cc]이번 턴[/color] [b]공격[/b]을 더 이상 내지 않았다면, [color=#6666cc]턴 종료[/color] 시 기록 카드의 복사본을 자동 사용.",
    "기록 카드가 [color=#6666cc]이번 턴[/color] 사용되지 않았다면, [color=#6666cc]턴 종료[/color] 시 [color=#99ccff]【안심】[/color] {Amount}스택 획득; 이후 그 카드를 내면 체력 회복.",
    "대상 체력 비율 ≤ 자신의 [b]마녀화[/b] 4분의 1이면, [color=#ff99cc]공격 카드[/color]로 [color=#ff99cc]방어도 무시 피해[/color]를 입히면 즉시 처형 및 [b]【마녀화】[/b] 10스택 획득",
    "소모되면 마녀화 50 획득, 아니면 마녀화 30 상실",
    "3회차의 결단",
    "강세 제거",
    "재판 개정",
    "심판 사슬",
    "심문 정지",
    "통제 불능의 정의",
    "사자갈기 해파리",
    "열 번의 회광",
    "감정의 방패",
    "공격 피해를 받을 때 1스택 제거, 그 미방어 피해의 50%를 다른 모든 적에게 확산. 다른 적이 없다면 [b]약화[/b] 1스택 획득.",
    "냉담함",
    "페이지의 한 귀퉁이",
    "길은 달라도 정의는 하나",
    "셋까지 세면 돼",
    "쌍생 마녀의 힘",
    "이중 승화",
    "죽음의 되감기",
    "[color=#CC6666]사망[/color] 시 [color=#CC6666]부활[/color], [b]마녀화[/b] 스택만큼의 체력 획득\n마녀화=200 시, 전투 후 덱에서 [color=#CC6666]죽음의 되감기[/color] 제거\n마녀화=300 시, 이 능력 [color=#CC6666]무효[/color]",
    "수색",
    "스택이 증가할수록 [b]피해[/b] 증가, [b]100[/b]스택 시 [b]【마법】[/b] 획득,\n[b]200스택[/b] 시 [color=#CC6666]스킬 카드[/color]와 [color=#ff99cc]파워 카드[/color]를 내면 체력 1 상실, 턴 종료 시 체력 13 상실,\n[b]300스택[/b] 시 스킬 카드가 소모를 획득하고 사용 시 [b]마녀화[/b] 100 제외 체력 상실, 이때 [color=#cccccc]공격 카드[/color]를 내면 체력 3 회복",
    "덱에서 부여가 없는 카드에 [color=#339966]【[/color][color=#cc9966]심[/color][color=#6699cc]판[/color][color=#339966]】[/color] 부여 무작위 적용",
    "색인 페이지",
    "대리인의 방패",
    "춤추는 인형",
    "왕복 광란의 공방",
    "미친 카드 추적",
    "위증",
    "너를 싫어한다는 사실은 영원히 변하지 않아",
    "[color=#99ccff]【안심】[/color] 획득 불가,\n[color=#6666cc]턴 시작[/color] 시 [color=#6666cc]카드[/color] 1장 덜 드로우, 손패 상한 절반 및 {energyPrefix:energyIcons(1)} 1 상실\n[color=#6666cc]턴 종료[/color] 시 제거",
    "무음 각주",
    "무음 증폭",
    "마녀화로 인한 체력 감소 무시\n3턴 후 사망\n[color=#CC6666]턴 시작[/color] 시 [b]【마녀화】[/b] 50스택마다 드로우 더미에서 공격 카드 1장을 골라 이번 턴 [gold]비용 0으로 설정[/gold]",
    "나는 나의 친구",
    "희망의 내일",
    "세뇌",
    "세뇌 반동",
    "다음 턴 에너지 감소",
    "다음 턴 시작 전 공격을 1회 받을 때마다 감정 1스택 획득.",
    "다음 턴 시작 시 {energyPrefix:energyIcons(1)} 획득",
    "다음 턴 시작 시 {energyPrefix:energyIcons(1)} 상실",
    "다음 턴 시작 시 기본 감정 카드 1장을 무작위 복사해 손패에 추가.",
    "다음 [b]공격 카드[/b]를 낸 후 {energyPrefix:energyIcons(1)} 획득.",
    "다음에 미방어 피해로 [color=#99ccff]【안심】[/color]을 잃게 될 때, 1스택만 상실로 변경.\n[color=#6666cc]턴 종료[/color] 전 발동하지 않으면 [color=#6666cc]【침묵】[/color] 1스택 획득.",
    "다음 세뇌 시 [color=#6666cc]【침묵】[/color] {NextBrainwashSilenceCost:diff()}스택 소모",
    "다음 치명적 피해가 대신 체력 1로 생존; 발동하지 않으면 턴 종료 시 [color=#6666cc]【빈 페이지】[/color] 획득.",
    "다음 [color=#6666cc]【빈 페이지】[/color]가 카드를 생성할 때, 생성된 카드 복사",
    "나츠메 안안의 마법",
    "선행 투표",
    "혐의",
    "작은 소리로 승낙",
    "[color=#6666cc]대상 의도[/color] 수정 및 대상을 임의 아군으로 변경",
    "대상의 [gold]의도 대상[/gold] 수정(대상이 공격자에게 카드를 주는 능력 또는 카드 주입 의도를 가진 경우 사용 불가)",
    "축적 반동",
    "선고의 메아리",
    "카드 1장 선택 소모",
    "유지할 카드 선택",
    "복사할 [rainbow][b]【복잡한 감정】[/b][/rainbow] 선택",
    "비용 0으로 설정할 공격 카드 선택",
    "소모할 손패 선택",
    "제거하고 그 카드 풀을 잊을 카드 선택",
    "다시 쓸 손패 선택",
    "손패에 추가할 [rainbow][b]【감정】[/b][/rainbow] 카드 1장 선택",
    "지정된 카드 풀의 재작성 카드 선택",
    "아리사의 인연",
    "지연 반환",
    "의심이 싹트다",
    "제거된 카드",
    "의도가 유도됨; 턴 종료 시 제거.",
    "사쿠라바 에마의 마법",
    "이 능력을 가진 적이 [color=#CC6666]피해[/color]를 입힐 때, 스택마다 10% 증가, 턴 종료마다 1스택 추가",
    "보유자가 공격받을 때, 공격자가 감정 1스택 획득, 본 능력 1스택 감소",
    "카드의 카드 더미 변경을 기록하는 내부 효과.",
    "우클릭 또는 두 손가락 탭으로 [color=#6666cc]【침묵】[/color] 사용 후, 모든 [color=#6666cc]【침묵】[/color] 제거 및 활력과 0비용 다단 [b]공격[/b] 카드 획득.",
    "우클릭 또는 두 손가락 탭으로 [color=#6666cc]【침묵】[/color] 13스택 소모 후 모든 적이 현재 의도를 즉시 실행, 이후 대체 의도 1개 선택 및 각 대체 의도는 [color=#6666cc]교체 선택[/color] 필요",
    "우클릭 또는 두 손가락 탭으로 [color=#6666cc]【침묵】[/color] 소모 없이 적 전체의 현재 의도를 직접 재작성.\n강제 세뇌 후 [color=#6666cc]【세뇌 반동】[/color] 1스택 획득",
    "오른손 주먹",
    "징조",
    "원죄",
    "토노 한나의 마법",
    "[green]이번 턴[/green] 받는 피해 절반",
    "[color=ff99cc]방어도 무시 피해[/color]를 입힌 후 체력이 스택 수 이하이면 즉시 [color=ff99cc]사망[/color]",
    "피해가 3배로, 대상이 무작위 전체 대상으로 변경. 이 피해는 치명적이지 않음. 1턴 후 제거.",
    "사와다 코코의 마법",
    "전투 승리 시, 덱에서(【엔딩】 자체 제외) 희귀 또는 고급 카드를 무작위 선별해 [color=#CC6666]【엔딩】[/color]에 복사, 부착된 [color=#CC6666]고급 카드[/color]는 추가 효과 1회, [color=#CC6666]희귀 카드[/color]는 2회 발동 가능",
    "설욕",
    "진범인",
    "진실",
    "정의",
    "집행의 여운",
    "집착",
    "옷자락을 붙잡다",
    "후지 아리사의 마법",
    "최대 3스택.\n미방어 피해를 받으면 모든 [color=#99ccff]【안심】[/color] 상실.\n다른 카드를 내면 효과 발동: [b]공격 카드[/b]는 스택만큼 추가 피해; [b]스킬 카드[/b]는 스택만큼 방어도 획득; [b]파워 카드[/b]는 스택만큼 [color=#6666cc]【침묵】[/color] 획득.\n2턴 연속 [color=#6666cc]턴 종료[/color] 시 [color=#99ccff]【안심】[/color]이 3스택이면, 상실 후 [color=#6666cc]【고립 무원】[/color] 획득",
    "마지막 인사",
    "죄의 사슬",
    "왼손 주먹",
    "왼손은 큰 힘, 오른손은 힘이 크다",
    "사에키 미리아의 마법",
]

assert len(kor_3) == 115, len(kor_3)
mapping = {texts[i+201].replace('\n', '\\n'): kor_3[i].replace('\n', '\\n') for i in range(115)}
with io.open(r'D:\ManosabaLin\map_powers_kor_3.json', 'w', encoding='utf-8') as f:
    json.dump({"kor/powers.json": mapping}, f, ensure_ascii=False, indent=2)
print("kor powers 批次3 完成: 115 条 (202-316)")
