---
name: manosabalin-character-play-animation
description: Build character play-card animations (attack/cast/power) for the ManosabaLin Slay the Spire 2 mod. Use when adding or redoing a character's card-play animation, generating attack/cast frame assets, matting AI-video frames, building sprite sheets, editing <char>_idle.tscn SpriteFrames, or troubleshooting the CharacterPlayAnimation hook (transitions, clipping, pose mismatch).
---

# ManosabaLin Character Play Animation

## 背景机制（先读懂再动手）

- 原版角色出牌动画 = **Spine 骨骼**，触发点在**每张卡自己的 `OnPlay`**：
  `CreatureCmd.TriggerAnim(owner.Creature, "Attack"/"Cast"/"PowerUp", delay)`
  → `NCreature.SetAnimationTrigger(name)` → `_spineAnimator?.SetTrigger(name)`。
  `DamageCmd.Attack(...).WithAttackerAnim("Attack", delay)` 同理。
- **mod 角色是 `AnimatedSprite2D`、没有 Spine** ⇒ 引擎这套触发**全部落空**（打牌只有 idle 循环）。
  所以必须「卡基类统一触发 + AnimatedSprite2D 承接」，不要逐卡改，也不要试图接 Spine。
- ⭐ **受伤（hit）是另一条链，别和出牌动画混**：引擎 `CreatureCmd.Damage` 对每个受伤者
  （**不限于怪物**，只排除 `receiver == dealer` 与带 `ValueProp.SkipHurtAnim` 的伤害）调用
  `TriggerAnim(receiver, "Hit", 0f)` → `NCreature.SetAnimationTrigger("Hit")` → `_spineAnimator?.SetTrigger(...)`
  ⇒ mod 角色同样落空。接入 = 挂 **`NCreature.SetAnimationTrigger`（同步方法）的 Postfix**
  （`Patches/CharacterHurtAnimationPatch.cs`），`Entity is { IsPlayer: true }` 且 `trigger == "Hit"` 时
  调 `CharacterPlayAnimation.Play(creature, "hit")`；没有 hit 动画的角色被 `HasAnimation` 静默跳过。
  ⚠️ **别挂在 async 的 `CreatureCmd.TriggerAnim` 上**：它有 `await Cmd.CustomScaledWait(...)`，Postfix 要跨 await；
  挂同步的 `SetAnimationTrigger` 时机明确、且落在那个 await **之前** ⇒ 受击表现立刻出现，
  也不受本机 `PrefsSave.FastMode` 影响（这点和联机同步的坑同源）。
  受击动画**不需要自己写"播完回 idle"**：`CharacterPlayAnimation` 已连 `AnimationFinished` → CrossFade 回 default。
- 现有 idle 素材本身就是「AI 图生视频 → 抽帧 → 抠图 → sprite sheet」做出来的，**沿用同一条产线**。

## 产线步骤

### 1. 准备首帧（决定"动作从哪开始"）
默认用 idle sheet 的**首帧**（`arisa_0405a0c1_loop_16fps_sheet_8x6.png` 的 (0,0,960,960)）——
它与游戏内构图一致。**不要用立绘**（画布带噪点/底色）。
合成到**纯中性灰底**（`(128,128,128)`），再放大到 1080×1080。

⭐★ **首帧里角色要"小"、四周留大片空白**（占画布高度 **42%~46%**）：
```
s = 目标人物高(如 660) / idle 人物 bbox 高;  缩放 idle 帧 → 居中偏左贴在 1440×1440 灰底上
```
Arisa（窄武器、动作小）撑满画面没问题；**希罗（长剑大幅挥砍）撑满必炸**——
模型一放大动作，剑尖就被画面边缘裁断（实测 17~60 帧全部裁断，两版视频全废）。
留白后同一个模型就能把整把剑保持在画面内（第三版零裁断）。
⚠️ 代价：抠图时要按 `scale = idle高/视频人物高 ≈ 1.36` 放大回来，画质略软，可接受。

⭐ **想跳过"蓄力/搓火"这类前置段时**：从上一版视频里挑一张**已经抬手蓄势**的帧
（如 `arisa_attack5/raw/0038.png`）当首帧，再生成"挥击→恢复站立"。
这样模型没有机会再演一遍蓄力，是控制动作段最有效的手段。

### 2. 生成视频（VideoGen 图生视频）
prompt 必须写清（**成本敏感，一次写好**）：
- 固定机位、镜头完全静止、背景纯中性灰不变
- 角色完整外观描述（发色/瞳/面罩/斗篷/绷带/铁链铁球/靴）
- **朝向**：游戏里玩家在左、敌人在右 ⇒ 写「面向画面右方，向右挥砍」
- **只做一个动作** + **结尾必须回站姿**（「砸完后手臂立即收回、恢复垂手站立姿势并保持不动」）
- **装饰不变**：「头发/斗篷/面罩/绷带/铁链/铁球全程保持原样，不脱落、不消失、不变形」
- negative：`镜头移动, 运镜, 背景变化, 多余人物, 服装变化, 装饰脱落, 转身, 迈步, 连续多次攻击, 蓄力, 先搓火, 火焰飞出去, 水印`
- ⚠️ 模型**天然爱加戏**：① 先"掌心慢慢生火"再打；② 火焰变成脱离身体的飞行道具。用户明确不要这两段
  ⇒ 必须在 prompt 里点名排除（「不蓄力、不先搓火，火焰只在挥出手的一瞬间出现」「火焰不飞出去」）。
  **武器同理**：会自己补剑气/光刃/白色拖影弧线 ⇒ negative 写 `剑气, 能量光效, 发光, 特效, 光晕`，特效一律自己后处理。
- ⚠️ **大挥砍类动作**追加：正面写「人物只占画面中间约一半高度，四周留出大片空白」「整个人物和整把剑在全部时间里都完整保持在画面内，不触碰画面边缘」；negative 追加 `放大人物, 人物变大, 剑尖超出画面, 肢体触碰画面边缘, 弓步, 跨步, 前扑`。
- ⚠️ 输出**恒为 5.04s / 121 帧 @24fps**（1080P 输入 → 1440×1440 输出），无法在生成端缩短 ⇒ 靠第 5 步剪短。

### 3. 抽帧
`cv2.VideoCapture` 顺序写 `raw/%04d.png`；先用接触表（每 10 帧采样拼图）确认朝向/动作/装饰是否正确。

### 3.5 素材来源 B：用户直接给「N 帧立绘序列图」（跳过 1~3 步）
判据：一张大图里**网格排布的 N 个独立立绘姿势**（白底或透明底），不是视频抽帧。
先例：**希罗受击**——4×3=12 帧白底 1448×1086（`hiro_hurt2/`）。此时**跳过首帧/图生视频/抽帧**，直接进第 4 步的抠图。

