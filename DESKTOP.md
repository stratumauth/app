# Stratum Desktop - Windows WPF 桌面应用

## 概述

Stratum Desktop 是 Stratum 2FA 的 Windows 桌面版本，基于 WPF (.NET 9.0) 开发，复用了 Android 版本的核心逻辑 (`Stratum.Core`)。

## 架构

```
┌─────────────────────────────────────────────────────────┐
│                    Stratum.Desktop                       │
│                      (WPF UI)                            │
│  ┌─────────────────────────────────────────────────┐    │
│  │   MainWindow          - 认证器列表 + OTP 显示     │    │
│  │   SettingsWindow      - 设置界面                  │    │
│  │   CategoriesWindow    - 分类管理                  │    │
│  │   AddAuthenticatorDialog - 添加认证器             │    │
│  │   EditAuthenticatorDialog - 编辑认证器            │    │
│  │   ImportDialog        - 导入数据                  │    │
│  │   PasswordDialog      - 密码输入                  │    │
│  │   QrCodeDialog        - 显示 QR 码                │    │
│  └─────────────────────────────────────────────────┘    │
│                          │                               │
│  ┌─────────────────────────────────────────────────┐    │
│  │              Persistence Layer                    │    │
│  │   Database.cs + 6 个 Repository 实现              │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────┐
│                   Stratum.Core (复用)                    │
│  ├── Entity/     - 数据实体                              │
│  ├── Service/    - 业务服务                              │
│  ├── Generator/  - OTP 生成器 (TOTP/HOTP/Steam 等)       │
│  ├── Backup/     - 加密备份/恢复                         │
│  └── Converter/  - 18+ 应用导入转换器                    │
└─────────────────────────────────────────────────────────┘
```

## 项目结构

```
Stratum.Desktop/
├── Stratum.Desktop.csproj    # WPF 项目文件 (net9.0-windows)
├── App.xaml                  # 应用入口与全局资源
├── App.xaml.cs               # 应用启动逻辑
├── MainWindow.xaml           # 主界面
├── MainWindow.xaml.cs
│
├── Views/                    # 子窗口/对话框
│   ├── SettingsWindow.xaml
│   ├── CategoriesWindow.xaml
│   ├── AddAuthenticatorDialog.xaml
│   ├── EditAuthenticatorDialog.xaml
│   ├── ImportDialog.xaml
│   ├── PasswordDialog.xaml
│   └── QrCodeDialog.xaml
│
├── ViewModels/               # MVVM 视图模型
│   ├── MainViewModel.cs
│   ├── AuthenticatorViewModel.cs
│   └── CategoryViewModel.cs
│
├── Persistence/              # 数据持久化层
│   ├── Database.cs           # SQLite + SQLCipher 数据库管理
│   ├── AsyncRepository.cs    # 异步仓储基类
│   ├── AuthenticatorRepository.cs
│   ├── CategoryRepository.cs
│   ├── AuthenticatorCategoryRepository.cs
│   ├── CustomIconRepository.cs
│   ├── IconPackRepository.cs
│   └── IconPackEntryRepository.cs
│
├── Services/                 # 桌面端服务
│   ├── Dependencies.cs       # Autofac DI 容器配置
│   ├── PreferenceManager.cs  # 用户偏好设置 (JSON)
│   ├── LocalizationManager.cs # 多语言切换
│   ├── IconResolver.cs       # 图标加载
│   ├── DesktopAssetProvider.cs
│   ├── DesktopIconResolver.cs
│   └── DesktopCustomIconDecoder.cs
│
└── Resources/                # 资源文件
    ├── Styles.xaml           # WPF 样式
    ├── Strings.en.xaml       # 英文字符串
    ├── Strings.zh.xaml       # 中文字符串
    └── Icons/                # 图标资源
```

## 数据存储位置

所有用户数据存储在 Windows AppData 目录，与项目代码分离：

```
%APPDATA%\Stratum\
├── authenticator.db3    # 加密数据库 (SQLCipher)
├── settings.json        # 用户偏好设置
└── logs/                # 日志文件
```

实际路径：`C:\Users\<用户名>\AppData\Roaming\Stratum\`

## 开发命令

### 运行调试

```bash
dotnet run --project Stratum.Desktop/Stratum.Desktop.csproj
```

### 构建

```bash
dotnet build Stratum.Desktop/Stratum.Desktop.csproj
```

### 发布打包

**单文件可执行程序 (推荐)**：

```bash
dotnet publish Stratum.Desktop/Stratum.Desktop.csproj \
  -c Release \
  -r win-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish
```

输出：`./publish/Stratum.exe` (~64MB，包含 .NET 运行时，无需安装依赖)

**其他发布选项**：

| 参数 | 说明 |
|------|------|
| `-r win-x64` | 目标平台 (可改为 `win-arm64`) |
| `--self-contained` | 包含 .NET 运行时 |
| `-p:PublishSingleFile=true` | 打包为单个 exe |
| `-p:PublishTrimmed=true` | 裁剪未使用代码 (可减小体积，但可能有兼容问题) |

## 功能特性

### 认证器管理
- 支持 TOTP、HOTP、Steam、Mobile-OTP、Yandex 等多种类型
- 实时 OTP 显示与倒计时
- 点击复制验证码
- 拖拽排序
- 分类筛选
- 搜索功能

### 备份与恢复
- **加密备份** (.stratum) - 使用密码加密
- **HTML 导出** (.html) - 包含 QR 码，便于打印
- **URI 列表** (.txt) - otpauth:// 格式

### 导入支持
- Google Authenticator
- Aegis
- Bitwarden
- 2FAS
- 以及 18+ 其他应用格式

### 多语言
- English
- 中文

### 主题
- 浅色模式
- 深色模式
- 跟随系统

## 依赖项

| 包 | 版本 | 用途 |
|----|------|------|
| sqlite-net-base | 1.9.172 | SQLite ORM |
| StratumAuth.SQLCipher | 1.1.0 | 数据库加密 |
| Autofac | 8.0.0 | 依赖注入 |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 工具包 |
| Serilog.Sinks.File | 6.0.0 | 日志记录 |
| QRCoder | 1.6.0 | QR 码生成 |

## 与 Android 版本的关系

Desktop 版本复用了 `Stratum.Core` 项目中的所有核心逻辑：

- **Entity/** - 数据实体 (Authenticator, Category, etc.)
- **Service/** - 业务服务 (AuthenticatorService, BackupService, etc.)
- **Generator/** - OTP 生成算法
- **Backup/** - 备份加密/解密
- **Converter/** - 其他应用格式导入

桌面端只需实现：
1. UI 层 (WPF XAML + ViewModels)
2. 持久化层适配 (SQLite 路径、偏好设置存储方式)
3. 平台特定服务 (图标解析、资源加载等)

## 已知问题

1. QR 码扫描功能暂未实现 (Windows 摄像头 API 复杂)，建议使用文件导入或手动输入
2. 系统托盘功能需要 Windows 10 1903 或更高版本

## 许可证

GPL-3.0-only
