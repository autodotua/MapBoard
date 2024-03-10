dotnet publish MapBoard.MAUI -f net8.0-android -c Release -p:AndroidKeyStore=true 
Invoke-Item MapBoard.MAUI\bin\Release\net8.0-android\publish
pause