# <img src="https://raw.githubusercontent.com/null511/PixelGraph/master/media/icon.png" height="28"/>&nbsp; PixelGraph&nbsp; [![Actions Status](https://github.com/null511/PixelGraph/workflows/Release/badge.svg)](https://github.com/null511/PixelGraph/actions)

PixelGraph is an application for publishing Minecraft resource packs, specially tooled for PBR materials. It allows you to work in a "raw" texture space and automates publishing to one or more encodings, rather than trying to directly encode your textures as design-time. Yaml configuration files can also be used to apply final adjustments to your compiled textures. A cross-platform command-line version is also available, allowing you to completely automating your publishing process from your remote content repository.

<img src="https://github.com/null511/PixelGraph/raw/master/media/UI.png" alt="User Interface" />

 - **Simplify your workflow** by adjusting text instead of pixels. Getting image-based material values just right can be tedious, time consuming, and destructive.

 - **Preserve Quality** by adjusting material values through text rather than altering the original image data. Repeatedly scaling the integer-based channels of your image slowly destroys quality. Save the gradients!

 - **Support more users** by publishing multiple packs with varying quality. The resolution and included textures can be altered using either the command-line or Publishing Profiles to create multiple distributions.

 - **Automate** Normal & AO generation, resizing, and channel-swapping so that you can spend more time designing and less time repeating yourself.

<img src="https://github.com/null511/PixelGraph/raw/master/media/LAB11.png" alt="PBR Workflow" />

### Normal-Map Generation

Allows normal-map textures to be created from height-maps as needed during publishing, or by prerendering them beforehand. Strength, blur, and wrapping can be managed using the textures matching pbr-properties file.

<img src="https://github.com/null511/PixelGraph/raw/master/media/NormalGeneration.png" alt="Normal-Map from Height-Map" height="180px"/>
 
### Occlusion-Map Generation

Allows ambient-occlusion textures to be created from height-maps as needed during publishing, or by prerendering them beforehand. Quality, Z-scale, step-count, and wrapping can be managed using the materials properties.

<img src="https://github.com/null511/PixelGraph/raw/master/media/OcclusionGeneration.png" alt="Occlusion-Map from Height-Map" height="180px"/>

## Installation

For manual installation, download the latest standalone executable from the [Releases](https://github.com/null511/PixelGraph/releases) page. For automated usage see [Docker Usage](https://github.com/null511/PixelGraph/wiki/Installation#docker). Visit the [wiki](https://github.com/null511/PixelGraph/wiki/Installation) for more information.

## Manual Usage

A single Pack-Input file lives in the root of the workspace (`~/input.yml`) which specifies the default formatting of all content. The example below uses a 'raw' encoding which manages each channel in a different texture; The 'height' channel has been set to inverted.

```yml
# ~/input.yml
format: raw
height:
  invert: true
```

One or more Pack-Profiles are used to describe a publishing routine; they also live in the project root and should match the naming convention `~/<name>.pack.yml`. Each profile can specify pack details, encoding, format, resizing, etc; this allows a single set of content to be published for multiple resolutions and encodings, ie `pbr-lab1.3-64x` or `default-128x`

```yml
# ~/pbr-lab13-x64.yml
output:
  format: default
texture-scale: 0.5
```

Material files are used to desribe a collection of textures that compose a single game "item". For more details, see the [Wiki](https://github.com/null511/PixelGraph/wiki/File-Loading).
```yml
# ~/assets/minecraft/textures/block/lantern.pbr.yml
smooth:
  scale: 1.2
metal:
  scale: 0.8
emissive:
  scale: 0.2
```

## Sample Repository

[Oversized](https://github.com/null511/MCRP-Oversized) - A high-resolution texture pack made primarily using stock textures and CTM.

[Textureless](https://github.com/null511/MCRP-Textureless) - A low-resolution pack using solid colors, material values, and alpha-masking to provide a smooth visual base for shader testing. Also good for toon/outline style.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## Support
Ask about PixelGraph in the [ShaderLabs Discord](https://discord.gg/PG9RmWTBU9) under `#null-pbr-and-pixelgraph`.

## PixelGraph 101 Windows UI

**__PixelGraph 101__** (For Windows UI)

**`Creating A Project :`**
1) Download PG if you haven't already downloaded it- <https://github.com/null511/PixelGraph> Click on the first thing under `Releases`, then proceed to download **__`PixelGraph-UI-Windows-x64.exe`__**. (PixelGraph will be referred to PG from now on.)
- 1.1) If met with a this file is dangerous warning, click on the up arrow, and then click on **Keep**.
- 1.2) You will be met with a popup from Windows Defender SmartScreen, proceed to click on **More Info**, then on **Run anyway**. This program is completely safe!
2) After PG loads, click on **New Project**,  under Resource Pack Name set your RP's name, click on the folder icon, and set your directory (Make a folder in *your desktop*).
- 2.1) Click on **Next**, and then **Create**.
- 2.2) Download `.NET Core 3.1 Desktop Runtime (v3.1.24) - Windows x64` if you haven't already.
**Congrats! You have created a project!**

**`Importing Textures :`**
1) Firstly, you need to get your required textures- Albedo, Normal & Roughness/Smoothness. You can draw them, or procedurally make them using softwares like Substance Desinger, or download from CC0 sites- <https://ambientcg.com/>, <https://www.sharetextures.com/>.
2) Now with PG open with the project, navigate to your project folder, assets > minecraft > textures > block > make a folder for a block. We will be taking bricks as example.
- 2.1) This is the bricks texture we will be using- https://ambientcg.com/view?id=Bricks077. I will be downloading the 1k res version. After extracting, I 
