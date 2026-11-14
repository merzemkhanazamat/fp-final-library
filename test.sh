set -eu
cd "$(dirname "$0")"
sh Native/build.sh
dotnet run --project Tests/Tests.csproj
