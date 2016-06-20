# Markdown Editor

[![Build status](https://ci.appveyor.com/api/projects/status/m07cnunnni8w82o5?svg=true)](https://ci.appveyor.com/project/madskristensen/markdowneditor)

Download this extension from the [VS Gallery](https://visualstudiogallery.msdn.microsoft.com/eaab33c3-437b-4918-8354-872dfe5d1bfe)
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
- Outlining/folding of code blocks
- Hotkeys for easy add bold and italic text
- Brace completion with type-through
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

Live preview can be disabled in the [settings](#settings).

> The syntax highligter is powered by [Prism](http://prismjs.com/)

### Drag 'n drop images
Drag an image directly from Solution Explorer onto the document to
insert the appropriate markdown that will render the image.

### Paste images
This is really helpful for copying images from a browser or for
inserting screenshots. Simply copy an image into the clipboard and
paste it directly into the document. This will prompt you for a file
name relative to the document and then it inserts the appropriate
markdown.

It will even parse the file name and make a friendly name to use
for the alt text.

### Outlining
Any fenced code and HTML blocks can be collapsed, so that tihs:

![Outlining Expanded](art/outlining-expanded.png)

...can be collapsed into this:

![Outlining Collapsed](art/outlining-collapsed.png)

### Hotkeys
Hotkeys are available for making text bold and italic. Select the
text and hit **Ctrl+b** for bold and **Ctrl+i** for italic.

Bold will surround the selected text with `**` and italic surrounds
with `_`.

This feature overrides build in commands such as
_Incremental Search_ so they hotkeys can be disabled in the
[settings](#settings).

### Brace completion with type-through
This makes typing faster. Whenever you type opening braces,
paranthesis or brackets, a corrosponding closing character is
inserted. It is smart about when it adds the closing character
so it doesn't become annoying.

It also inserts `*` and `_` characters to make typing bold and
italic text as fast as possible.

This feature can be disabled in the [settings](#settings).

### Settings
Control the settings for this extension under
**Tools -> Options -> Text Editor -> Markdown**

![Options](art/options.png)

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