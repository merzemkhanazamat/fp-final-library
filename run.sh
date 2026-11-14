set -eu
cd "$(dirname "$0")"
sh Native/build.sh
dotnet run --project App/App.csproj -- "${1:-Data/library.csv}"
