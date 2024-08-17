dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net8.0-windows --output Output\Windows\x64\Release\Desktop-Wallet\Net8\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net7.0-windows --output Output\Windows\x64\Release\Desktop-Wallet\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net6.0-windows --output Output\Windows\x64\Release\Desktop-Wallet\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Release --framework net5.0-windows --output Output\Windows\x64\Release\Desktop-Wallet\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Debug --framework net7.0-windows --output Output\Windows\x64\Debug\Desktop-Wallet\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Debug --framework net6.0-windows --output Output\Windows\x64\Debug\Desktop-Wallet\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Desktop-Wallet\SeguraChain-Desktop-Wallet.csproj --configuration Debug --framework net5.0-windows --output Output\Windows\x64\Debug\Desktop-Wallet\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

pause