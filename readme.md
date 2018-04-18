# Custom File Icons

![Example](example.png)

Configuration-based file association manager for Windows with the goal of providing the file explorer with, as Atom's [file-icons](https://github.com/file-icons/atom) puts it, "file-specific icons for improved visual grepping."

This is not a full suite of icons but a tool for assigning your own. Normally, setting various extensions to open by default with an editor, for example, would result in everything having the program's icon. This tool can be used instead to give each file type a distinct name and icon along with custom associations. It can also add additional menu items, such as "Pack" and "Unpack" to NuGet specs and packages, respectively, and the configuration file and icons can be synced across machines.

## Usage

Clone the repo and build the solution in Release mode. The configuration and icons provided are my own and can be replaced; see the schema for details. SVGs can be drag & dropped onto the convert-svg.bat script to convert them. Run apply.bat (or the main project from VS) to apply the configuration, and set this "app" as the default for all extensions in the window that opens. Note that the file associations will be bound to the location of the repo; rerun if moving the directory.

## Changes

This is the third iteration of the tool. The first, lost to the void, was a ginormous bash script that used a combination of Inkscape and ImageMagick to generate icons and registry files from templates (being difficult at the time to find quality icons for a variety of file types).

The second, found here in the [v2](https://github.com/maxkagamine/custom-file-icons/tree/v2) branch, abandoned the templated icons and registry files, instead using a Python script to forcibly nuke and set file associations based on a config file and directory of icons, most of which generated from [file-icons](https://github.com/file-icons/atom)' fonts and later [vscode-icons](https://marketplace.visualstudio.com/items?itemName=robertohuertasm.vscode-icons)' SVGs using Node.

This latest version is a C# rewrite that instead registers itself as an available "Default App/Program" for better Windows 10 compatibility (and to avoid Edge likewise forcing its way back in as the default), now that programmatically setting file associations for the user is no longer permitted. The Node-based SVG converter has also been replaced.

## TODO

Due to a limitation of Windows file associations, only the extension at the end is considered. It should be possible, however, to write a [shell extension](https://msdn.microsoft.com/en-us/library/windows/desktop/cc144122(v=vs.85).aspx) that provides a custom icon based on the entire filename, so that e.g. "package.json" gets an NPM icon instead of a JSON one.
