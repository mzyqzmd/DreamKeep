# Spine 动画资产配置清单（玩家）

代码已就位，本文只讲**在 Unity 编辑器里要点的三步**。做完之后回到编辑器等一次自动编译，把场景里 `GameRoot` 的空引用补齐即可运行。

资产目录：`Assets/_Art/Card Game Kit/Spine Animations/Player/`
Spine 运行时版本：`spine-unity-4.1-2024-06-19`（配 Spine Editor 4.1.xx）

---

## 第 0 步 · 先修一个既有缺陷：骨架数据没挂图集

`Player_SkeletonData.asset` 里的 **Atlas Assets 数组是空的**，不补这一步，角色拖到场景里会是空白（加载不到任何附件）。

1. 选中 `Assets/_Art/Card Game Kit/Spine Animations/Player/Player_SkeletonData.asset`
2. Inspector 里找到 **Atlas Assets**，把 `Size` 从 `0` 改成 `1`
3. 把同目录的 `Player_Atlas.asset` 拖进 `Element 0`
4. 点 Inspector 右上角的 **Reload**（或右键 Reimport）

校验点：Inspector 底部预览区应能看到角色，且展开 **Animations** 能看到 9 个动画名。

> 注：`EggKnight_SkeletonData.asset` 等其他 9 个角色的 Atlas 也是空的，本次只修玩家，敌人接进来时同样操作一遍。
>
> `Default Mix` 保持 `0`（硬切）即可 —— 循环/一次性由代码里的状态类决定，不在这里配。

---

## 第 1 步 · 建 9 个动画引用资产

`AnimationReferenceAsset` 是 Spine 官方的"动画名容器"，它自带骨架引用，在 Inspector 里是**下拉框选动画名**，不会打错字、改名也能被追踪。

在 `Player` 目录（或你喜欢的动画资产目录）里，右键 → **Create → Spine → Animation Reference Asset**，一共建 9 个，逐个改名并按表填：

| 文件名 | Skeleton Data Asset | Animation Name（下拉选） |
|---|---|---|
| `Anim_Player_Stand` | `Player_SkeletonData` | `stand` |
| `Anim_Player_AttackPillow` | `Player_SkeletonData` | `attack_pillow` |
| `Anim_Player_Skill` | `Player_SkeletonData` | `skill` |
| `Anim_Player_Parry` | `Player_SkeletonData` | `parry` |
| `Anim_Player_ParryStand` | `Player_SkeletonData` | `parry_stand` |
| `Anim_Player_Hurt` | `Player_SkeletonData` | `hurt` |
| `Anim_Player_Death` | `Player_SkeletonData` | `death` |
| `Anim_Player_Sleep` | `Player_SkeletonData` | `sleep` |
| `Anim_Player_Wake` | `Player_SkeletonData` | `wake` |

填法：选中新建的资产 → **Skeleton Data Asset** 拖 `Player_SkeletonData` → **Animation Name** 点下拉框选对应名字。

校验点：9 个都填完后，逐个点一下，Inspector 里不应出现
`Animation 'xxx' not found in SkeletonData` 的黄色警告。

---

## 第 2 步 · 搭玩家预制体

1. 在 `Assets/_Prefabs/` 新建空物体，命名 `Obj_Player`
2. 加组件 **Spine → Skeleton Animation**
   - **Skeleton Data Asset** = `Player_SkeletonData`
   - **Material** 用同目录的 `Player_Material`（一般会自动带出）
   - **Initial Animation / Loop** 留空，由代码驱动
3. 加组件 `Obj_PlayerView`（脚本在 `Assets/_Scripts/Presentation/Object/`）
4. 把第 2 步的 `Skeleton Animation` 拖进 `Obj_PlayerView → Skeleton`
5. 按表把 9 个动画引用资产拖进对应槽位：

| 分组 | 槽位（Inspector 显示名） | 拖入的资产 |
|---|---|---|
| 基类 `Obj_ActorView` | `Stand` | `Anim_Player_Stand` |
| 基类 | `Act` | `Anim_Player_AttackPillow` |
| 基类 | `Hurt` | `Anim_Player_Hurt` |
| 基类 | `Death` | `Anim_Player_Death` |
| 子类 `Obj_PlayerView` | `Skill` | `Anim_Player_Skill` |
| 子类 | `Parry` | `Anim_Player_Parry` |
| 子类 | `Parry Stand` | `Anim_Player_ParryStand` |
| 子类 | `Sleep` | `Anim_Player_Sleep` |
| 子类 | `Wake` | `Anim_Player_Wake` |

6. 摆好位置（世界坐标）、按需加 `Sorting Group` 控制层序，保存预制体。

> `Parry Stand`、`Sleep`、`Wake` 三个如果暂时不想接，留空即可 —— 代码里请求到空槽位会自动降级到 `stand`，不会报错。

---

## 第 3 步 · 接进组合根

场景里的 `GameRoot`（脚本 `Assets/_Scripts/Presentation/Config/GameRoot.cs`）新增了两个序列化槽位：

| 槽位 | 拖入什么 |
|---|---|
| `Playback Queue` | 挂了 `BattlePlaybackQueue` 脚本的物体（可以和 `GameRoot` 同一个物体） |
| （`BattlePlaybackQueue` 上）`Player View` | 场景里的 `Obj_Player` 实例（不是预制体本身） |

`BattlePlaybackQueue` 挂在哪都行，建议直接挂在 `GameRoot` 同一物体上省一次引用。

---

## 运行后应该看到什么

| 时机 | 动作 |
|---|---|
| 进战斗 | `wake` 醒一次 → 转 `stand` 循环 |
| 打出攻击牌 | `attack_pillow` → 回落 `stand` |
| 打出技能/能力牌 | `skill` → 回落 `stand` |
| 打出带格挡的技能牌 | `parry` → 转入 `parry_stand` 循环（随后"获得格挡"事件不再重播起手） |
| 被打中 | `hurt` → 回落 `stand` |
| 玩家回合开始、格挡被清 | 退出 `parry_stand` → `stand` |
| 玩家死亡 | `death`，停在最后一帧不再回落 |
| 战斗胜利 | `sleep` 循环（若玩家已死则跳过，保持 `death`） |

**已知未覆盖**：格挡在敌人回合里被伤害打光时不会收到"清空"事件，玩家会保持举盾直到下个玩家回合开始被清。要修就在 `IBA_Damage.OnExecute` 里补一条 `IBE_BlockCleared` 的发射。

**排错**：Console 出现 `Animation 'xxx' not found in SkeletonData` 说明第 1 步某张资产的动画名选错；角色完全不显示说明第 0 步的图集没挂。
