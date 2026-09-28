#!/usr/bin/env bash
# Builds the release zips for every supported platform into ./artifacts.
#
#   packaging/build-release.sh           # version from IRLP.RefConnMon.csproj
#   packaging/build-release.sh 2.1.0     # override the version (the release workflow passes the tag)
#
# Needs the .NET 10 SDK and zip. Every platform can be built from any OS.
set -euo pipefail

cd "$(dirname "$0")/.."
VERSION="${1:-}"
OUT="artifacts"
NOTE="Do Not copy the config file if you already set one up.txt"

# Runtime identifiers to build. win-x64 keeps the historic IRLP.RefConnMon.zip name so existing links keep working.
# "any" is a portable build with no native launcher that runs anywhere .NET 10 does (dotnet IRLP.RefConnMon.dll).
RIDS=(win-x64 linux-x64 linux-arm64 linux-arm any)

rm -rf "$OUT"
mkdir -p "$OUT"

for rid in "${RIDS[@]}"; do
    stage="$OUT/stage/$rid"
    args=(-c Release --self-contained false -p:DebugType=None -o "$stage")
    if [[ "$rid" == "any" ]]; then
        args+=(-p:UseAppHost=false)
    else
        args+=(-r "$rid")
    fi
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
    (cd "$stage" && zip -q -X -r "../../$zipname" ./*)
done

rm -rf "$OUT/stage"
ls -l "$OUT"
