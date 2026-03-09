# DH.UI 独立迁移手册（仅 4 平台）

本手册用于把 `DH.UI` 从当前仓库独立出去，并只支持以下 4 种运行环境：

1. `win-x64`
2. `linux-x64`
3. `linux-arm64`
4. `osx-arm64`

## 1. 迁移前确认

当前 `DH.UI` 已完成以下前置改造：

1. `VisualProgramming.csproj` 已限制 RuntimeIdentifiers 为上述 4 种。
2. FlowEditor 的导出动态库后缀支持跨平台（`.dll/.so/.dylib`）。
3. 非托管库加载已使用 `NativeLibrary`（不是 `kernel32`）。

## 2. 迁移目录

建议把整个 `src/DH.UI` 目录迁到新仓库根目录。

需要保留的关键内容：

1. `VisualProgramming.sln`
2. `VisualProgramming.csproj`
3. `AlgorithmModule/`
4. `FlowEditor/`, `Views/`, `ViewModels/`, `Services/`, `Models/`, `Controls/` 等源码目录
5. `App.axaml`, `MainWindow.axaml`, `app.manifest`

不建议迁移的构建产物：

1. `bin/`
2. `obj/`

## 3. 推荐迁移命令（macOS/Linux）

在你想放新仓库的位置执行：

```bash
mkdir -p ~/Projects/DH.UI.Standalone
rsync -av \
  --exclude bin \
  --exclude obj \
  /Users/justinxie/Projects/DHdemo/src/DH.UI/ \
  ~/Projects/DH.UI.Standalone/
```

然后初始化 git：

```bash
cd ~/Projects/DH.UI.Standalone
git init
git add .
git commit -m "init: extract DH.UI as standalone project"
```

## 4. 本地开发与构建

在新仓库目录执行：

```bash
dotnet restore VisualProgramming.csproj
dotnet build VisualProgramming.csproj -c Debug
dotnet run --project VisualProgramming.csproj
```

## 5. 四平台构建验证（必须通过）

```bash
dotnet build VisualProgramming.csproj -c Release -r win-x64
dotnet build VisualProgramming.csproj -c Release -r linux-x64
dotnet build VisualProgramming.csproj -c Release -r linux-arm64
dotnet build VisualProgramming.csproj -c Release -r osx-arm64
```

## 6. 四平台发布命令

```bash
dotnet publish VisualProgramming.csproj -c Release -r win-x64 --self-contained true -o ./artifacts/win-x64
dotnet publish VisualProgramming.csproj -c Release -r linux-x64 --self-contained true -o ./artifacts/linux-x64
dotnet publish VisualProgramming.csproj -c Release -r linux-arm64 --self-contained true -o ./artifacts/linux-arm64
dotnet publish VisualProgramming.csproj -c Release -r osx-arm64 --self-contained true -o ./artifacts/osx-arm64
```

## 7. C++ 动态库编译器约定（FlowEditor）

FlowEditor 会优先按平台查找工具链目录：

1. Windows: `Toolchains/mingw64/bin`
2. Linux x64: `Toolchains/linux-x64/bin`
3. Linux arm64: `Toolchains/linux-arm64/bin`
4. macOS arm64: `Toolchains/osx-arm64/bin`

也可以手工指定（优先级更高）：

```bash
export DH_CPP_TOOLCHAIN_BIN=/absolute/path/to/toolchain/bin
```

## 8. 迁移后验收清单

1. `dotnet run --project VisualProgramming.csproj` 能启动。
2. 四个 RID 的 `dotnet build` 全部成功。
3. FlowEditor 导出动态库时，后缀随平台变化（Windows `.dll`，Linux `.so`，macOS `.dylib`）。
4. 外部算法文件选择支持 `.xtj/.xtjs/.dll/.so/.dylib`。
5. 新仓库中不包含旧仓构建缓存（`bin/obj`）。

## 9. 范围声明

本项目当前只保证以下目标运行环境：

1. `win-x64`
2. `linux-x64`
3. `linux-arm64`
4. `osx-arm64`

其他平台（例如 `win-arm64`、`osx-x64`）不在支持范围内。