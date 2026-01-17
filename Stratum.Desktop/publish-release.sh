#!/bin/bash
# Stratum Desktop - 快速发布脚本 (Linux/macOS)
# 用途：一键编译并打包 Release 版本

echo "========================================"
echo "Stratum Desktop - Release 发布工具"
echo "========================================"
echo ""

# 设置版本号
VERSION="1.0.0"
read -p "请输入版本号 (默认 $VERSION): " input_version
VERSION=${input_version:-$VERSION}

echo ""
echo "版本号: v$VERSION"
echo ""

# 清理旧的发布文件
echo "[1/4] 清理旧的发布文件..."
rm -rf "releases/v$VERSION"
mkdir -p "releases/v$VERSION"

# 发布 Windows x64 版本
echo ""
echo "[2/4] 发布 Windows x64 版本..."
echo "构建版本: $VERSION"
dotnet publish -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -p:Version=$VERSION

if [ $? -ne 0 ]; then
    echo "错误: 发布失败！"
    exit 1
fi

# 复制可执行文件
echo ""
echo "[3/4] 复制可执行文件..."
cp "bin/Release/net9.0-windows/win-x64/publish/Stratum.exe" \
   "releases/v$VERSION/Stratum-Windows-x64-v$VERSION.exe"

# 显示文件信息
echo ""
echo "[4/4] 发布完成！"
echo ""
echo "========================================"
echo "发布文件位置:"
echo "----------------------------------------"
ls -lh "releases/v$VERSION"
echo "----------------------------------------"
echo ""

# 计算文件大小
SIZE_EXE=$(du -h "releases/v$VERSION/Stratum-Windows-x64-v$VERSION.exe" | cut -f1)

echo "EXE 文件大小: $SIZE_EXE"

echo ""
echo "========================================"
echo "下一步操作:"
echo "----------------------------------------"
echo "1. 测试 releases/v$VERSION/Stratum-Windows-x64-v$VERSION.exe"
echo "2. 创建 GitHub Release"
echo "3. 上传发布文件"
echo "========================================"
echo ""
