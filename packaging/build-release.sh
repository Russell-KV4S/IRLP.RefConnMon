#!/usr/bin/env bash
# Builds the release zips for every supported platform into ./artifacts.
#
#   packaging/build-release.sh           # version from IRLP.RefConnMon.csproj
#   packaging/build-release.sh 1.4.0     # override the version (the release workflow passes the tag)
#
# Needs the .NET 10 SDK and zip. Every platform can be built from any OS.
set -euo pipefail

cd "$(dirname "$0")/.."
VERSION="${1:-}"
OUT="artifacts"
NOTE="Do Not copy the config file if you already set one up.txt"

# Runtime identifiers to build. win-x64 keeps the historic IRLP.RefConnMon.zip name so existing links keep working.
RIDS=(win-x64 win-arm64 linux-x64 linux-musl-x64 linux-arm64 linux-musl-arm64 linux-arm osx-x64 osx-arm64)

rm -rf "$OUT"
mkdir -p "$OUT"

for rid in "${RIDS[@]}"; do
    stage="$OUT/stage/$rid"
    args=(-c Release -r "$rid" --self-contained false -p:DebugType=None -o "$stage")
    if [[ -n "$VERSION" ]]; then
        args+=(-p:Version="$VERSION")
    fi
    echo "==> $rid"
    dotnet publish IRLP.RefConnMon "${args[@]}"

    cp "packaging/notes/$rid.txt" "$stage/$NOTE"
    if [[ -f "$stage/IRLP.RefConnMon" ]]; then
        chmod +x "$stage/IRLP.RefConnMon"
    fi

    if [[ "$rid" == "win-x64" ]]; then
        zipname="IRLP.RefConnMon.zip"
    else
        zipname="IRLP.RefConnMon-$rid.zip"
    fi
    (cd "$stage" && zip -q -X "../../$zipname" ./*)
done

rm -rf "$OUT/stage"
ls -l "$OUT"