- ⭐★ **切格绝不能用固定网格**：立绘式排布的行/列**几乎相接**（希罗同列上下间距 <14px），固定 362px 网格会把
  **上一行的鞋、隔壁格的剑尖**切进本格（实测 cell3 `left=0`、多格 `top=0/bot=402` 贴边）。
  正解 = **连通域提取**：`connectedComponentsWithStats` ⇒ 期望**恰好 N 个连通域**（希罗：12 个、每个 30k± 面积、
  每个 = 1 个完整角色含剑、无碎剑）⇒ 按 `(cy//362, cx)` 排序即得帧序。
  ⚠️ 先跑一步体检（`.local/hiro_hurt2/analyze.py`）：打印每个 comp 的 bbox/面积，确认「数量 == 帧数、无碎片」再往下走。
  ⚠️★ **同样禁止「固定网格 + PAD 外扩」的折中做法**（2026-10-11 又踩一次）：`hiro_hurt4/build.py` 用 362 网格把 blobs 分组后再给每格 bbox
  外扩 `PAD=24`，24 源像素被 ×2.1 放大 ⇒ 帧 5~12 **头顶凭空多出一对鞋**、帧 1~4 脚边多出一撮头发，正是上面警告的「上一行的鞋」。
  **外扩 == 把「刚好落在格子外」的那一小片邻居重新切回来，和固定网格是同一个错**，不是折中。
  唯一正解：**取 N 个最大连通域各自的 bbox、`PAD=0`**。希罗源图实测：46 个连通域，最大 12 个面积 29.7k~34.7k、其余全 ≤13px，
  面积极差 <2× ⇒ 就是干净 12 个角色（含各自整把剑，无碎剑）。
  ✅ **成品自检（必做，数值化）**：对拼好的**每一帧**算 `最大连通域面积 / 该帧 alpha 总面积`，**必须 > 0.98**；
  再打印各帧「对齐后暗部顶 y」的极差兜底。一掉下 0.98 就是又把邻居切进来了。先例脚本 `.local/hiro_hurt5/build.py`。
  （注：`<攻击/能力>`动画里**与身体分离的刀光/法阵/弹道**本来就会构成第 2 个连通域 ⇒ 这套阈值只适用于**立绘序列图**这类「一帧一姿势」的素材。）
  不要做「bbox 邻近合并」——同列间距比同行还小，会把**整列合成一个 figure**。
- ⭐★ **白底抠图：背景必须用「与图像四边相连的连通域」，不能按『近白』一刀切**：
  `d = max(|R-255|,|G-255|,|B-255|)` → `bg0 = d<=22` → 对 `bg0` 做 `connectedComponents` → **只取 label 出现在四条边上的那些**为背景。
  否则角色**内部的白衬衫领、白花、皮肤高光**（d≈0）会被一起挖空。
  白边：前景**边缘 3px 带**内 `alpha = clip((d-12)/34)`（软过渡）；其余前景 alpha=255；`alpha==0` 处 RGB 清零。
  丢弃面积 <800px 的连通域（jpeg 噪点）。参 `.local/hiro_hurt2/prepare.py`。
- ⭐★ **对齐：水平必须用基准帧的全局固定 offset，垂直才逐帧锚**。
  立绘序列里每帧是**独立绘制**的姿势，**逐帧按鞋底中心锚 x 会让整个人左右滑**——
  希罗 cell5 鞋心比 cell1 偏右 43px，×scale 2.52 ⇒ **108px 横向漂移**（bbox 左缘 370 vs 479）。
  最终：`x = orig_x + M - ref_x*scale`（ref = 一张站姿帧，全局固定）、`y = orig_y + M - y*scale`（逐帧锚鞋底）
  ⇒ 12 帧脚底 1263~1267、左缘全部 479 ✓。**这正是「整体平移 ⇒ 混合法」规则的立绘版**。
- ⭐ **换 sheet 时保持「同名 + 同网格 + 同格尺寸 + 同 idle 贴点」⇒ 场景 `.tscn` 一个字都不用改**。
  `AtlasTexture_*` 的 `region` 是按 `(i%COLS*W, i//COLS*H, W, H)` 生成的，只要这几项不变，覆盖 PNG 即生效
  （希罗受击：12 帧 / 6×2 / 格 1476×1652 / idle@(300,300) / 总幅 8856×3304 **全部沿用**，`nikaido_hiro.tscn` 零改动）。
  这是最省事也最不容易出错的做法，**优先于「改网格再去改 tscn」**。
- ⭐ **逐帧不做缩放**：立绘姿势不同 ⇒「头顶高度」天然有差（希罗后 8 帧含胸举剑格挡，头顶低 30~80px）。
  先判断是**姿势差异**还是**缩放差异**（比脚底-头顶高、比肩宽/头长），确认是姿势就别缩放（技能里明确警告「别靠缩放凑」）。
- ⭐★ **"还有白边"第一步必须判断来源，别调抠图参数**（希罗受击踩坑，连调 7 版无效）：
  **把原图放到深灰底上看**——`out[:]=(58,60,70); m = lum<246; out[m]=C[m]`。
  若原图在灰底上**也有白边** ⇒ 是**画进素材里的白色"贴纸描边"**（AI 生成常见，沿每根发丝 2~3px、lum 180~240），
  **任何"按距背景距离"的抠图处理都够不到它**（它长在轮廓内侧）。
  ✅ 解法 = **按亮度+饱和度替换**：`pale = keep & (dist<=2.5) & (lum>=170) & (sat<=45)`，用邻近"更暗"像素的颜色覆盖
  （`src_lum < lum-8`，4 轮 roll 传播）；皮肤（饱和）与暗色发丝（暗）自然保住。
  ⚠️ **参数别照抄旧版也别一味放大**：`dist<=10 + 8 轮` 会把边缘染成**黑毛刺**（希罗 v3 踩过）；每次换素材都要
  先在小图上试 **2.5px/4 轮**，确认「黑描边还在、白雾没了」再全量跑。
  ❌ 无效/有害，别再走：放宽背景阈值（会吃白格纹，希罗 cell-06 身高掉 8%）／**白键 alpha**（`clip((215-lum)/120)`
  ⇒ 整个人半透明）／大范围无差别替换（吃掉墨线描边）。
  ⚠️ 若白边/白块在**轮廓内侧较深处**（如脸部），那是素材画面内容，抠图去不掉 ⇒ 只能重生成。
- ⭐★ **人物比例不一致时，缩放只能对齐「高矮」，永远对不齐「体型」**：头身比、裙长、发宽是**画出来的**。
  ⇒ 只要用户说「比例不对 / 太矮」，先量 **头长/身高比、肩宽、裙摆位置**再决定：
  若体型一致只是高矮差 ⇒ 缩放能救；若头身比或服装比例不同 ⇒ **必须在生成阶段解决**（用 idle 立绘做首帧走图生视频，
  或以 idle 做参考图重生成动作帧），别在抠图后硬凑。
- ⭐★ **缩放基准要用「暗色主体高度」，不要用 alpha-top**（那是第三次踩同一类坑）：
  idle 的红花**低于**发顶，素材的红花**高出 10px** ⇒ 用 alpha-top 算身高会让 scale 偏小 3~7%（表现 = "受击太矮"）。
  正解：`dark = alpha>180 & max(RGB)<115` 的 bbox（头发+黑衣+黑袜），**逐帧** `s_i = darkH_idle / darkH_i`，
  水平用 `dark left` 锚到 idle 的 `dark left`（比重心/鞋底中心稳，实测 25~28 抖动仅 3px）。
  ⇒ 合成后逐帧复查 `dark top/bot/h/left` 应全部≈ idle，这是"比例对齐"的硬证据。
- ⭐ `.godot/imported/<name>-<hash>.ctex` 的 **hash 只依赖「源路径 + 导入设置」，不随文件内容变**
  ⇒ 换图后同名 ctex 被**原地覆盖重写**，不必担心旧图残留（先删一次也无害）。
- 验收：`--headless --script res://.local/check_scenes.gd` 应打印 `animations: ... hit=12`。

### 3.6 素材来源 C：用 idle 立绘当参考图自己生成帧序列（`ImageGen`）
当「素材来源 B」的体型对不上待机立绘、而缩放又救不了时（见上「体型对不齐」），**正解是用 idle 立绘重新生成整套动作帧**。
先例：**希罗受击 v3**（`.local/hiro_hurt3/`）。

