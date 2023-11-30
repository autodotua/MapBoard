param(
    [Parameter()]
    [switch]$w,
    [switch]$d,
    [switch]$s,
    [switch]$f
)
try {
    
    Write-Output "请先阅读ReadMe"
    Write-Output "请确保："
    Write-Output "已经安装.NET 8 SDK"
    Write-Output "已经安装Microsoft Visual C++ 2015-2019 Redistributable"

    pause
    Clear-Host

    try {
        dotnet
    }
    catch {
        throw "未安装.NET SDK"
    }
    
    Clear-Host
    try {
        Remove-Item Generation/Publish -Recurse
    }
    catch {
    }
   
    

    Write-Output "正在发布WPF"
    dotnet publish MapBoard.UI -c Release -o Generation/Publish/WPF/bin -r win10-x64 --self-contained false
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut("$PSScriptRoot\Generation\Publish\WPF\MapBoard.lnk")
    $shortcut.TargetPath = "$PSScriptRoot\Generation\Publish\WPF\bin\MapBoard.exe"
    $shortcut.Save()
    
    
    Write-Output "正在发布WPF（单文件）"
    dotnet publish MapBoard.UI -c Release -o Generation/Publish/WPF_SingleFile -r win10-x64 --self-contained false /p:PublishSingleFile=true
    Copy-Item Generation/Publish/WPF/Extension.*.dll Generation/Publish/WPF_SingleFile

    Write-Output "正在发布WPF（包含框架，单文件）"
    dotnet publish MapBoard.UI -c Release -o Generation/Publish/WPF_Contained_SingleFile -r win-x64 --self-contained true /p:PublishSingleFile=true
    Copy-Item Generation/Publish/WPF/Extension.*.dll Generation/Publish/WPF_Contained_SingleFile
    # 暂时无法发布包含框架的单文件应用
    # 从 .NET 8 开始，该值是为其生成了运行时的 RID。 
    # 这意味着对于运行时的可移植生成（所有 Microsoft 提供的生成），该值不特定于版本且不特定于发行版。 
    # 例如，在 Windows 11 上，该值为 win-x64，在 Ubuntu 20.04 上，该值为 linux-x64。 对于不可移植的生成（源生成），
    # 生成将设置一个可以具有版本和发行版的生成 RID，该值就是返回的 RID。
    # 但是ArcGIS Maps SDK要求必须指定win10-x64，
    # 这就造成了冲突

    Write-Output "正在清理"
    Remove-Item Generation/Release -Recurse

    Write-Output "操作完成"

    Invoke-Item Generation/Publish
    pause
}
catch {
    Write-Error $_
}