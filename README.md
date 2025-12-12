# Derail Valley Better Chase Camera mod

A mod for the game [Derail Valley](https://store.steampowered.com/app/588030/Derail_Valley/) that improves on the default external camera:

When you switch to first person camera, it will remember the state of the external camera.

When you switch back to external camera, if you have done so before, it will restore the previous state.

The camera will "freeze" to the previous state until you move your mouse, then it resets to normal.

## Install

Download the zip and use Unity Mod Manager to install it.

## Development

Template from https://github.com/derail-valley-modding/template-umm

Created in VSCode (with C# and C# Dev Kit extensions) and MSBuild.

1. Run `msbuild` in root to build

## Publishing

1. Run `.\package.ps1`
