**《地图画板》软件操作说明文档**

1.  概述

目前，大多数的画图软件均为位图，无法无限放大；矢量画板通常限制了大小，无法与地图相结合；而专业的GIS软件会占用大量硬盘和内存空间，且操作过于专业，不适合非专业人员。本软件解决了以上痛点，是一个轻量级的以瓦片地图为底图的矢量画板软件，上手简单、运行速度快。同时，拥有《瓦片地图下载器》和《GPX工具箱》两个辅助软件，可以用于缓存离线地图、处理GPX文件。

1.  创新点

-   采用WPF技术，技术先进

-   使用显卡加速，图形性能更好

-   全部使用矢量图形，支持高分屏

-   使用ArcGIS Runtime作为框架，采用C++作为底层，执行效率高

-   界面干净、扁平，无广告

-   附带《瓦片地图下载器》和《GPX工具箱》，用于缓存离线地图、处理GPX文件

1.  软件及硬件要求

-   软件要求

| 系统             | Windows 10 x86/x64           |
|------------------|------------------------------|
| .Net平台         | .Net 4.7.2，C++ Runtime 2017 |
| 显示应用程序框架 | DirectX 11及以上             |

-   硬件最低要求

| CPU    | 第四代酷睿i3               |
|--------|----------------------------|
| 显卡   | HD4000、GTX620及同等级显卡 |
| 显示器 | 1440\*900                  |
| 硬盘   | 200M可用空间               |

1.  主界面介绍

![](docPic/image1.png)

1.  基本操作

-   新建样式

点击**新建样式**

![](media/aece8a8b6c6fdc6fefe28f4d2bf32fbe.png)

选择样式的类型，包括点、多点、线、面。一种要素只能指定一种类型。

![](media/6fcf7d65efb2d2fed86cf6fbe9832cb8.png)

新建后，样式列表会添加一条图层

![](media/4c7614b9c85c233c642da9aed72d537b.png)

-   绘制

点击左侧工具栏最下方的绘制按钮

![](media/6ec6a871ae32fdab25ffe6d22c8894f2.png)

即可在地图上绘制了

1.  绘制

-   进行绘制的条件

若需要绘制，则必须满足两个条件：

1.  在样式列表中选中了一个样式

2.  选中的样式为可见状态

-   开始绘制

要开始绘制，点击工具栏最下方的按钮。根据不同的样式类型，按钮的标题会不同。

![](media/6ec6a871ae32fdab25ffe6d22c8894f2.png)

对于线、面类型的样式，含有超过一种的绘制类型，要选择非默认绘制类型，点击

![](media/a0b93dc9645a2ee96eee4955862b7482.png)

，即可呼出所有的绘制选项。

![](media/826d6042b167f9bb6a8d35e37f1bc311.png)

当焦点在地图上时，按Enter也可开始默认绘制

-   绘制方式

点击地图上一点，即可在该点增加一个折点（对于点/多点来说，是点）。重复点击不同的地方，即可绘制出图形。

![](media/a662d31b023d82045a3004ac2153da21.png)

方形的点为控制点，是用于控制图形的形状。圆形白色的点是两个控制点的中点。选中该点，然后拖动，就可将之变为新的控制点。

若选中点不是最后一个（数字最大的）点，则下一个点将在这个点和下一个点之间插入。

点击面的边框，或线的非点位置，可以选中整个图形。继续拖动，可以将整体图形进行平移。

![](media/86205cec1ed85acd826becdef5d91c04.png)

-   控制栏

![](media/2c0586d78bec36b69ebfbf462a48e81f.png)

![](media/75a3ca020d4ca566d5e913436e4c18a9.png)

控制栏包含了绘制相关功能，将在开始绘制后自动弹出。

| 撤销     | 撤销上一个操作，快捷键：Ctrl+Z                               |
|----------|--------------------------------------------------------------|
| 恢复     | 恢复上一个撤销的操作，快捷键：Ctrl+Shift+Z或Ctrl+Y           |
| 标签     | 标签值，用于控制在图形上显示的标签的内容                     |
| 日期     | 日期值，用于指定与该图形该图形有关的日期，用于“时间范围”功能 |
| 删除节点 | 删除选中的节点，快捷键Delete                                 |

-   结束绘制

若要结束并保存绘制的内容，点击控制栏上的**完成**按钮，或按Enter

若要结束但不保存，点击控制栏上的**取消**按钮，或按Esc

1.  样式设置

通过修改工具栏上方的数据，可以修改样式的名称、渲染属性、标签属性

