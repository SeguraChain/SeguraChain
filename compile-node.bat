dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net9.0 --output Output\Node\Linux\x64\Debug\Net9\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net9.0 --output Output\Node\Windows\x64\Debug\Net9\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net8.0 --output Output\Node\Linux\x64\Debug\Net8\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net8.0 --output Output\Node\Windows\x64\Debug\Net8\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net8.0 --output Output\Node\Linux\x64\Remease\Net8\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net8.0 --output Output\Node\Windows\x64\Remease\Net8\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true


dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Remease --framework net9.0 --output Output\Node\Linux\x64\Release\Net9\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net9.0 --output Output\Node\Windows\x64\Release\Net9\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true


dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net7.0 --output Output\Node\Linux\x64\Debug\Net7\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net7.0 --output Output\Node\Windows\x64\Debug\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net7.0 --output Output\Node\Linux\x64\Release\Net7\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net7.0 --output Output\Node\Windows\x64\Release\Net7\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net6.0 --output Output\Node\Linux\x64\Debug\Net6\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net6.0 --output Output\Node\Windows\x64\Debug\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net6.0 --output Output\Node\Linux\x64\Release\Net6\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net6.0 --output Output\Node\Windows\x64\Release\Net6\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net5.0 --output Output\Node\Linux\x64\Debug\Net5\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net5.0 --output Output\Node\Windows\x64\Debug\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net5.0 --output Output\Node\Linux\x64\Release\Net5\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net5.0 --output Output\Node\Windows\x64\Release\Net5\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true


pause