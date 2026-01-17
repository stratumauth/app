# Stratum Desktop v1.0.0 - 更新日志

## [1.0.0] - 2026-01-17

### 🎉 重大更新：UI 现代化重设计

本版本对 Stratum Desktop 进行了全面的 UI 现代化重设计，从传统的多窗口模式升级为现代化的单窗口 + 侧边栏导航模式。

---

## ✨ 新增功能

### 架构升级
- **单窗口 + 侧边栏导航** - 所有功能集成在主窗口，告别多窗口割裂
- **NavigationRail 组件** - 80px 宽的侧边栏，支持 6 个导航按钮
- **面板系统** - 5 个独立面板，模块化设计

### 面板功能
- **HomePanel** - 认证器列表
  - 搜索和分类筛选
  - OTP 码显示和倒计时
  - 点击复制功能
  - 右键菜单（编辑/删除/显示QR码）

- **SettingsPanel** - 设置面板
  - 主题切换（浅色/深色/系统）
  - 语言切换（中文/英文）
  - 行为设置（点击复制、最小化到托盘）
  - 排序模式
  - 备份/恢复/导入功能

- **CategoriesPanel** - 分类管理
  - 添加/编辑/删除分类
  - 卡片式列表展示

- **BackupPanel** - 备份/恢复
  - 3 种备份格式（加密/HTML/URI列表）
  - 恢复功能（添加/替换模式）
  - 使用提示和警告

- **AboutPanel** - 关于页面
  - 应用信息和版本号
  - 许可证信息
  - GitHub 链接

### 视觉升级
- **Material Design 3 颜色系统**
  - 完整的颜色规范（Primary/Secondary/Tertiary）
  - 浅色主题（Colors.Light.xaml）
  - 深色主题（Colors.Dark.xaml）

- **动画资源**
  - 6 种动画（淡入淡出、滑入滑出、缩放）
  - 4 种缓动函数

- **界面优化**
  - 窗口尺寸：450×600 → 850×600
  - 更宽敞的布局
  - 统一的设计语言

---

## 🔧 改进

### 代码质量
- ✅ 模块化架构，每个面板独立
- ✅ MVVM 模式，视图和逻辑分离
- ✅ 易于维护和扩展
- ✅ 代码复用率提升

### 用户体验
- ✅ 导航更直观（侧边栏 vs 弹窗）
- ✅ 所有功能触手可及
- ✅ 流畅的面板切换
- ✅ 统一的交互体验

### 性能优化
- ✅ 面板缓存（HomePanel 单例）
- ✅ 虚拟化列表（VirtualizingPanel）
- ✅ 延迟加载面板内容

---

## 🐛 修复

- 无（首次发布）

---

## 🗑️ 移除

- 无（保留所有现有功能）

---

## ⚠️ 破坏性变更

- 无（完全向后兼容）

---

## 📦 依赖更新

- .NET 9.0
- CommunityToolkit.Mvvm 8.2.2
- Autofac 8.0.0
- Serilog.Sinks.File 6.0.0
- sqlite-net-base 1.9.172
- StratumAuth.SQLCipher 1.1.0

---

## 📊 统计数据

### 文件变更
- **新增文件：** 15 个
  - 5 个 Panel XAML + 5 个 Panel .cs
  - 1 个 NavigationRail XAML + 1 个 .cs
  - 3 个资源文件（Colors.Light, Colors.Dark, Animations）

- **修改文件：** 4 个
  - MainWindow.xaml（重构为双列布局）
  - MainWindow.xaml.cs（添加导航逻辑）
  - Styles.xaml（添加 NavigationButtonStyle）
  - App.xaml（加载新资源）

### 代码行数
- **新增：** 约 1500+ 行
- **删除：** 约 200 行
- **净增加：** 约 1300+ 行

---

## 🎯 已知问题

- 无

---

## 📝 升级说明

### 从旧版本升级

1. **数据兼容性**
   - ✅ 数据库结构不变
   - ✅ 设置和偏好自动迁移
   - ✅ 所有现有数据保持完整

2. **功能兼容性**
   - ✅ 所有现有功能保持完整
   - ✅ 键盘快捷键保持不变
   - ✅ 备份文件格式兼容

3. **升级步骤**
   - 下载新版本 exe
   - 关闭旧版本
   - 运行新版本
   - 数据自动迁移

---

## 🚀 下一步计划

### v1.1.0（计划中）
- [ ] 实现编辑认证器功能
- [ ] 拖拽排序认证器
- [ ] 拖拽排序分类
- [ ] 应用动画到面板切换
- [ ] 优化卡片布局

### v1.2.0（计划中）
- [ ] 创建 ImportPanel
- [ ] OverlayService 弹窗系统
- [ ] 转换对话框为 Flyout
- [ ] 自动更新功能

### v2.0.0（远期计划）
- [ ] 多主题色支持（14 种）
- [ ] 云同步功能
- [ ] 浏览器扩展集成

---

## 📚 文档

- **完整实施文档：** [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md)
- **发布指南：** [RELEASE_GUIDE.md](./RELEASE_GUIDE.md)
- **文档索引：** [README_DOCS.md](./README_DOCS.md)

---

## 🙏 致谢

感谢所有贡献者和用户的支持！

特别感谢：
- Stratum 项目原作者
- Material Design 团队
- .NET 社区
- 所有提供反馈的用户

---

## 📄 许可证

本项目采用 GPL-3.0-only 许可证。

---

**完整源代码：** https://github.com/banlanzs/stratum-2fa
**问题反馈：** https://github.com/banlanzs/stratum-2fa/issues
**下载地址：** https://github.com/banlanzs/stratum-2fa/releases

---

**发布日期：** 2026-01-17
**版本：** v1.0.0
**代号：** UI Modernization
