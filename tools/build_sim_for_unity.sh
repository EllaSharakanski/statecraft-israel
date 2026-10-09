#!/usr/bin/env bash
# Publishes the pure C# sim (netstandard2.1) plus YamlDotNet into Unity's plugin folder.
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet publish sim/Statecraft.Sim -c Release -o unity/Assets/Plugins/Statecraft
ls unity/Assets/Plugins/Statecraft/Statecraft.Sim.dll unity/Assets/Plugins/Statecraft/YamlDotNet.dll