- 调用：`ImageGen`，`image1 = <角色 idle 首帧 PNG>`、`input_fidelity: high`、`size 1536x1024`、`quality: high`。
- ⭐★ 提示词必须写死三件事，缺一不可：
  ① **格子数**：「画面必须被均分为 4 列 × 3 行 = 12 个格子，每格完整画一个角色，**不可少于 12 格、不可合并**」；
  ② **与参考图完全一致**：列出「头身比 / 身高 / 服装长度 / 发型宽度 / 脸型」都同参考图，并写明**约 7.5 头身**（防幼态/防比例漂移）；
  ③ **逐格姿势**（1…12 依次描述），并加塔二画风。
- ⚠️★ **AI 出序列图的三种固定毛病**（希罗连撞两次）：
  · **格数不足**：只说「12 格」它常只画 8 格；
  · **缺人**：某格只画了武器/背景，人没了；
  · **裁腿**：某一整行只有上半身（腿出画）。
  ⇒ **处理：不要反复重生成，直接挑「全部完整的那一张」**（哪怕只有 8 格），
  **再用 blend 补出首尾过渡帧**凑回目标帧数：`lead = blend(idle, f0, t) for t in (0.35, 0.70)`、
  `tail = blend(f_last, idle, t) for t in (0.35, 0.70)` ⇒ 8 帧变 12 帧，**场景 `.tscn` 零改动**。
- ⭐ 好消息：**同一参考图生成出来的帧，体型天然对齐**（比例问题自动消失）；但仍会带**白色贴纸描边**，
  照上面「按亮度+饱和度替换」处理即可（一步到位，别再走 distance-from-bg）。
- 一趟脚本搞定（抠图 + 连通域切格 + 擦白 + 对齐 + 拼 sheet）：`.local/hiro_hurt3/rebuild.py`。

### 4. 抠图 + 对齐（脚本 `.local/arisa_cut_v2.py`，按角色改参数即可）
```bash
# 显式帧表 + 指定基准帧 + 边距
python arisa_cut_v2.py <raw_dir> <idle首帧png> <out_dir> "14,20,26,31,35,39,43,49,62,76,92,108,120" 110 margin=120
# 均匀采样: ... <out_dir> <start> <end> <step> [ref_index] [margin=NN]
```
- **背景色键**：取四条边 30px 环带的**中位数**为背景色（比四角均值抗噪）。
- **软阈值 alpha**：`dist<32` 全透明、`dist>72` 全不透明、中间线性过渡（去锯齿灰边）。
- **强制清水印**：`mask[y>0.92H, x>0.70W] = 0` 及左下角 `x<0.30W`（AI 视频右下角有「可灵AI生成」水印，不清会污染 bbox）。
- **形态学**：13px 椭圆闭运算 + 5px 开运算；**连通域**保留最大块 + 主体 bbox 外扩 40px 内、面积 >250 的碎片（铁链/铁球）；floodFill 填内部孔洞。
- **去灰边**：alpha 40~220 的边缘像素，RGB 取 7×7 邻域内 alpha>200 像素的中值色。
- **对齐 = 统一变换（关键）**：以**一张站姿帧**为基准算一次 `scale`（= idle 人物高 / 视频人物高）与位移（脚底对齐 `iy1`、**脚部水平中心**对齐），之后**所有帧共用同一变换**。逐帧对齐会抖。
- ⚠️★ **`ref_index` 必须是一张站姿帧**：若第 0 帧是"举手蓄势"（用上一版帧当首帧时必然如此），
  还拿它当基准会把整体缩放/对位全算错。此时显式传一张站姿帧（如 `110`）。
- ⚠️★ **`margin=NN` 防裁断**：挥击火焰会超出 960 画布被切成**硬直边**。加 `margin=120`
  ⇒ 帧画布变 1200。角色尺寸/位置**完全不变**（只扩大画布），因为 `AnimatedSprite2D` 默认 `centered`，
  按**帧中心**对齐，所以 idle(960) 与 attack(1200) 可以共存、无需改节点。
- ⚠️ 不要用 `rembg`（模型从 github 下载被代理挡）。

### 5. 剪短！只挑「一次动作」
- 用户要的是**原版战士那样「就砍一下」**：**12~16 帧**（20~22fps ≈ 0.6~0.8s）。
- ⛔ 失败教训：42 帧 / 2.1s / 多段动作（甩链+爆火+收势）= **太长、动作太多**。
- ⭐ **用非均匀帧表**代替 `start/end/step`：蓄力段稀、挥击段密、**收势段稀**（收势快反而自然），
  例如 `"14,20,26,31,35,39,43,49,62,76,92,108,120"`（13 帧：3 帧蓄力 → 4 帧挥击带火 → 6 帧收回站立）。
- ⭐ **末帧尽量是站姿**（与 idle 接近），能大幅减轻"攻击→待机"的跳变。
- ⭐ **火焰只保留 3~4 帧**（"出手那一下"），不要把模型演出来的大段火焰全收进来。

⭐★ **快挥武器的必杀技：只用"画得完整"的关键帧 + 补间**（希罗长剑踩出来的）
- 模型渲染**快速挥击的中段帧**极不可靠：武器糊成一坨、**整把消失只剩手**、或方向前后矛盾。
  逐帧取视频帧一定翻车（希罗 14 帧里只有 5 帧武器完整）。
- 做法：从视频里挑**武器完整的 3~5 张姿态**（如 idle / 举剑 / 劈出 / 收势），
  中间用**交叉溶解补间**（见 5.5）补出拖影 —— 高速挥砍时这就是运动模糊，反而自然。
- ⚠️★ **溶解补间只适合「身体几乎不动、只有武器在甩」的动作**（希罗劈砍就是这种）。
  若**整个身体在位移**（雪莉直拳要弓步前压 + 抬臂），溶解会把两具身体叠成**半透明双影**，很难看。
  ⇒ 判据：先在接触表上看「除武器外的身体有没有移位」。
  **有 ⇒ 取真实视频中间帧**；没有 ⇒ 才用溶解。
  雪莉的做法：帧表末尾追加 `,46,47`（**追加在末尾可保持已有索引不变**），
  再在脚本里按新索引 `f[14]`/`f[15]` 插进出拳段，全程零溶解、零双影。
- 判"这帧能不能用"：抽帧后看**连通域表**（`.local/hiro_check.py` 拼对照图 + 逐帧打印 `area/实心度/bbox`）。
  武器与身体**断连是常态**（刀身常是独立连通域）⇒ **不能"只留最大块"**，
  要为每帧**显式列出保留几个额外连通域**（希罗：举剑帧留 1 个=剑身，其余留 0）。
- 模型自画的**白色拖影弧线**是**中性灰白**（R≈G≈B、`max−min`≤12），剑身带粉红调（R−B≈12）⇒ 可按饱和度区分；
  更稳的是照上面的显式保留表清掉。
- ⚠️ **脚部对齐的自检**：视频里角色会整体漂移。用"**鞋底平台**"测量（逐列取最低 y，
  找连续 ≥25 列、相邻差 ≤8 的水平台）而不是整块 mask 的 bbox 中心——垂到脚底的武器会把中位数/中距拉偏
  （希罗 f000 被剑尖拖偏 100+px）。量出来逐帧平移对齐到 idle 鞋底线即可。

### 5.5 帧后处理：过渡帧 + 手上特效（可选，脚本 `.local/make_attack_v2.py`）
在**抠图成品帧**上做，输出新目录（如 `cut_final18/`）后再拼 sheet。

