Copy-Item C:\Users\$($env:USERNAME)\.nuget\packages\esri.arcgisruntime.runtimes.win10\200.2.0\runtimes\win10-x64\native\* MapBoard.UI
Copy-Item C:\Users\$($env:USERNAME)\.nuget\packages\esri.arcgisruntime.wpf\200.2.0\runtimes\win10-x64\native\* MapBoard.UI
Write-Output "复制完成"
pause