set -eu
cd "$(dirname "$0")/.."
mkdir -p runtimes
case "$(uname -s)" in
    Darwin) cc -dynamiclib -Wall -Wextra -Werror Native/fee.c -o runtimes/liblibraryfee.dylib ;;
    Linux) cc -shared -fPIC -Wall -Wextra -Werror Native/fee.c -o runtimes/liblibraryfee.so ;;
    *) echo "Use macOS or Linux"; exit 1 ;;
esac