- ⭐ **首尾过渡帧（"待机→这 / 回到待机"的溶解）**：取 idle 首帧（idle sheet 的 `(0,0,960,960)`），
  `Image.blend(idle0, 攻击首帧, t)`（t=0.35 / 0.70）生成 2 帧放在攻击序列**之前**；
  末帧同理 `Image.blend(攻击末帧, idle0, 0.35 / 0.70)` 放在**之后** ⇒ 总帧数 +4。
  ⚠️ `Image.blend` 是**逐通道**插值 ⇒ 主体区 alpha 仍为 255，**不会变半透明**，只是两姿势混合；
  别自己写「alpha 也按 t 缩」的版本（会得到幽灵透明感）。这与代码里的 CrossFade 叠加，过渡更顺。
  ⚠️★ **位移大时（整条手臂+长剑扫过）要改用"覆盖度加权"**：逐通道混合会留下**实心双像**（旧剑清清楚楚还在）。
  覆盖度加权：`alpha = a1*(1-f)+a2*f`、`rgb = (rgbA·a1·(1-f)+rgbB·a2·f)/alpha`
  ⇒ 两帧都占的**身体保持不透明**，只有错开的部位变**半透明拖影**＝运动模糊。
- ⭐ **手上特效（火球等）**：用 numpy 画 RGBA 特效球 = 分层色环（亮黄白核心 → `#F0A030` 亮橙 →
  `#C4631C` 浅橙 → `#B0251C` 赤红 → `#4A0D0D` 暗边）+ `bump` 多频正弦扰动的**锯齿边缘** + 外圈柔和光晕；
  按 **帧号 → ((中心x, 中心y), 缩放)** 表贴到手上（坐标用带 60px 网格的分析图读出）。火球色系＝项目火色，**禁用青色**。
- ⭐ 用户口径：「在**手挥出去的帧**里给手添加火球，**完全挥出去火球消失**」
  ⇒ 只贴在挥击过程帧（含蓄势托火段），手臂展开到位后的帧**不贴**。
  ⚠️ **背身帧**（手臂被身体挡住）别把火球压在脸上/肩上，挪到身体轮廓外的挥动路径上。
- 自检：输出一张 `check18.png`（首 2 帧 + 过渡帧 + 火球帧 + 末 2 帧）拼图，肉眼确认贴上位置与不透明度。
- ⭐★ **拳类 / 无武器动作（雪莉直拳踩出来的）**——模型演"出拳"有三个固定毛病：
  ① **保持段被拉得极长**：雪莉"出拳 → 保持"占了 raw48~102（约 2s / 50 帧），必须按帧表剪成 2~3 帧定格；
  ② **手可能画成张开的手掌/指人**而不是握拳 ⇒ 抽帧后**务必把手部 200~300px 放大看**（雪莉这版是握拳，可用）；
  ③ **拳头定位（贴特效用）不能取 mask 的右缘最远点**：弓步的**前腿/鞋**、女巫**斗篷下摆**会抢走它
     （雪莉 cut f004 的右缘点是鞋，y≈1236）。必须**限制在上半身高度带**（如 `340<=y<=980`）再取最右一簇的质心。
- ⭐ **波纹 / 冲击环（雪莉 #33ccff）**：numpy 在拳心画**同心椭圆环**（`ring(r) + 0.60·ring(0.66r)`，纵向压扁 0.86）
  + 外扩柔晕 `haze`；颜色按强度在 `#1068BA → #33CCFF → #CEF6FF` 间插值；
  再沿外环撒 4~6 颗小亮星（后 `GaussianBlur(r*0.018)`）；整体 `GaussianBlur(r*0.045)`。
  逐帧「半径变大 + alpha 变小」＝冲击波扩散。同样**画在角色下层**（拳头/袖子挡住内侧，正好是"从拳后散出"）。
  ⚠️ 波纹中心必须**逐帧检测拳心**（拳头在移动），不能用固定偏移；用 `{帧序: (半径, alpha)}` 表驱动。
- ⭐ **武器特效（火球/剑气）自己画，别让模型画**。剑气 = **二次贝塞尔锥形弧**（脚本 `.local/make_hiro_attack.py`）：
  两端点自动取**两帧的剑尖**（连通域里"离主体重心最远点"），控制点 = 中点外侧 170~300px；
  沿 t 采样画四边形条带（`宽 = W0·sin(πt)^0.55`，两端尖中间粗），色带
  `#3A0A0C → #6B1116 → #B0251C → #DC143C → #FF7860`，再叠 3 条细亮芯（法向偏移 0 / ±0.26W），
  `GaussianBlur(W0·0.055)` 软化后整体乘 alpha。
- ⭐★ **"伸出手臂"类动作：先量，别急着改**（艾玛踩出来的，两轮返工才收敛）。
  ⚠️ 起因：要「前下方伸手」，连写两版 prompt（`斜着向画面右下方伸出、手掌落到腰部附近`）模型都生成**水平直伸**；
  换 ImageGen 图生图改姿势更糟（**整个角色被重画**：袖子黑白反了、发色变了）。
  ✅★ **最终正解＝什么都不做，直接用生成的原帧**——先用数值**量一遍手臂朝向**再决定要不要改。
  量法：取手臂走廊（`x > 肩点x` 且 `y` 在手臂高度带）内的亮部像素做 **PCA 主轴角**，再单独取
  「手部」质心（`x>800` 的亮部）看落点。艾玛 14 帧实测：f003 手落在 (822,593)（前下方，角 48°），
  f004 (848,481)，**f005–f008 稳定在 (846,461)（角 ~8°，即「向前伸直、正对敌人」）**，f009 起下收。
  ⇒ 模型**本来就是按"直臂指向敌人"生成的**，硬压成「前下方」纯属自找麻烦。
  - ❌ **别做「逐像素渐变扭曲」**：旋转角随半径/角度平滑渐变 ⇒ 旋转量沿**径向**有梯度，
    重采样后袖子**拉丝变细**、手指被抹成一条肉丝、上臂到手掌**扭成 C 形弧——"人手弯不出那个弧度"**（用户原话）。
    收紧角窗/半径窗、加亮度门**全都治标**，病根是"渐变"本身。
  - ❌ **别做「图层刚体旋转 + 底图挖空」**（`.local/ema_arm_droop3.py`，看着很美，实际把立绘挖残）：
    手臂图层绕肩点整体转 28° 后，底图必须把原手臂位置**挖空**；而手臂底下究竟是**躯干**还是
    **透明背景**事前无法预知 ⇒ 挖到躯干就把肩/胸挖出**洞**（用户看到的「人物立绘都不完整了」），
    挖不到就在背景里留**悬空白条 / 两条袖子**。**28° 远超可行范围**：近肩区位移小还能凑合补，
    远端位移 ~150px，被遮挡的躯干内容**根本无法重建**。
    ⇒ **铁律：手臂与躯干重叠时，任何"搬手臂"的方案都会破坏立绘；宁可不改。**
    （角小到 10° 以内、且位移区全在**透明背景**上才勉强可行，近肩处仍要手写 inpaint 补裙子/下巴——不值得。）
  - ⭐ 真要「前下方」：**在生成阶段就引导**（首帧立绘摆成前下方伸手），别在抠图后改姿势。
  - ⭐ 角度校准用**叠加「从肩点出发的角度射线 + 同心半径圆」的网格图**（`arm_grid2.png`）肉眼读，
    比纯数值直方图快得多；前景 bbox 也能救命（艾玛整个角色只在 x 437–893）。
