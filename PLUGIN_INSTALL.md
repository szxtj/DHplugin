# DHPlugin 插件安装指南

## 📦 插件包说明

插件包是 `.dhplugin` 文件，这是一个 ZIP 格式的压缩包，包含：

```
MyPlugin.dhplugin/
├── manifest.json      # 插件元数据
├── MyPlugin.dll       # 插件程序集
├── MyPlugin.pdb       # 调试符号（可选）
└── assets/            # 资源文件（可选）
```

---

## 🔧 插件安装方法

### 方法 1：通过程序界面安装（推荐）✅

**步骤：**

1. **运行主程序**
   ```bash
   ./run.sh
   ```

2. **点击"➕ 安装插件"按钮**

3. **选择 .dhplugin 文件**
   - 浏览到你的插件包文件
   - 例如：`CPlusPlusEditor.dhplugin`

4. **启用插件**
   - 在左侧插件列表中选择刚安装的插件
   - 点击"▶️ 启用"按钮
   - 插件会自动创建标签页并显示界面

**优点：**
- 简单直观
- 自动解压和注册
- 实时看到效果

---

### 方法 2：手动安装到插件目录

**步骤：**

1. **找到插件数据目录**

   | 系统 | 路径 |
   |------|------|
   | Linux | `~/.local/share/DHPlugin/Plugins/` |
   | macOS | `~/Library/Application Support/DHPlugin/Plugins/` |
   | Windows | `%APPDATA%\DHPlugin\Plugins\` |

2. **复制插件包**
   ```bash
   # Linux 示例
   cp CPlusPlusEditor.dhplugin ~/.local/share/DHPlugin/Plugins/
   ```

3. **重启主程序**
   ```bash
   ./run.sh
   ```

4. **启用插件**
   - 程序会自动扫描并加载插件
   - 在左侧列表选择插件
   - 点击"▶️ 启用"

---

### 方法 3：使用预置插件目录

**步骤：**

1. **在发布目录创建 plugins 文件夹**
   ```bash
   cd ~/DHPlugin-Ubuntu-*
   mkdir plugins
   ```

2. **复制插件包**
   ```bash
   cp CPlusPlusEditor.dhplugin plugins/
   ```

3. **运行程序**
   ```bash
   ./run.sh
   ```

---

## 📁 已有插件包位置

### 打包时附带的插件

打包好的 Ubuntu 版本中已包含示例插件：

```
ubuntu/
└── plugins/
    └── CPlusPlusEditor.dhplugin    # C++ 代码编辑器
```

**使用方法：**
1. 解压后，插件已在 `plugins/` 目录
2. 运行 `./run.sh`
3. 程序会自动加载该插件
4. 在左侧选择"C++ 代码编辑器"
5. 点击"▶️ 启用"

---

## 🔍 插件管理

### 查看已安装的插件

程序启动后，左侧边栏会显示所有已安装的插件列表。

### 启用/禁用插件

```
左侧插件列表 → 选择插件 → 点击"▶️ 启用"或"⏸️ 禁用"
```

### 卸载插件

```
左侧插件列表 → 选择插件 → 点击"🗑️ 卸载"
```

### 手动删除插件

```bash
# Linux 示例
rm -rf ~/.local/share/DHPlugin/Plugins/CPlusPlusEditor/
rm ~/.local/share/DHPlugin/Plugins/CPlusPlusEditor.dhplugin
```

---

## 📥 获取新插件

### 从源代码构建插件

```bash
cd /path/to/DHPlugin

# 构建所有插件
dotnet build src/Plugins/CPlusPlusEditor/CPlusPlusEditor.csproj -c Release

# 插件包位置
ls src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin
```

### 下载第三方插件

从插件市场或开发者处下载 `.dhplugin` 文件。

---

## 🔧 常见问题

### Q1: 插件安装后不显示？

**解决方法：**
```bash
# 1. 检查插件目录
ls ~/.local/share/DHPlugin/Plugins/

# 2. 重启程序
./run.sh

# 3. 查看日志
# 程序启动时会显示加载的插件信息
```

### Q2: 插件启用后崩溃？

**解决方法：**
```bash
# 1. 检查依赖
# 确保插件所需的依赖已安装

# 2. 查看错误日志
# 程序控制台会输出错误信息

# 3. 禁用问题插件
# 在左侧选择插件 → 点击"⏸️ 禁用"
```

### Q3: 如何开发自己的插件？

**步骤：**
1. 参考 `publish/sdk/templates/SimplePlugin`
2. 实现 `IPlugin` 接口
3. 创建 `manifest.json`
4. 运行 `dotnet build` 生成 `.dhplugin`

---

## 📋 插件目录结构示例

```
~/.local/share/DHPlugin/
├── Plugins/
│   ├── CPlusPlusEditor/           # 解压后的插件目录
│   │   ├── manifest.json
│   │   ├── CPlusPlusEditor.dll
│   │   └── CPlusPlusEditor.pdb
│   └── OtherPlugin.dhplugin       # 未解压的插件包
├── Config/
│   └── plugins.config.json        # 插件配置
└── PluginData/
    └── CPlusPlusEditor/           # 插件数据
```

---

## 🎯 快速参考

```bash
# ================================
# 一键安装插件
# ================================

# 1. 运行程序
./run.sh

# 2. 点击"➕ 安装插件"

# 3. 选择 .dhplugin 文件

# 4. 启用插件
# 左侧选择 → 点击"▶️ 启用"

# ================================
# 手动安装插件
# ================================

# Linux
cp MyPlugin.dhplugin ~/.local/share/DHPlugin/Plugins/

# macOS
cp MyPlugin.dhplugin ~/Library/Application\ Support/DHPlugin/Plugins/

# Windows
copy MyPlugin.dhplugin %APPDATA%\DHPlugin\Plugins\
```

---

**DHPlugin Team** © 2026
