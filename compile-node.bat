dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net7.0 --output Output\Linux\x64\Debug\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Debug --framework net7.0 --output Output\Windows\x64\Debug\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true

dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net7.0 --output Output\Linux\x64\Release\ --runtime linux-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true
dotnet publish SeguraChain-Peer\SeguraChain-Peer.csproj --configuration Release --framework net7.0 --output Output\Windows\x64\Release\ --runtime win-x64 --verbosity quiet --self-contained true -p:PublishSingleFile=true


pause