- ⭐★ **法阵（艾玛 `#ff99cc`）**：`numpy` 画**同心椭圆环**（`exp(-((rr-k)/w)²)`）+ `|sin(12θ)|^10` 辐条
  + `|sin(24θ)|^18` 外刻度点 + 内外柔光；`rr` 用 `(x-cx)/(R·0.48)` 归一化 ⇒ **水平压扁**＝该圆面正对右方敌人（侧视透视）。
  颜色在 `#FF99CC → #FFE4F2` 之间插值，`f` 同时当强度与 alpha。**画在角色下层**（手掌/袖子挡住内侧）。
  ⚠️★ **柔光项必须压得很低**（外柔光/中心柔光 ≈ 0.07/0.05）：换成**饱和色**后，原 0.16/0.18 的柔光
  会铺满整个圆面，缩略图上看就是**一张实心圆盘**而不是法阵。浅黄时看不出来，色彩一换就暴露。
  ⚠️★ **法阵色号用户改过三次**（`#ffcc99` → `#33ccff` → `#ff99cc`）⇒ 以**最新一次**为准；改色只动
  `C_BASE`/`C_CORE` 两个常量，别顺手改形状/半径/位置。
  ⚠️★ **合成后要把全透明像素的 RGB 清零**（`arr[arr[:,:,3]<1,:3]=0`）：`_over` 里 `oa` 微小但 >1e-6 的分母
  会把法阵底色写进 alpha=0 的像素，Godot **线性过滤**时会拿它给角色剪影**染边**。
  ⭐ 圆心 `x = 掌心x + 26`（落在手掌**前面**＝远离身体一侧）；逐帧用 `{帧序:(半径比例, 透明度)}` 表驱动，
  出现 0.34→0.74→1.0、保持、再 0.66→0.30 收掉。
  ⚠️ 掌心定位同拳类：只取**上半身高度带**（`300<=y<=1000`）内轮廓最右一簇的质心（否则会被裙摆/鞋抢走）。
  ⭐★ **交付前必须做"与原帧逐像素比对"**：主体帧应**逐字节等于**抠图原帧，法阵帧的可见差异
  （`max|Δ|>8` 且 `alpha>2`）必须**全部落在法阵几何范围内**——这是"立绘没被改坏"的唯一硬证据。
- ⭐★ **"甩出光"类动作（安安踩出来的）**。用户口径：「光在立绘拿的本子上**汇聚**，再把**光**甩出去；
  本子**一直拿着**（不是把本子扔出去）」。安安实测＝怀里小羊玩偶 + 左手速写本，两样都是 idle 固有物，
  别误判成"模型幻觉出的道具"（先看 idle 立绘确认哪个是固有物）。
  - ⚠️★ **索引空间绝不能混**：擦除/重着色用**抠图帧号**，而光效若写成**序列帧号**，会整体错位 2 帧画到没动作的帧上（安安踩过）。
    **正解＝擦除 + 光效全部在「抠图帧号」空间做完，再拼首尾过渡帧**，绝不在 seq 上二次索引。
  - ⭐★ **模型自画的白色光球：优先"重着色"，不要"擦除"**。擦除要堆「亮度/饱和度/区域/连通域面积」一堆条件，仍会误伤邻近白物
    （安安 f04 的白球**紧贴速写本白书页**，擦除必挖洞）。重着色只要一行：
    `m = 圆内 & alpha>0 & lum>116 & sat<90` → `arr[m,:3] = C_GLOW * (lum/136)` ⇒ **保留原亮度场与软边、只换色相**；
    白核自然变淡蓝白、外圈自然过渡；书页被一起染蓝反而是好事（读作"光落在书页上"）。
  - ⭐★ **色相映射的坑**：`t = clip(f,0,1)` 之后再 `thr = clip((t-a)/b)` ⇒ `t` 永远 ≤1，**白核永远出不来**，整片光发白。
    必须用**未截断的强度场**算白度：`thr = clip((f-0.95)/0.45, 0, 1)`，`t = clip(f,0,1)` 只用于 alpha
    ⇒ 中低亮＝主色 `#6666cc`，只有 `f>1` 的极亮处才近白。
  - ⭐ 轨迹与书本位置对齐：`GATHER` 逐帧汇聚到书上（半径/强度递增到 1.0）；`SIGIL` 从书上起飞、
    逐帧**右移 + 降 alpha**、末 1~2 帧出画 ⇒ "甩出去"。`SIGIL` 形状＝超椭圆本体 + 内辉 + 书脊亮带
    + 书页双细线 + 轮廓光 + **向左**锥形拖尾（拖尾朝"来处"，不是朝飞行方向）。
  - ⭐ **"不能太用力 / 体弱人设"靠生成阶段控制**（首帧立绘 + 提示词），后期只补光，别靠后期改姿势。
  - ⚠️ **别要求光效帧也逐字节相等**：柔和光场的尾巴会让**实体像素**变化 1~4/255（浮点舍入 + 极淡染色）。
    判据应为「无光效帧逐字节相等」+「实体像素(`alpha>200`)在**光场强度 ≤0.02** 处 `max|Δ| ≤ 5`」。
    （照搬"`max|Δ|>8` 必须落在几何范围内"会报几十个 1~4 的**假越界**，白折腾。）
- ⭐★ **剑气画在角色下层**（`Image.alpha_composite(arc, frame)`）⇒ 扫过身体/头部的部分被角色挡住，不会糊脸。
- ⭐ 用"**只画到 t=p_max**"表达扫击过程：`p_max` 逐帧 0.38→0.68→0.92→1.0，之后保持 1.0 并逐帧降 alpha（0.9→0.55→0.22）收尾。
- 预览页：`.local/make_attack_preview.py`（亚里沙）；`.local/make_hiro_preview.py`（希罗 idle 是 61 张单图、
  attack 画布更大 ⇒ 脚本先把两者贴到同一 1476×1652 画布再裁方形，这样预览与游戏内的相对位置一致）；
  `.local/make_sherry_preview.py`（雪莉，同希罗套路，画布 1478×1648、`CROP=(39,124,1439,1524)`）；
  `.local/make_ema_preview.py`（艾玛，画布 1310×1588、`CROP=(88,175,1288,1375)`）；
  `.local/anan_attack/make_anan_preview.py`（安安，画布 1474×1652、`CROP=(240,330,1474,1564)`；
  idle 是 **sheet** 7×5 ⇒ 脚本自己切格 `(i%7)*874, (i//7)*1052`）。
  ⚠️ 预览脚本就是把 `<char>` 的 idle 帧**居中贴到与抠图相同的画布**（居中偏移必须 == `margin`），
  否则预览里的相对位置与游戏内不一致。

### 6. 拼 sprite sheet
`cols×rows` 网格、每格 = **抠图输出尺寸**（默认 960；用了 `margin` 就是 idle 尺寸 + 2×margin）、
输出 RGBA PNG，命名 `<char>_attack_<N>f_sheet_<C>x<R>.png`（放 `images/characters/<Char>/`）。
**同时删掉旧 sheet 及其 `.import`**，并清 `.godot/imported/<旧名>*.ctex|.md5`（否则旧图仍进 PCK）。
- ⭐ **格子的长宽可以≠方形**：希罗 idle 是 876×1052、`margin=300` ⇒ 攻击格 1476×1652。
- ⭐★ **攻击格尺寸 = idle 格尺寸 + 2×margin**（用**同一个节点 scale** ⇒ 内容自动落到同一位置、人物大小不变，
  因为 `AnimatedSprite2D` 按**格中心**对齐，而抠图时内容位置 = idle 位置 + margin）。
  艾玛：idle 单图 830×1108 ⇒ 攻击格 1310×1588（margin=240）；安安：idle 格 874×1052 ⇒ 攻击格 1474×1652（margin=300）。
  ⚠️ 别直接拿「idle 格尺寸」当攻击格：光/武器/火焰飞出去会被裁断，且一裁就破坏上面这条对齐关系。
