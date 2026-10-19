# 光影姿态资产

使用内置 `image_gen`，不是 CLI/API。当前项目成品：`textures/figure_01.png` 至 `textures/figure_06.png`，每张 1254×1254 RGBA；旧 `figures_atlas.png` 不再供运行时采样。

## 当前版本：独立透明帧与三窗审判台

原始六格图中部分头发与衣摆跨越格边。整图抠图仍会留下邻格内容，因此最终逐帧生成独立 PNG；每次仅指定六格参考中的一个动作，材质、人物身份及姿态仍以用户参考为准。以下为共用提示词规格，逐次将指定位置替换为左上、上中、右上、左下、下中、右下：

```text
Use case: precise-object-edit. Extract ONE sprite from the supplied six-cell atlas, targeting ONLY the specified cell. One SINGLE pair of kneeling characters on a square true-alpha canvas, NOT a grid or contact sheet. Preserve that cell's pose, both characters, faces, clothing, expression, white hair, bone wing and all hair. Remove ALL neighboring-cell content. Keep the complete pair including crown, wing tips and skirt inside the canvas; preserve anatomy and proportions without squeezing. All gaps between ribs are transparent. No background, ground, text, matte rectangle or disconnected fragments. Exactly two arms and two hands per character, no duplicate wrist. Both the bone-wing ribs and sword/guard have brighter pink-white cores and refined rose halos; faces, hair and dresses retain their existing exposure. Sword contact is magical light, with no wound detail. Frames: 1 open palm, no sword; 2 raised sword; 3 sword contact; 4 bone burst on both sides; 5 quiet aftermath; 6 final still.
```

第三帧修正提示词原文：

```text
Use case: precise-object-edit. Edit only the anatomy error in this single transparent fantasy animation sprite. Keep canvas dimensions, every character's head, body, clothing, pose, framing, sword design, glow, color, luminous material and transparency unchanged. The dark-haired woman currently has THREE visible hands, which is incorrect. Keep her left hand resting on the white-haired woman's shoulder. Keep only the UPPER RIGHT hand that grips the SWORD HANDLE above the sun-shaped guard; this is her one right hand attached to her right forearm. REMOVE the extra LOWER gripping hand at the middle of the image around x=0.54,y=0.49, and remove its duplicate dark sleeve/arm that extends down-right to x=0.61,y=0.63. Carefully restore the white-haired woman's glowing torso clothing and the luminous sword blade behind this deleted hand/arm. The sword blade now passes cleanly from the guard diagonally down-left into a magical light contact point, with no hand gripping its blade. White-haired woman keeps her hand resting on her own lap; her other hand remains naturally hidden. Exactly TWO arms and TWO hands per character; no extra or disconnected limbs. Preserve bright sword and bone wing equally luminous, brighter than cloth, white-pink cores with rose glow. All transparency retained, no background, no text, no additional effects. Keep the full single pair inside frame and its position identical. This is one sprite, not a grid.
```

当前场景已按用户后续要求调整为中等装饰密度、中央银灰照明与稀疏外侧光流；最终提示词原文见 [court-stage-prompt.md](court-stage-prompt.md)。下面是初版三窗场景规格，供构图来源追溯：

```text
Transparent RGBA game VFX environment, 1536x1024. Use the supplied tribunal architecture as the material reference and the video scenery as the spatial framing reference. Exactly THREE Gothic stained-glass windows, each with its own ornate tribunal lectern. One large window and lectern on the left faces inward/right; the outer-right large window and lectern faces inward/left; the smaller, farther-back inner-right window and lectern faces directly forward. The near groups flank a wide empty center for two complete kneeling light figures; the distant group adds depth. Thick dimensional jambs, carved bronze and burgundy lecterns, floral rosettes, blush and muted violet glass, delicate pearlescent filaments and small prismatic glints. Refined painterly fantasy VFX, not flat line art. Remove the entire shared circular or star-shaped platform and base disc. No room photo, people, vegetation, text or UI. All negative space and the outside are genuinely transparent. The soft central #CCCCCC downlight will be added by the shader, so leave the central gap open.
```

采用的内置生成输出（原文件保留；运行时只引用项目副本）：

