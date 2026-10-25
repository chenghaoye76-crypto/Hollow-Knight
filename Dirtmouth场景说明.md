# 德特茅斯场景

打开 `Assets/Scenes/Dirtmouth.unity`，进入 Play 即可游玩。新建存档从此场景开始，已有存档仍加载原来的场景。水井连接现有 Game 场景，长椅处按配置中的“上”键休息、恢复生命并尝试保存；未选择存档槽时只恢复生命，不创建存档。

场景使用项目已有的德特茅斯建筑、长椅、水井、路灯、草木、墓碑、音乐与风声。新增的天空和暗角由 Shader 绘制，没有导入新的位图资产。虫长老等 NPC 的立绘与动画未在现有资产中找到，本次未使用其他角色冒充；商店和车站为外观场景，未实现室内、购买及交通系统。

中央按提供的参考图重排：左侧贝壳房屋和挂饰、双头街灯、中央长椅、右侧拱形入口、蓝雾远景与多层石砖前景。相机在中央广场保留固定构图，主角走出广场后再跟随。主角、HUD 和攻击引用从现有 Game 场景复制并重连，新场景的碰撞与源场景独立。

Hierarchy 按天空、远景、中景、建筑、地面、水井、街灯、草木、声音分组，中央细调位于 `08 · Central reference composition`。素材中存在 16、32、64 不同的 Pixels Per Unit，已统一显示比例，未修改原始图片或它们的导入参数。

Play 模式下运行 `Tools > Hollow Knight > Validate Dirtmouth` 可检查素材引用、Shader、HUD、音量分类、长椅回血、街道碰撞与水井通路。预览及报告保存在项目根目录 `DirtmouthPreviews/`。`07-CentralClean.png` 是暂时隐藏 HUD 和地名文字的实机中央画面，正常游戏仍显示 HUD。

`Build Dirtmouth Scene` 只用于第一次生成场景，已有场景不会被覆盖。后续可直接编辑场景；`Refine Dirtmouth Central Square` 仅用于这次参考构图的调校，不会重建整个小镇。

## 雾气、长椅光点与路灯白蝶

场景根物体 `Dirtmouth atmosphere - mist, bench motes and white moths` 上的 `DirtmouthAmbientEffects` 控制动态氛围：`Fog Intensity` 调整分层贴地蓝雾，`Bench Intensity` 调整长椅上升光点，`Distant Blur` 调整 Back 图层远景模糊。角色、近景建筑和地面不参与模糊。路灯旁的小白蝶会振翅、绕飞，长椅光点会上升并淡出。

特效优先识别当前使用的 `town_bench` 长椅与 `light` 分组中的路灯；使用三个动态网格批次绘制。保存场景时暂时恢复远景原材质，保存后重新显示，临时材质与网格不写进场景文件。可用 `Tools > Hollow Knight > Apply Dirtmouth Atmosphere` 重新定位特效并生成 `DirtmouthPreviews/08-Atmosphere.png`。重定位与预览需在非 Play 模式进行。
`Fog Flow Speed` 控制横向雾带及世界坐标雾纹的流动速度（默认 1，0 为静止）。雾气使用脚本传入的时间，编辑模式与游戏运行都可更新；`DirtmouthPreviews/FogFlow/` 保存了排除光点和蝴蝶后的连续雾气验证帧。

## 摄像机跟随范围

`Main Camera` 的 `DirtmouthCamera` 已绑定场景中的 `cameraColl`，取消中央广场固定构图及固定高度，角色在允许范围内保持画面中心，使用 0.10 秒平滑跟随。限制会扣除当前相机可视宽高，确保画面边缘不超过 cameraColl；靠近边界时角色可偏离中心。当前 cameraColl 为矩形 PolygonCollider2D，移动、缩放或修改这个矩形即可调整相机范围。若范围小于视野，相机会缩小视野以容纳边界。`Tools > Hollow Knight > Apply And Validate Dirtmouth Camera` 可重新绑定并验证。