- ⚠️ **显存**：帧画布越大越吃显存（1476×1652×15 帧 ≈ 146MB，加上 61 帧 idle 合计 ~370MB）。
  只在大挥砍（武器要出画）时才用大 margin，否则能小就小。

### 7. 改场景 `<char>_idle.tscn`（脚本 `.local/build_attack_tscn.py` / `.local/build_hiro_attack_tscn.py`）
```bash
# 亚里沙（idle 是 sheet，列数固定 7）
python .local/build_attack_tscn.py res://ManosabaLin/images/characters/Yalisalin/arisa_attack_18f_sheet_7x3.png 18 960 22
# 希罗（idle 是 61 张单图、格子非方形、列数可变）
python .local/build_hiro_attack_tscn.py res://ManosabaLin/images/characters/Hiro/hiro_attack_15f_sheet_5x3.png 15 1476 1652 20 5
# 雪莉（idle 是 56 张单图；场景文件名不是 `<char>_idle` 而是 `sherrylin.tscn`）
python .local/build_sherry_attack_tscn.py res://ManosabaLin/images/characters/Sherrylin/sherry_attack_19f_sheet_5x4.png 19 1478 1648 22 5
# 艾玛（idle 是 59 张单图；场景 emalin.tscn；原 load_steps=61）
python .local/build_ema_attack_tscn.py res://ManosabaLin/images/characters/Emalin/ema_attack_18f_sheet_6x3.png 18 1310 1588 22 6
# 安安（idle 是 sheet 7×5 / 35 帧、格 874×1052；场景 ananlin.tscn；原 load_steps=38；idle 图集 id 前缀 anan_idle_）
#   ⭐ attack(18f/22fps) 与 hit(12f/24fps) 一次生成 —— 该脚本自身幂等，从 bak 重跑即可
python .local/anan_hurt/build_anan_anim_tscn.py
```
- 各脚本的差异只在**常量**：`BAK`/`SRC` 路径、`ORIG_STEPS`（亚 49 / 希 63 / 雪 58 / 艾 61 / 安 38）、
  最后一条 idle `ext_resource` 锚点串、`SpriteFrames` 的 id、图集 id 前缀。
  新角色照抄最接近的那个改这 5 处即可。
- ⚠️ 脚本**固定从 `.local/<scene>.tscn.bak`（无 attack 的原始备份）读、覆盖写 SRC**；
  备份必须存在，且**每次重做都从它重跑**（在已含 attack 的文件上叠加会重复插入）。
  换版本前先确认「当前文件剔除全部 attack 段后 == 备份」再动手。
- `load_steps` = 原值 + Σ(1 ext + N sub)（亚里沙原 49 ⇒ 13 帧=63、18 帧=68；希罗原 63 ⇒ 15 帧=79；
  安安原 38 ⇒ attack(18)+hit(12) = 38 + 2 + 30 = **70**）。
- 在**最后一个 idle ext_resource**（亚里沙=idle sheet、希罗=`hiro_0061.png`）之后插一条
  `[ext_resource type="Texture2D" path=... id="2_<char>_atk"]`（希罗那条没有 `uid=`，手写即可）。
- `SpriteFrames` 之前插 N 个 `[sub_resource type="AtlasTexture" ...]`（`region = Rect2(网格坐标, CELL_W, CELL_H)`）。
- 在 idle 动画结尾（`"speed": 16.0\n}]`，注意希罗前面还有 `"name": &"default",`）之后追加：
  `}, {"frames":[...], "loop": false, "name": &"attack", "speed": 22.0}]`
- ⭐ **一个角色有多个非 idle 动画时，把它们串在同一条尾串里**：每个动画贡献
  `}, {"frames":[...], "loop": false, "name": &"<名>", "speed": <fps>}`，只有最后一个才收尾 `}]`。
  安安 = `default`(loop,16) → `attack`(22) → `hit`(24)。多动画建议写成一个**幂等**脚本
  （从 bak 重跑、一次写全部动画），避免"在已含 attack 的文件上再叠 hit"造成重复插入。
- ⚠️ 改之前先 `cp` 一份原始备份；**基于备份重跑**，不要在已改过的文件上再叠加（会重复插入）。
- ⚠️★ **`animations` 数组的闭合 `]` 只能出现一次**。`}, {` 这个分隔符是「上一段的 `}`」+「下一段的 `{`」，
  **每段的字符串里已经含了首尾**，拼接时极易多写一个 `}` 或少写/多写一个 `]`。
  曾把 `]` 写进每一段 ⇒ 文件出现 `}]}, {`，`animations` 数组被**提前闭合** ⇒ Godot 解析 `SpriteFrames` 失败
  ⇒ `PackedScene.Instantiate` 返回 null ⇒ 游戏里报
  **`Invalid scene: root node Bounds cannot specify a parent node`**（`Bounds` 是第 3 个节点，被当成了新的根）
  ⇒ **角色立绘整块不显示**。报错信息与真正的原因（SpriteFrames 括号）完全无关，别被带偏。
  生成脚本里必须加**括号配平自检**（区间 `animations = [` 到 `\n[node`，`[{`/`]}` 计数 depth 归零且中途不为负）。
- ⭐★ **改完 `.tscn` 必须用 Godot 本体验证「能否实例化」**，不要靠肉眼看文本：
  ```bash
  "D:/222/.../Godot_v4.5.1-stable_mono_win64_console.exe" --headless --path "D:/ManosabaLin" \
      --script "res://.local/check_scenes.gd"
  ```
  `.local/check_scenes.gd`（`extends SceneTree`）对每个角色场景 `load()` + `instantiate()`，
  并打印 `Visuals` 的每个动画名与帧数 ⇒ 输出 `[OK] ... animations: attack=18, default=35, hit=12` 才算过。
  这是唯一能在**启动游戏之前**发现"立绘不显示"的办法；PCK 字节搜索查不出结构错误。

### 7b. ⭐ **cast / power 动画的快捷产线：两张 4×3 网格 → 24 帧**（与 attack 不同，先看这条）
attack 走的是「整段视频 → 抽帧」；**cast 目前是「生成两张 4×3 网格图（各 12 帧），按 A、B 顺序拼 24 帧」**。
- 源图：用户提供或内置 image_gen 产出两张 **1024×1536 RGBA**，每张 4 行 × 3 列 = 12 帧；**顺序不可颠倒**（第二张接着第一张的动作，如「汇聚峰值 → 光消退收手」）。两张源图通常直接放进 `images/characters/<Char>/`，但**不要**把它们当作 sheet 用（它们是「帧网格」不是「拼好的 sheet」，列数 3 与目标 6 不一致）。
- 脚本：`.local/<char>_power/build.py`（+ `emautils.py` / `inspect.py`），幂等、可重跑。先例 `.local/hiro_power/`、`.local/ema_power/`。
- 与 attack 产线的关键差异：
  1. **固定统一缩放**（`scale` 由第 1 帧的「脚底→头顶」推出后**全程不再变**），不逐帧归一化 —— 因为施法时手臂/光效会改变包围盒，逐帧归一化会让身体忽大忽小。
  2. `advance=[...]` 是**逐帧前后位移曲线**（24 个值，首尾为 0），用来做「前踏施法」的位移；`dy` 用脚底对齐。
  3. **第 1 帧与第 24 帧强制复制 idle 底图**（不是抠出来的帧），保证 CrossFade 首尾与待机像素一致、无缝。
  4. 断言：身高极差 `< 35px`、首尾帧字节相等、每帧 `getbbox()` 不贴边。
