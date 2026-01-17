# Stratum Desktop 发布指南

## 问题修复说明

### 1. 批处理脚本乱码问题
**原因**：批处理文件使用 UTF-8 编码，但 Windows 命令行默认使用 GBK 编码。

**修复**：
- 在脚本开头添加 `chcp 65001` 切换到 UTF-8 编码
- 将所有中文提示改为英文，避免编码问题

### 2. WPF 不支持剪裁（Trimming）
**原因**：WPF 应用使用反射和动态加载，不支持 .NET 的代码剪裁功能。

**修复**：移除以下参数：
```bash
-p:PublishTrimmed=true
-p:TrimMode=partial
```

---

## 发布方法

### 方法 1：使用批处理脚本（推荐）

```bash
.\publish-release.bat
```

**优点**：
- 自动化流程
- 自动创建版本目录
- 自动生成 ZIP 压缩包
- 显示文件大小

**输出位置**：`releases\v{VERSION}\`

---

### 方法 2：手动发布命令

#### 单文件发布（推荐）
```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true
```

**输出位置**：`bin\Release\net9.0-windows\win-x64\publish\Stratum.exe`

**文件大小**：约 80-100 MB（包含 .NET 运行时）

---

#### 依赖框架发布（体积小）
```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

**输出位置**：`bin\Release\net9.0-windows\win-x64\publish\`

**文件大小**：约 5-10 MB（需要用户安装 .NET 9.0 运行时）

**要求**：用户必须安装 [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)

---

## 发布参数说明

| 参数 | 说明 | 推荐 |
|------|------|------|
| `-c Release` | 使用 Release 配置（优化） | ✅ 必须 |
| `-r win-x64` | 目标平台：Windows x64 | ✅ 必须 |
| `--self-contained true` | 包含 .NET 运行时 | ✅ 推荐 |
| `-p:PublishSingleFile=true` | 打包为单个 EXE | ✅ 推荐 |
| `-p:IncludeNativeLibrariesForSelfExtract=true` | 包含原生库 | ✅ 推荐 |
| `-p:EnableCompressionInSingleFile=true` | 压缩单文件 | ✅ 推荐 |
| `-p:PublishTrimmed=true` | 代码剪裁 | ❌ WPF 不支持 |
| `-p:TrimMode=partial` | 部分剪裁 | ❌ WPF 不支持 |

---

## 发布后测试清单

### 1. 基本功能测试
- [ ] 应用能正常启动
- [ ] 添加验证器
- [ ] 生成 OTP 码
- [ ] 复制验证码
- [ ] 删除验证器

### 2. 界面测试
- [ ] 侧边栏导航正常
- [ ] 所有面板能正常切换
- [ ] 主题切换正常（浅色/深色）
- [ ] 语言切换正常（中文/英文）

### 3. 分类功能测试
- [ ] 创建分类
- [ ] 分配验证器到分类
- [ ] 分类筛选正常（不卡死）
- [ ] 删除分类

### 4. 导入导出测试
- [ ] 创建备份（加密/HTML/URI）
- [ ] 恢复备份
- [ ] 导入其他应用数据

### 5. 设置持久化测试
- [ ] 关闭应用
- [ ] 重新打开
- [ ] 验证主题、语言、排序设置保持

---

## 常见问题

### Q1: 为什么 EXE 文件这么大（80-100 MB）？
**A**: 因为使用了 `--self-contained true`，将 .NET 9.0 运行时打包进了 EXE。

**解决方案**：
- 如果用户已安装 .NET 9.0，可以使用 `--self-contained false` 发布，文件只有 5-10 MB
- 或者使用 ClickOnce/MSIX 安装包，共享运行时

### Q2: 发布后运行报错 "找不到 DLL"？
**A**: 确保使用了 `-p:IncludeNativeLibrariesForSelfExtract=true` 参数。

### Q3: 能否进一步减小文件大小？
**A**: WPF 不支持代码剪裁，但可以：
- 使用 `--self-contained false`（需要用户安装运行时）
- 移除不需要的语言资源
- 使用 ILRepack 合并程序集（高级）

### Q4: 如何创建安装包？
**A**: 可以使用以下工具：
- **Inno Setup**：创建传统安装程序
- **WiX Toolset**：创建 MSI 安装包
- **MSIX Packaging Tool**：创建 MSIX 应用包（推荐）

---

## 发布到 GitHub Release

### 1. 创建 Release
```bash
# 使用 GitHub CLI
gh release create v1.0.0 ^
    releases\v1.0.0\Stratum-Windows-x64-v1.0.0.exe ^
    releases\v1.0.0\Stratum-Windows-x64-v1.0.0.zip ^
    --title "Stratum Desktop v1.0.0" ^
    --notes "Release notes here"
```

### 2. Release Notes 模板
```markdown
## 🎉 Stratum Desktop v1.0.0

### ✨ 新功能
- 单窗口 + 侧边栏导航设计
- Material Design 3 主题系统
- 完整的中英文双语支持
- 分类管理与筛选
- 备份/恢复功能
- 导入 15+ 种其他应用数据

### 🐛 Bug 修复
- 修复分类筛选导致软件卡死的问题
- 修复语言切换覆盖不全的问题
- 修复 AboutPanel 绑定错误

### 📦 下载
- **Stratum-Windows-x64-v1.0.0.exe** (80 MB) - 独立运行，无需安装 .NET
- **Stratum-Windows-x64-v1.0.0.zip** - 压缩包版本

### 📋 系统要求
- Windows 10 1809+ / Windows 11
- x64 架构
- 无需安装 .NET 运行时（已内置）

### 🔒 安全提示
- 首次运行可能被 Windows Defender SmartScreen 拦截
- 点击"更多信息" → "仍要运行"即可
- 或者右键 → 属性 → 解除锁定
```

---

## 性能优化建议

### 1. 启动性能
- 使用 ReadyToRun (R2R) 编译：`-p:PublishReadyToRun=true`
- 缺点：文件大小增加 20-30%

### 2. 运行时性能
- 已启用 Tiered Compilation（默认）
- 已启用 Quick JIT（默认）

### 3. 内存优化
- 使用虚拟化列表（已实现）
- 及时释放大对象（已实现）

---

## 版本号规范

遵循 [Semantic Versioning 2.0.0](https://semver.org/)：

- **主版本号（Major）**：不兼容的 API 变更
- **次版本号（Minor）**：向后兼容的功能新增
- **修订号（Patch）**：向后兼容的问题修复

**示例**：
- `1.0.0` - 首次正式发布
- `1.0.1` - Bug 修复
- `1.1.0` - 新增功能
- `2.0.0` - 重大变更

---

## 自动化发布（CI/CD）

### GitHub Actions 示例
```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x

      - name: Publish
        run: |
          dotnet publish Stratum.Desktop -c Release -r win-x64 --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:EnableCompressionInSingleFile=true

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            Stratum.Desktop/bin/Release/net9.0-windows/win-x64/publish/Stratum.exe
```

---

## 联系方式

- **GitHub Issues**: https://github.com/banlanzs/app/issues
- **文档**: 查看 `IMPLEMENTATION_SUMMARY.md`
- **快速参考**: 查看 `QUICK_REFERENCE.txt`
