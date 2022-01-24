# Jellyfin IMVDb Plugin

## Part of the [Jellyfin Project](https://jellyfin.org)

## Build Process

1. Clone or download this repository

2. Ensure you have .NET SDK setup and installed

3. Build plugin with following command.

```sh
dotnet publish --configuration Release --output bin
```
4. Place the resulting file in the `plugins` folder under the program data directory or inside the portable install directory