- 输出命名 `<char>_power_24f_sheet_6x4.png`（**6 列 × 4 行**），每格 = `idle 尺寸 + 2×margin`：
  - 希罗 `hiro_power_24f_sheet_6x4.png` 7656×5808，格 **1276×1452**（idle 876×1052，margin=200）
  - 艾玛 `ema_power_24f_sheet_6x4.png` 7620×6192，格 **1270×1548**（idle 830×1108，margin=220）
- 场景写法与 step 7 相同（`load_steps` +1 ext +24 sub；`}, {"frames":[...],"loop": false,"name": &"cast","speed": 24.0}]`），
  但 `build.py` 通常**自己就从 `.local/<char>_power/<scene>.before-cast.tscn` 备份重跑并直接产出完整场景**，不用单独再写 `build_*_tscn.py`。
  ⚠️ 脚本里应断言 `'"name": &"cast"' not in text`，防止在已含 cast 的文件上重复插入。
- 自检：`.local/<char>_power/check_scene.gd`（per-character，非共用的 `check_scenes.gd`）——
  断言 `default`/`attack`/`hit` 帧数**未被破坏** + `cast=24` + `speed=24` + `loop=false` + 每帧 `region.size == (格宽,格高)` 且不越界 + 首尾帧 `get_data()` 相等 + 播完 `frame==23 and not is_playing()` + 之后 `play("default")` 能继续。
  先例：`hiro_power/check_scene.gd`、`ema_power/check_scene.gd`。
- ⭐ helper：`.local/ema_power/emautils.py` 用 `sys.path.insert` 复用 `.local/arisa_hurt/analyze.py` 的 `components()`，
  并提供 `body_anchor()`（按暗部行数阈值取头顶 y 与头部中位 x）与 `shoe_floor()`（暗部最大 y = 脚底）。新角色照抄改常量即可。
- **已做**：希罗（竖剑 + 暗红/灰白光）、艾玛（前下方伸手 + `#ffcc99` 法阵）。剩余：亚里沙 / 雪莉 / 安安 / 杏奈 的 cast。

### 8. 接入代码
`ManosabaLinCode/Characters/Common/CharacterPlayAnimation.cs`
- `PlayByCardType(creature, CardType)` → `Attack→"attack"`、`Power/Skill→"cast"`；
  `HasAnimation` 不存在则静默跳过；播完连 `AnimationFinished` 回 `"default"`，每个 sprite 只连一次。
- `ManosabaCardTemplate.BeforeCardPlayed` 里 `cardPlay.Card == this` 时调 `PlayByCardType(Owner.Creature, Type)`
  ⇒ **一张卡不用改，全 mod 卡生效**。
- ⭐★ **交叉溶解（CrossFade）——解决"待机↔攻击过渡跳变"**：
  idle 是垂手站立、attack 首帧是举手，直接 `Play()` 会**跳帧**。
  做法：切换瞬间把**旧画面**复制成一个 `AnimatedSprite2D` 替身插在 `%Visuals` **之后**（画在上层），
  复制 `SpriteFrames/Animation/Frame/Position/Scale/Offset/Flip/Modulate/ZIndex`，`Stop()` 后设回 `Frame`（冻结），
  然后 `TweenProperty(ghost, "modulate:a", 0, 0.12)` 淡出 + `QueueFree()`；本体 `Play(新动画)`。
  用 `Dictionary<ulong, Node2D>` 记住每个 sprite 的替身，新切换时先 `QueueFree` 旧的防叠影。
  回 idle 也走同一个 `CrossFade`（`AnimationFinished` 回调里调），不要直接 `Play("default")`。
- 新增角色的动画 = 在该角色 `<char>_idle.tscn` 加同名动画即可（hook 与角色无关）。
- ⭐★ **受伤/death 等"引擎触发器"动画：用 RitsuLib 的官方接口，绝对不要写 Harmony 补丁。**
  引擎里 `CreatureCmd.TriggerAnim` 最终只调 `NCreature.SetAnimationTrigger(trigger)`（原版实现是
  `_spineAnimator?.SetTrigger(...)`）。RitsuLib 的 `ModCreatureCombatAnimationPlaybackPatch`
  **已经**在这个方法前拦截，只要角色模型实现了 `IModCreatureCombatAnimationStateMachineFactory`
  并返回非空状态机，触发器就路由过去 —— 而 `ModCharacterTemplate` 本来就实现了该接口，
  **官方覆写点就是 `protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(Node visualsRoot, CharacterModel character)`**。
  （曾经在这里 Postfix `SetAnimationTrigger` 自己补一次 hit 动画 —— 完全多余，且与 RitsuLib 的 Prefix 双重播放。）
  实现放在 `Characters/Common/ManosabaCharacterTemplate.cs`（全角色共用）：
  ```csharp
  var frames = FindAnimatedSprite(visualsRoot)?.SpriteFrames;            // %Visuals → 递归兜底
  if (frames == null || !frames.HasAnimation("default")) return null;    // 只认场景里真实存在的动画
  var builder = ModAnimStateMachineBuilder.Create().AddState("default", loop: true).AsInitial().Done();
  // Hit→"hit" / Attack→"attack" / Cast→"cast"：一次性状态必须 WithNext("default") 自动回 idle
  builder.AddState(anim, loop: false).WithNext("default");
  builder.AddAnyState(trigger, anim);
  return builder.BuildForVisualsRoot(visualsRoot, character);            // 或 ModAnimStateMachines.StandardCue(...)
  ```
  ⭐ **为什么必须 `WithNext(idle)`**：原版 Spine 有**多条轨道**（idle 循环垫在下面，hit/attack 是一次性叠加），
  所以 Spine 不需要显式回 idle；`AnimatedSprite2D` **只有单轨**，一次性动画播完会**定在最后一帧**。
  RitsuLib 的兜底路径（`ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger`）只做
  `Play(名, 1f, false)`，不会回 idle ⇒ **不建状态机就会出现"受击后一直僵在护胸姿势"**。
  ⭐ 触发器名 = 原版 `CreatureAnimator` 的常量：`Idle`/`Attack`/`Cast`/`Hit`/`Dead`/`Revive`（注意大小写）；
  兜底路径的别名表是 `hurt`→{hurt,hit,Hit}、`attack`→{attack,Attack}、`die`→{die,death,dead,Dead}。
  ⚠️ 一旦为某角色建了状态机，**该角色的所有触发器都被状态机消费**（RitsuLib 的 Prefix 直接 return false）
  ⇒ 只声明真实存在的动画，缺的分支自然落空（等于改造前行为），**别用 `StandardCue` 的"null 名 → 指向 idle"**，
  那会让没有 hit 动画的角色"被打一下重播 idle"。

### 9. 发布 + 核验
- 先 `probe_procs.py` 探测游戏（运行中会锁 DLL）。
- **改过 `.tscn` 先跑 Godot headless 实例化验证**（见第 7 步的自检脚本），再发布。
- `dotnet publish ManosabaLin.csproj -c Release`（**在根目录**）⇒ 自动 `--export-pack` **会自动导入新图**。
- `cp -f DLL/json/pck` 到 `steamapps/workshop/content/2868840/3747637918/`（游戏只从这里加载）。
- 核验：PCK 字节搜索 `<sheet名>`>0、旧 sheet=0、`AtlasTexture_<char>_atk_N`>0、`CharacterPlayAnimation`>0。
- 核验场景：从 PCK 里按 `[gd_scene` 偏移截出 `.tscn` 文本，与磁盘文件比对（应逐字节一致）⇒ 确认打进包的是修好的那版。