![](media/f286544802fc90816081eda34a38f7bb.png)

-   渲染器属性

|                    | 点       | 线       | 面           |
|--------------------|----------|----------|--------------|
| 线宽/边框宽/点大小 | 点的直径 | 线的宽度 | 面的边框宽度 |
| 边框/线颜色        |          | 线的颜色 | 边框的颜色   |
| 填充/点颜色        | 点的颜色 |          | 填充的颜色   |

当前设置的点、线、面：

![](media/8795efa35b7c9a8c92e7825f62b3e49e.png)

-   标签格式属性

| 字体颜色 | 标签字体的颜色                                                         |
|----------|------------------------------------------------------------------------|
| 边框颜色 | 字体描边的颜色                                                         |
| 字体大小 | 字体的大小，单位为磅                                                   |
| 边框宽度 | 字体描边的宽度                                                         |
| 最小比例 | 当地图缩放小于该比例时，标签将不会显示以增加性能 若该值为0，则总是显示 |

点击“设为当前”，可以将“最小比例”值设置为当前地图的缩放比例。

当前设置的标签：

![](media/d1d60d43e7b701955cb9ba1725565ca3.png)

-   控制按钮

![](media/52f3e51032a9126528157834117bf4cd.png)

浏览模式：取消选中所有样式，样式编辑被禁止

应用样式：样式编辑后应用当前样式，保存配置文件、刷新地图

新建样式：新建一个样式

1.  样式列表和功能

-   列表

![](media/1b2f747853b93de4a3fb6e84820a2a96.png)

样式列表共有三个控制列、三个信息列。

| 显示     | 控制样式的可见性         |
|----------|--------------------------|
| 标签     | 控制样式的标签可见性     |
| 时间     | 控制样式的时间范围的开关 |
| 标题     | 显示样式的标题           |
| 类型     | 显示样式的类型           |
| 图形数量 | 显示样式的图形的总数     |

在某一行上点击右键，将会呼出该样式的菜单

线：

![](media/88bf096ee8d0d42997f85dc076bb243d.png)

；其他：

![](media/e1f171a303eaacc0fa2518907ee4cd5e.png)

| 复制         | 将该样式内所有元素复制到另一个类型相同的图层中 |
|--------------|------------------------------------------------|
| 建立缓冲区   | 将线转为面                                     |
| 删除         | 删除该图层，包括硬盘中的文件                   |
| 新建副本     | 建立样式的拷贝，可以选择是否包含图形           |
| 缩放到图层   | 将画面边界转变到该样式的可以容纳所有图形的边界 |
| 坐标转换     | 将样式所有图形在几个坐标系之间进行转换         |
| 设置时间范围 | 设置图形可见的时间范围                         |
| 导出         | 导出单个样式                                   |

-   菜单-复制

复制前，需要确定存在除该样式外至少一个相同类型的样式，否则会弹出错误:

![](media/fb8e737b209bb36c9f81e33d38167aa0.png)

点击复制后，选择目标样式，点击确定完成复制

![](media/658a21abc71ab12c3686b562b44506bb.png)

-   菜单-建立缓冲区

此操作仅针对于线。建立缓冲区后，线样式会转换成面样式。缓冲区半径在设置面板中设置。

![](media/d5ff33c1055aa56dffb91f0b5a7ad96c.png)

点击**新建副本**后，会弹出对话框：

![](media/568d3da0bf1ed1717ed4ba02961cfb7b.png)

若选择**仅样式**，则只会复制样式属性，而不复制样式中的图形；选择后者，则会建立除名称外完全一样的副本。

-   菜单-坐标转换

点击**坐标转换**后，会弹出对话框：

![](media/24773287665d9f04a80de5322c05a23a.png)

选择当前坐标系和目标坐标系，点击**确定**，即可转换所有图形到新的坐标系。建议转换前先做备份。

如，GCJ02转换为CGCS2000后：

![](media/aa6fbdbc506e5b0a51e4b077cbc9026b.png)

-   菜单-设置时间范围

点击**设置时间范围**，将打开设置对话框：

![](media/710418f6c6458f7f6762ebc9d99bbd04.png)

选择是否启用，以及起止时间。则该样式的图形只会显示指定时间内的图形。对于没有设置过日期的图形，若启用了时间范围功能，则一律不显示。

-   菜单-导出

点击**导出**，则可将当前样式导出。目前暂时仅支持mblpkg格式，实质是zip文件。

![](media/1f2fdd64eca8b1e4059a9e47c16e7333.png)

-   全样式操作

央视列表下有三个按钮：

