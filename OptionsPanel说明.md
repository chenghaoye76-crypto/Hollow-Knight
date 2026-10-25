# Options Panel 说明

运行 MainMenu，点击 Options。五个分类按提供的参考截图布局，页面运行时由原 OptionsPanel 预制体生成。

- Audio：Master / Sound / Music 三条 0–10 滑杆，可拖动；键盘左右方向键可调节。
- Game Options：设置页中英文切换、受伤镜头震动、HUD 大小、成就显示与支持者名单偏好。
- Keyboard：双列 13 个圆角按键框；点击后按键改绑，Esc 取消，重复键会被拦截。
- Controller：未连接时显示提示；连接后显示动作绑定，高级设置包含绑定和震动测试。
- Video：分辨率、全屏、垂直同步、帧率上限；Screen Scale、Brightness、Advanced Settings 可进入子页。
- 动画：花纹展开和收回、逐项显隐；切换期间锁定输入，返回恢复上次分类选择。设置页显示时虚化背景，退出时恢复。

设置保存在 PlayerPrefs 的 Options.* 键，与游戏存档槽分开。Reset Defaults 只重置当前分类。

现有移动、跳跃、攻击、冲刺及方向攻击使用这些绑定。项目还没有地图、法术、超级冲刺、梦之钉和物品栏系统；这些行现在保存按键配置，待相应玩法实现后读取。成就与支持者名单开关也保存为 GameOptions 偏好，尚无原有系统可接入。

主音量控制 AudioListener；未标记 AudioSource 使用音效音量。音乐 AudioSource 添加 OptionsAudioChannel 并勾选 music，保留音源原始音量后乘以音乐音量。

Play 模式菜单 Tools > Hollow Knight > Validate Options 检查输入绑定；Validate Reference UI 从主菜单/Options 分类页检查五页跳转、滑条和改键，并把实际渲染预览输出至 Temp/OptionsReferencePreviews。