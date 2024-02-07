dotnet build
dotnet test
dotnet publish -r win-x64 --self-contained true
Compress-Archive -Path "bin\Release\net8.0\win-x64\publish\" -DestinationPath "publish.zip" -Force 