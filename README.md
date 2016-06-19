# Markdown Editor

[![Build status](https://ci.appveyor.com/api/projects/status/m07cnunnni8w82o5?svg=true)](https://ci.appveyor.com/project/madskristensen/markdowneditor)

<!-- Update the VS Gallery link after you upload the VSIX-->
Download this extension from the [VS Gallery](https://visualstudiogallery.msdn.microsoft.com/[GuidFromGallery])
or get the [CI build](http://vsixgallery.com/extension/9ca64947-e9ca-4543-bfb8-6cce9be19fd6/).

---------------------------------------

A full featured Markdown editor with live preview and syntax highligting. 
Supports GitHub flavored Markdown.

See the [changelog](CHANGELOG.md) for changes and roadmap.

## Features

- Powered by [Markdig](https://github.com/lunet-io/markdig) - the best markdown parser
- Syntax highlighting supporting GitHub flavor
- Live preview window
- High-DPI support
- Drag 'n drop of images supported
- Paste image from clipboard directly onto document
- Lightning fast

### Syntax highlighting
All fonts can be changed in **Tools -> Options -> Environment -> Fonts and Colors**
dialog.

![Font Options](art/font-options.png)

#### GitHub and other flavors
Advanced markdown extensions are supported to give more features to
the syntax. This includes pipe tables, emoji, mathematics and a lot
more.

#### Live Preview Window
The preview window opens up on the right side of the document when
it opens. 

![Preview window](art/preview-window.png)

Every time the markdown document is saved, the preview window will
update and maintain the scroll position.

Any code blocks recieves full syntax highligting in the preview
window. Here's an example of JavaScript code rendered.

![Code Colorizing](art/code-colorizing.png)

> The syntax highligter is powered by [Prism](http://prismjs.com/)

### Drag 'n drop images
Drag an image directly from Solution Explorer onto the document to
insert the appropriate markdown that will render the image.

### Paste images from clipboard
This is really helpful for copying images from a browser or for
inserting screenshots. Simply copy an image into the clipboard and
paste it directly into the document. This will prompt you for a file
name relative to the document and then it inserts the appropriate
markdown.

It will even parse the file name and make a friendly name to use
for the alt text.

## Contribute
Check out the [contribution guidelines](.github/CONTRIBUTING.md)
if you want to contribute to this project.

For cloning and building this project yourself, make sure
to install the
[Extensibility Tools 2015](https://visualstudiogallery.msdn.microsoft.com/ab39a092-1343-46e2-b0f1-6a3f91155aa6)
extension for Visual Studio which enables some features
used by this project.

## License
[Apache 2.0](LICENSE)