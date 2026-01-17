# Stratum Desktop - 项目文档索引

## 📚 文档清单

本项目包含以下完整文档，帮助您了解、使用和发布 Stratum Desktop。

---

## 核心文档

### 1. [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md)
**完整的实施总结文档**

**内容：**
- ✅ 项目概述
- ✅ 已完成的工作（Phase 1-4）
- ✅ 架构对比（改进前 vs 改进后）
- ✅ 文件变更清单
- ✅ 使用指南
- ✅ **发布 Release 指南**（新增）
- ✅ 技术细节
- ✅ 待完成的工作

**适合：** 开发者、贡献者、项目维护者

---

### 2. [RELEASE_GUIDE.md](./RELEASE_GUIDE.md)
**详细的发布指南**

**内容：**
- 📦 快速发布（使用脚本）
- 📦 手动发布（3 种方法）
- 📦 GitHub Release 发布流程
- 📦 Release Notes 模板
- 📦 发布检查清单
- 📦 版本号管理规范
- 📦 常见问题解答
- 📦 自动化发布（GitHub Actions）

**适合：** 发布管理员、项目维护者

---

### 3. 发布脚本

#### Windows: [publish-release.bat](./Stratum.Desktop/publish-release.bat)
```cmd
cd Stratum.Desktop
publish-release.bat
```

#### Linux/macOS: [publish-release.sh](./Stratum.Desktop/publish-release.sh)
```bash
cd Stratum.Desktop
chmod +x publish-release.sh
./publish-release.sh
```

**功能：**
- 自动清理旧文件
- 编译 Release 版本
- 创建单文件 exe
- 创建 ZIP 压缩包
- 显示文件大小

---

## 快速参考

### 🚀 快速开始

**编译和运行：**
```bash
cd Stratum.Desktop
dotnet build
dotnet run
```

**发布 Release：**
```bash
cd Stratum.Desktop
publish-release.bat  # Windows
# 或
./publish-release.sh  # Linux/macOS
```

---

### 📁 项目结构

```
stratum-2fa/
├── IMPLEMENTATION_SUMMARY.md    ← 完整实施文档
├── RELEASE_GUIDE.md             ← 发布指南
├── Stratum.Desktop/
│   ├── publish-release.bat      ← Windows 发布脚本
│   ├── publish-release.sh       ← Linux/macOS 发布脚本
│   ├── Controls/
│   │   └── NavigationRail.xaml  ← 侧边栏导航
│   ├── Panels/
│   │   ├── HomePanel.xaml       ← 认证器列表
│   │   ├── SettingsPanel.xaml   ← 设置
│   │   ├── CategoriesPanel.xaml ← 分类管理
│   │   ├── BackupPanel.xaml     ← 备份/恢复
│   │   └── AboutPanel.xaml      ← 关于
│   ├── Resources/
│   │   ├── Colors.Light.xaml    ← 浅色主题
│   │   ├── Colors.Dark.xaml     ← 深色主题
│   │   └── Animations.xaml      ← 动画资源
│   └── MainWindow.xaml          ← 主窗口
└── Stratum.Core/                ← 核心库
```

---

### 🎯 核心功能

| 功能 | 状态 | 位置 |
|------|------|------|
| 侧边栏导航 | ✅ 完成 | Controls/NavigationRail.xaml |
| 认证器列表 | ✅ 完成 | Panels/HomePanel.xaml |
| 设置面板 | ✅ 完成 | Panels/SettingsPanel.xaml |
| 分类管理 | ✅ 完成 | Panels/CategoriesPanel.xaml |
| 备份/恢复 | ✅ 完成 | Panels/BackupPanel.xaml |
| 关于页面 | ✅ 完成 | Panels/AboutPanel.xaml |
| 浅色主题 | ✅ 完成 | Resources/Colors.Light.xaml |
| 深色主题 | ✅ 完成 | Resources/Colors.Dark.xaml |
| 动画系统 | ✅ 完成 | Resources/Animations.xaml |

---

### 📊 改进对比

| 方面 | 改进前 | 改进后 |
|------|--------|--------|
| 窗口模式 | 多窗口 | 单窗口 + 侧边栏 |
| 窗口尺寸 | 450×600 | 850×600 |
| 导航方式 | 弹窗 | 侧边栏 |
| 主题支持 | 单一浅色 | 浅色/深色 |
| 设计语言 | 传统 | Material Design 3 |

---

### 🔧 常用命令

**开发：**
```bash
# 编译
dotnet build

# 运行
dotnet run

# 清理
dotnet clean

# 恢复依赖
dotnet restore
```

**发布：**
```bash
# 快速发布（使用脚本）
publish-release.bat

# 手动发布
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 发布到 GitHub
gh release create v1.0.0 --title "v1.0.0" --notes "Release notes"
```

**测试：**
```bash
# 运行测试
dotnet test

# 代码覆盖率
dotnet test --collect:"XPlat Code Coverage"
```

---

### 📝 版本号规范

遵循 [语义化版本 2.0.0](https://semver.org/lang/zh-CN/)：

- `v1.0.0` - 首次正式发布
- `v1.0.1` - Bug 修复
- `v1.1.0` - 新功能
- `v2.0.0` - 重大变更

---

### 🐛 问题反馈

**GitHub Issues:**
https://github.com/banlanzs/stratum-2fa/issues

**报告 Bug 时请提供：**
- 操作系统版本
- 应用版本号
- 重现步骤
- 错误截图/日志

---

### 🤝 贡献指南

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

---

### 📄 许可证

本项目采用 GPL-3.0-only 许可证。详见 [LICENSE](./LICENSE) 文件。

---

### 🙏 致谢

感谢所有贡献者和用户的支持！

特别感谢：
- Stratum 项目原作者
- Material Design 团队
- .NET 社区

---

## 更新日志

### v1.0.0 (2026-01-17)
- ✨ UI 现代化重设计
- ✨ 单窗口 + 侧边栏导航
- ✨ Material Design 3 颜色系统
- ✨ 5 个独立面板
- ✨ 浅色/深色主题支持

---

**最后更新：** 2026-01-17
**维护者：** Stratum Contributors
**项目主页：** https://github.com/banlanzs/stratum-2fa
