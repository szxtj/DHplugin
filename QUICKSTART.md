# DHPlugin 快速启动指南

## 项目已完成构建！✅

### 项目结构

```
DHplugin/
├── src/
│   ├── DHPlugin.Host/          # 主程序 (空壳应用)
│   ├── DHPlugin.Core/          # 插件管理核心
│   ├── DHPlugin.Abstractions/  # 插件抽象接口
│   ├── DHPlugin.Sdk/           # 插件开发 SDK
│   └── Plugins/
│       └── CPlusPlusEditor/    # 示例插件：C++ 编辑器
├── build.sh                    # macOS/Linux 构建脚本
├── build.bat                   # Windows 构建脚本
├── DHPlugin.sln                # 解决方案文件
└── README.md                   # 详细文档
```

### 运行主程序

**使用 dotnet run (开发模式):**
```bash
dotnet run --project src/DHPlugin.Host/DHPlugin.Host.csproj
```

**发布后运行:**
```bash
# macOS
./build.sh
./publish/macos-x64/DHPlugin.Host

# Linux
./build.sh
./publish/linux-x64/DHPlugin.Host

# Windows
build.bat
publish\windows-x64\DHPlugin.Host.exe
```

### 安装示例插件

示例插件包已生成在:
```
src/Plugins/CPlusPlusEditor/CPlusPlusEditor.dhplugin
```

在主程序中:
1. 点击 "➕ 安装插件" 按钮
2. 选择 `CPlusPlusEditor.dhplugin` 文件
3. 插件安装完成后，在列表中选择它
4. 点击 "▶️ 启用" 按钮

### C++ 编辑器插件功能

启用插件后，主内容区将显示 C++ 编辑器界面：

- **代码编辑区**: 编写 C++ 代码
- **编译按钮**: 调用系统 GCC/Clang 编译器编译代码
- **运行按钮**: 运行编译后的程序
- **输出区**: 查看编译和运行结果

### 示例代码

```cpp
#include <iostream>

int main() {
    std::cout << "Hello, DHPlugin!" << std::endl;
    std::cout << "Welcome to C++ Editor Plugin!" << std::endl;
    
    auto message = "Modern C++ is awesome!";
    std::cout << message << std::endl;
    
    return 0;
}
```

### 开发新插件

1. **创建插件项目:**
   ```bash
   mkdir MyPlugin
   cd MyPlugin
   ```

2. **创建项目文件 `MyPlugin.csproj`:**
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net8.0</TargetFramework>
       <Nullable>enable</Nullable>
       <LangVersion>latest</LangVersion>
       <ImplicitUsings>enable</ImplicitUsings>
     </PropertyGroup>
   
     <ItemGroup>
       <Reference Include="DHPlugin.Abstractions">
         <HintPath>../src/DHPlugin.Abstractions/bin/Release/net8.0/DHPlugin.Abstractions.dll</HintPath>
       </Reference>
     </ItemGroup>
   </Project>
   ```

3. **创建 `manifest.json`:**
   ```json
   {
     "id": "MyPlugin",
     "name": "我的插件",
     "version": "1.0.0",
     "description": "这是一个示例插件",
     "author": "你的名字",
     "minimumHostVersion": "1.0.0",
     "dependencies": []
   }
   ```

4. **实现插件类:**
   ```csharp
   using DHPlugin.Abstractions;
   
   namespace MyPlugin;
   
   public class MyPlugin : IPlugin
   {
       public PluginManifest Manifest { get; } = new()
       {
           Id = "MyPlugin",
           Name = "我的插件",
           Version = "1.0.0",
           Description = "这是一个示例插件",
           Author = "你的名字"
       };
   
       public PluginState State { get; private set; } = PluginState.Unloaded;
   
       public Task InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
       {
           State = PluginState.Loaded;
           return Task.CompletedTask;
       }
   
       public Task EnableAsync(CancellationToken cancellationToken = default)
       {
           State = PluginState.Enabled;
           return Task.CompletedTask;
       }
   
       public Task DisableAsync(CancellationToken cancellationToken = default)
       {
           State = PluginState.Disabled;
           return Task.CompletedTask;
       }
   
       public Task ShutdownAsync(CancellationToken cancellationToken = default)
       {
           State = PluginState.Unloaded;
           return Task.CompletedTask;
       }
   }
   ```

5. **构建并打包:**
   ```bash
   dotnet build -c Release
   # 手动创建 .dhplugin 包 (ZIP 格式)
   zip -r MyPlugin.dhplugin manifest.json MyPlugin.dll
   ```

### 系统要求

**编译器要求 (C++ 编辑器插件):**
- macOS: `xcode-select --install`
- Ubuntu/Debian: `sudo apt install g++`
- CentOS/RHEL: `sudo yum install gcc-c++`
- Windows: 安装 MinGW 或使用 WSL

### 故障排除

**插件无法加载:**
- 检查 manifest.json 格式是否正确
- 确保插件 ID 唯一
- 查看日志输出

**编译失败:**
- 确保系统已安装 GCC/G++
- 检查 PATH 环境变量是否包含编译器路径

**UI 不显示:**
- 确保插件已启用
- 检查 UI 贡献点注册是否正确

---

**DHPlugin Team** © 2026
