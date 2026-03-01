# UnityNvg
Is a port of [Nvg.NET](https://github.com/zvasya/Nvg.NET) (fork of [SilkyNvg](https://github.com/SilkCommunity/SilkyNvg) (a port of [memononen/nanovg](https://github.com/memononen/nanovg/) to .NET)) for Unity.

## Overview
This package provides a robust implementation for vector graphics rendering, built upon the Nvg.NET libraries. It implements the INvgRenderer interface, offering a streamlined way to integrate NanoVG-style rendering into various Unity workflows.

## 🛠 Features & Examples
The project includes comprehensive examples for three primary use cases:

 - Render to Texture: Demonstrates how to bake vector graphics into textures using CommandBuffer. This is ideal for UI elements or dynamic decals.

 - Editor Window Integration: Shows how to use the ImmediateModeElement to draw custom vector interfaces directly within the Unity Editor.

 - Legacy UI (OnGUI): Provides a straightforward implementation for rendering within the standard OnGUI method.