## 坑
- ⚠️★ **图生视频的"动作时长"必须写死在提示词里**：素材恒 5.04s / 121 帧，但一个受击/施法动作只要 0.5s 左右。
  模型在 5 秒里会**持续演化**，越到后面越跑偏（道具被换成别的东西、人物缩小、长发炸开）。
  ⇒ 提示词务必加「**动作在开头一秒内完成，之后一直保持这个姿势不动**」，negative 里禁
  「人物缩小/放大、比例变化、头发飘散、道具替换、举过头顶、把东西举到脸旁」。
  **不要把"一个动作"和"全程 5 秒"混为一谈** —— 我们是去 121 帧里**挑一段**，不是播完整条。
- ⚠️★ **选段前先量化，别只看缩略接触表**：对候选帧用背景色键 mask 打印
  `frame / top / bot / h / left / right / width / footX` 表，一眼看出①动作集中在哪几帧
  ②人物从哪一帧开始漂移或缩身。缩略图上**看不出 5% 的尺寸漂移**（5% 在游戏里就是肉眼可见的"变小"）。
  采样时先排除右下角噪声：`m[1350:,:]=False; m[:,1280:]=False`（阈值 30 时边缘有噪点，会污染 bbox）。
- ⚠️★ **图生视频做"受击姿势"的五种固定毛病**（连撞 4 条素材）：
  ① 一律演成**后仰**（下巴抬起、挺胸），绝不按提示词前倾 —— 必须正面写死「上半身向前弯下一点点、含着胸、
     头微微低下来、下巴不要抬起来」，negative 逐条排除 `后仰/向后仰倒/头向后仰/抬脸/下巴抬起/昂头/挺胸`；
  ② 手里有道具就**把道具举到脸前遮脸** —— 正面写位置区间「本子整体保持在**胸口到脖子之间**，绝对不要遮住她的脸」，
     negative 加 `本子挡脸/本子盖住脸/遮住眼睛`；
  ③ 会把怀里的**玩偶替换掉**（只剩书）—— 正面写「玩偶**自始至终**在怀里，没有被放下、没有被书替换掉」；
  ④ 头发/裙摆**大幅飘散**成"大风"效果 —— 正面写「长发整齐垂在身后和肩前、**全程纹丝不动**、裙摆自然垂下不飘动」，
     negative 加 `头发飘动/长发飘散/长发炸开/裙摆飞扬/大风`；
  ⑤ 会**转身扭腰**变成侧面 —— 正面写「肩膀、腰和脸的朝向**全程保持不变**，不转身、不扭腰、不变成侧面」，
     negative 加 `转身/扭腰/转体/侧身/背对镜头/扭头/肩膀转动`。
  ⇒ 这五条是"手里抱着道具 + 被打"这一类镜头的通用补丁，写别的角色也照抄。
- ⚠️★ **人物漂移分两种，处理方式完全不同**：
  * **整体平移**（`footX` 逐帧漂、`h` 不变）⇒ 可救：抠图时逐帧锚定。
    ⚠️★ 但**逐帧锚定的 x 和 y 要分开对待**：`anan_hurt_cut.py` 那种「逐帧脚底带中心」会把**水平**位置
    带偏（脚被裙摆/头发遮住的帧尤其明显）⇒ **推荐混合法**（`.local/anan_hurt/anan_hurt_cut7.py`）：
    **水平用基准帧全局固定 offset，垂直逐帧锚到脚底 y**，并对 shoeY 做 **3 点中值滤波**去离群
    （v7 的 r0028 shoeY=1117 是离群，中值后 1143）。适用场景：人物屈身时**整体抬高**（脚离地），
    全局固定 offset 会保留这个抬高，逐帧 y 锚定才能贴回地面。
  * **大小/比例变化**（`h` 变）⇒ **不要靠缩放去凑**（会把飘散的长发一起放大，更怪）；**只取 `h` 稳定的那一段**。
  安安第二条素材 `h` 799→724（缩 9%）、footX 666→655（左移），头发还炸开 ⇒ 弃用；
  第三条 `h` 799→770（缩 4%）、footX 667.5→664.5（几乎不动）⇒ 取 f000–f038 即可用。
  ⚠️ **逐帧锚定只在真的漂移时用**；素材本身稳的话用**全局固定 offset**（`.local/arisa_cut_v2.py` 原版行为）最稳 ——
  逐帧算反而会被"脚被裙摆/头发遮住"的帧带偏（第三条 f032–f036 的 footX 就被带偏到 616）。
- ⚠️★ **动画之间必须共用同一对位基准**：attack / hit / cast 的抠图都要用
  「**idle 整体 bbox 中心 `icx` ↔ 视频脚底带中心 ×scale**」「**脚底 y 对齐 idle 的 `iy1`**」这套公式
  （即 `arisa_cut_v2.py` 的原始行为）。改了锚点概念 ⇒ 动画之间切换会左右/上下跳位。
  对位自检：`idle@1474 bbox` 与各帧 bbox 的**顶边、脚底**应基本一致（安安 idle (466,310,1005,1327) vs f000 (460,309,980,1328)）。
- ⭐ **受击动画的收尾不要自己加"回正"过渡帧**：播完由 `AnimationFinished` → CrossFade 回 default，
  这与出牌动画同一套；最后一帧停在"护住"的姿势即可。帧数取 **12 帧 / 24fps ≈ 0.5s**（比 18 帧的攻击更短）。
- ⚠️★ **颜色分割分不开主体部件时，改用「数值剖面」**：沿若干 `y` 行打印 `lum`/`alpha` 的 ASCII 剖面
  （例：`x 900→1474 每 12px 一个字符`，`. 透明 / # 暗(<120) / o 中 / * 亮(>180)`），一眼看出分界。
  比反复画网格图读坐标快且准（安安靠它定出速写本 x≈1050~1170、模型自画光球 x≈1150~1250；
  之前靠肉眼看网格图连错三次坐标，还把速写本白书页当成光球）。
  同理，**同一色系相连的部件（深蓝速写本 vs 深蓝裙摆）用连通域/颜色阈值都分不开**，别在这上面耗时间。
- 视频模型会生成**灰色半透明残影**（拖影）⇒ 靠连通域 + 位置约束清掉，别只靠色键。
- **火焰被画布裁断成硬直边** ⇒ 用 `margin` 放大帧画布（见第 4 步）。
- 角色朝向：**必须与游戏内一致（面向右）**，生成时就要写死。
- 抽帧后一定先看接触表再抠图，否则白抠。
- 素材与代码改完必须重新 publish（图在 PCK 里、代码在 DLL 里）。
- ⚠️★ **publish 后必须确认「游戏目录的 DLL 真的换了」——别信时间戳**：MSBuild 的 Copy 任务按**时间戳**判断，
  目标文件（游戏目录）比源（`.godot/mono/temp/bin/Release/ManosabaLin.dll`）**新就跳过** ⇒ 只更新了 PCK、DLL 还是旧的。
  （实测：mods 目录 DLL 时间 19:45 vs 本地产物 19:25 ⇒ 拷贝被静默跳过。）
  ⭐ **判据 = 直接搜 DLL 字节里有没有新类型名**（`open(dll,'rb').read().count(b'新类型名')`，如 `TransmigrationPairCapability`），
  三处都查：`.godot/mono/temp/bin/Release/`、`mods/ManosabaLin/`、`workshop/content/2868840/3747637918/`。
  ⭐ **workshop 目录最容易被漏**（游戏只从它加载）——即使 mods 是最新的，workshop 的 DLL/PCK 仍可能是几天前的。
  publish 只写 mods 目录，**必须自己 `cp -f` DLL + json + PCK 三件套到 workshop**（PCK ~780MB，约 15s）。
- ⚠️ **mods 目录可能留着上次的 `*.tmp`**（半更新残留，几百 MB）⇒ 发布前顺手 `rm -f <mods>/*.tmp`。
