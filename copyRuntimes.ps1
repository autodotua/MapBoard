Copy-Item C:\Users\$($env:USERNAME)\.nuget\packages\esri.arcgisruntime.runtimes.win\200.4.0\runtimes\win-x64\native\* MapBoard.UI
Copy-Item C:\Users\$($env:USERNAME)\.nuget\packages\esri.arcgisruntime.wpf\200.4.0\runtimes\win-x64\native\* MapBoard.UI
Write-Output "复制完成"
pause