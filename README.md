# 地图画板

## 简介

《地图画板》是一个以地图为底图的矢量画板软件，支持图层管理、分类上色、标签显示、简单分析、快速编辑等功能，上手简单、运行速度快。同时，配有《瓦片地图下载器》和《GPX工具箱》两个辅助软件，可以用于缓存离线地图、处理GPX文件。

## 结构

| 项目名称 | 介绍                                                  |
| -------- | ----------------------------------------------------- |
| Model    | 包含一些基础的模型/数据类                             |
| Core     | 包含一些地理和地图的工具类、输入输出类、图层管理      |
| UI       | 以WPF为技术的桌面端GUI程序                            |
| MAUI     | 以MAUI为技术的跨平台多端轻量版程序（暂仅支持Android） |

## 截图

### 桌面版

主界面
![](imgs/MapBoard_1.jpg)

选择界面
![](imgs/MapBoard_2.jpg)

编辑界面
![](imgs/MapBoard_3.jpg)

GPX工具箱
![](imgs/GpxToolBox.jpg)

地图瓦片下载拼接器
![](imgs/TileDownloaderSplicer.jpg)

### 手机版

![](imgs/MAUI.jpg)

## 注意事项

- 执行 `.build.ps1`能够一键构建，位于 `Generation\Publish`中。
- `MapBoard.Package`项目用于生成MSIX安装包或者提交到Windows应用商店中。