![](media/671a384e04e15802d5a3ef7cb79406ed.png)

。

*导入*：

![](media/8168ccf49f8a43eb36ffd6a5287999d4.png)

对于“地图画板包”，导入后将覆盖所有当前样式，请注意备份。

*导出*：

![](media/c413f5141102f5c96cc76cbe98bbd154.png)

*打开目录*：将打开存放样式数据的目录。若不熟悉Shapefile的结构，请不要随意修改。

1.  图形相关功能

-   选择图形

选择有两种方式：点选和框选。

框选：点击绘制按钮右边的

![](media/bee80e635bbc16b5e77c783b98881ebf.png)

按钮，开始选择。按下画面上的一点不松手，拖动到另一点释放，完成框选。

点选：点击

![](media/bee80e635bbc16b5e77c783b98881ebf.png)

按钮开始选择后，点击图形，就能选中图形。也可在一般情况下按住Ctrl键进行选择。

多选：选择第二个及更多个图形时，按住Ctrl或Shift进行多选

反选：按住Alt进行点选时，若该图形已被选中，则会取消选中

选中后，图形周围将会有蓝色的外发光，代表已经选中：

-   选择操作栏

选中面图形的操作栏：

![](media/d456ca5fc37ee1dd6219e014cfd342d1.png)

选中线图形的操作栏：

![](media/549c7332f5ccd5e82c80d4327bf11035.png)

选中一个以上的图形时，顶部会自动弹出选择操作栏。

当选中线时，会显示长度信息；当选中面时，会显示周长和面积信息。

信息右侧有六个按钮，分别代表不同的功能；其他一栏中，根据类型、所选图形数量的不同，会有不同的功能。以下是功能列表：

| 取消   | 清除选择                               |
|--------|----------------------------------------|
| 删除   | 删除选中的图形                         |
| 复制到 | 将选中的图形复制到其他相同类型的样式中 |
| 分割   | 将一个图形分割成多个部分               |
| 编辑   | 重新将图形变为可编辑状态，进行编辑     |
| 合并   | 将两个线/面图形进行合并，作为一个整体  |
| 连接   | 将两条以上的线进行端点之间的连接       |

-   分割

点击分割后，像绘制新的图形一样，绘制一条经过已有图形的折线：

![](media/4fad717932af0a92274a6d69ac4f2dcb.png)

然后点击

![](media/3b34a0a3a4fbe48464ac0f924dc93071.png)

，即可完成分割：

![](media/2c71c37d0e5a09715b3c549dceedf6b0.png)

-   连接

当选择的是线且数量大于等于2个时，可用连接功能。如：

![](media/6b7a14727b7798b9c429150f69fffd0c.png)

点击**连接**后，根据选择数量会弹出不同的对话框。若选择的数量为2，则弹出：

![](media/f99d593a8dbcfe21cb49f4e8cc28dd7e.png)

若选择的数量大于2，则弹出：

![](media/05a38277f64a28056f098c7e01f4e134.png)

不同的选项适用不同的情况。如：

![](media/f4d8bedee2bd841016592b4bc7e81710.png)

适合尾1头——头2尾巴；

![](media/c39fe9a0ae455f461852f1cbaf5c4c48.png)

适合使用头n尾——头n+1尾。

连接成功后，所有线段端点相连并合并：

![](media/68c9ac04ecdb485110eec064a8ae4eeb.png)

1.  设置面板

设置面板默认为折叠状态，需要点击后展开。

![](media/27ac888e7febd69de7a162b9b95527b7.png)

底图为GCJ02坐标系：用于导入GPX文件。GPX文件固定为WGS84坐标系，若直接导入到GCJ02坐标系底图的地图中，会产生偏移，故增加此选项来进行自动转换。

保留上一次绘制的标签：当进行连续绘制时，不会清空标签

瓦片地图地址：地图底图的瓦片地址，每一行代表一个图层，下方的地址图层靠上。默认为天地图。

转为面时缓冲区大小：建立缓冲区时的缓冲区半径

面板最下方有两个按钮，可以打开两个小工具。

1.  小工具

《地图画板》自带两个小工具程序，为《GPX工具箱》和《瓦片下载拼接器》。

《GPX工具箱》用于查看GPX的信息、轨迹，计算相关信息，并提供部分编辑功能。

《瓦片下载拼接器》用于下载和拼接网络地图，并可以作为缓存使用。

![](media/f759284115a91faf31e708b319db2f5d.png)

![](media/4254b1a6f3cdadca34db878b7ee60edf.png)