| 项目文件 | 生成输出文件名 |
| --- | --- |
| `figure_01.png` | `exec-dc68b66d-4642-4baa-b3f3-70dad44f4ae2.png` |
| `figure_02.png` | `exec-aff9c413-898b-4775-bac1-cd451bc53061.png` |
| `figure_03.png` | `exec-f6f6e14f-6e73-4885-95ee-3ee40f9d4f36.png` |
| `figure_04.png` | `exec-7bcab260-1e1f-4f17-a689-d01d598c592d.png` |
| `figure_05.png` | `exec-fca51d9f-5744-4b29-8daa-025b4eba660f.png` |
| `figure_06.png` | `exec-e0cff75c-ac24-4878-a93b-997edd796ec1.png` |
| `court_stage.png` | `exec-306c9c4e-ac4d-49d2-8d69-bb8f33d8f536.png` |

生成后的 alpha 检查：六帧具有 0–255 覆盖率，场景具有 0–254 覆盖率且画面中心透明。shader 严格保留原始 alpha，只对剑与骨翅的现有亮像素增亮，不再扩张覆盖率。

## 历史图集与旧场景规格（已停用）

第一轮：以 `images/cards/big/emabadending.png` 为人物、衣服和最终姿态参考，生成 3×2 六姿态透明图集。左艾玛、右希罗，固定机位、比例和跪姿落点，顺序为空手承接、凝剑、刺入、骨刺反击、靠拢、停顿。骨刺与接触处用魔法光表达，不加入伤口细节。每人两臂两手。

第二轮使用第一轮图集与用户补充的粉白／暗红相拥光影图作为参考，保留六个动作及位置，改为下面的材质规格：

> Keep exactly six chronological poses in a three-column, two-row sprite atlas. The white-haired woman is formed from translucent pink-white light, the black-haired woman from translucent dark crimson and pink light. All hair strands and fabric folds are fine luminous flowing filaments, tiny embedded starlight dots, pearlescent translucent planes and brighter luminous silhouette rims; faces and hands remain delicately readable like the supplied light-figure reference. Preserve the sword in poses 2–6 and ivory magical spikes in pose 4. No blood. Expressions tragic and subdued. Exactly two arms and hands per person. True alpha transparent cutouts, including gaps between wings and arms; no panel background, ground, text, frame or checkerboard pixels.

原始输出保存在 Codex generated_images；项目使用已复制的成品，不引用个人临时目录。RGB 中可能保留透明像素的生成底色，渲染必须使用 PNG alpha。运行时使用加法混合、分段光丝汇聚与衰减，不能当成不透明人物贴图绘制。

## 台座与彩窗光景

以下是已被三窗审判台替换的五窗圆盘初版提示词，仅作迭代记录，不代表当前场景：

```text
Use case: stylized-concept. Asset: transparent RGBA game VFX environment layer, 1536x1024 landscape.
Inputs: image 1 is architecture reference ONLY (gothic circular tribunal, pointed pink stained-glass with floral tracery, star-shaped round dais). Image 2 is the magic staging/compositing reference ONLY (a localized organic apparition with soft drifting wisps, curved shimmering edge, delicate small points and overhead shafts). Do not reproduce the screenshot UI, green vegetation, or its blue symbols.
Create a refined ethereal tribunal stage cutout, not a full background photo. A broad elliptical star-edged court platform occupies bottom 20 percent. Behind it an imposing semicircle of five tall gothic pointed stainedglass windows opens upward, with the central window reaching close to the top; floral rosettes and thin interlaced mullions, muted amethyst / dusky crimson glass, pale blush-white bright tiny accents, some amber reflected facets. A transparent luminous apparition, detailed painterly fantasy game VFX. Elegant broken strands of pearlescent light and wisps connect the windows around their outer silhouette like a soft round magical enclosure, uneven tiny glass sparkles. Narrow softly feathered overhead illumination, no hard cone borders. Fine dais engraving, concentric segmented stone slabs and subtle reflected window patterns, carved rim filigree. No giant magical rune icon.
Composition is centered and symmetrical in overall balance but naturally irregular in detail. Stage width approximately 94% canvas, architecture height 93%, entire asset and all points inside frame. Very broad unobstructed central space for two kneeling figures to be composited later: keep the central 55% of the image quiet, translucent and low contrast; the richest brightest tracery is on the outer left/right and the top window rosettes. Dark amethyst glass still visible but never opaque walls. The platform has wide empty top and shallow front edge, with no furniture, railings or foreground objects hiding people. NO PEOPLE or faces anywhere. Not a solid black/pink rectangle, not a framed illustration, no ceiling or room walls. All negative space and outside the stage completely true alpha transparent. Polished delicate layers, controlled brightness, soft prismatic glass edge glints, not flat line-art, not white overexposure. No text, watermark, border, checkerboard or decorative title.
```
