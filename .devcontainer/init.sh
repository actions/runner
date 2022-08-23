### Create _dotnetsdk
echo 'Creating symlink for embedded sdk...'
ln -s /home/codespace/.dotnet/sdk/ _dotnetsdk 
echo .6.0.300 >> _dotnetsdk/6.0.300/.6.0.300
ln -s /home/codespace/.dotnet/dotnet _dotnetsdk/6.0.300/dotnet

### Restore 
dotnet restore src/Runner.Listener &
dotnet restore src/Runner.Common &
dotnet restore src/Runner.Sdk &
dotnet restore src/Runner.Worker &
dotnet restore src/Test &
