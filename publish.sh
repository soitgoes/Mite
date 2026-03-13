#!/bin/sh

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/Mite/Mite.Console.csproj"
OUTPUT_DIR="$SCRIPT_DIR/publish"
RUNTIME="win-x64"
CONFIGURATION="Release"

echo "Publishing Mite as a self-contained single-file executable for Windows ($RUNTIME)..."
echo ""

dotnet publish "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --runtime "$RUNTIME" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    --output "$OUTPUT_DIR"

mv "$OUTPUT_DIR/Mite.Console.exe" "$OUTPUT_DIR/mite.exe"

echo ""
echo "Done. Output:"
ls -lh "$OUTPUT_DIR/mite.exe"
echo ""
echo "Copy $OUTPUT_DIR/mite.exe to any Windows machine -- no .NET install required."
