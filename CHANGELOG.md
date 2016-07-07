# Roadmap

- [ ] Buttons to switch between Split/Source/Rendered mode
- [ ] Show/hide the preview window when Options change
- [ ] Format document/selection (#12)
- [ ] Support for non-ascii image URLs (#20)
- [ ] Generate .html files nested under the markdown file
- [ ] Validate local image and link paths
- [ ] Intellisense for local image and link paths

Features that have a checkmark are complete and available for
download in the
[CI build](http://vsixgallery.com/extension/9ca64947-e9ca-4543-bfb8-6cce9be19fd6/).

# Changelog

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

## 1.5

**2016-07-07**

- [x] Item template for Markdown file
- [x] Outlining per heading (#21)
- [x] Hotkey for wrapping selection in a code block
- [x] Turned line numbers off by default

## 1.4

**2016-06-29**

- [x] Toggle live sync on individual document
- [x] Navigational dropdowns
- [x] Keyboard navigation between headings (Ctrl+PgUp/Down)

## 1.3

**2016-06-24**

- [x] Smart completion of lists, task lists and blockquotes
- [x] Smoother sync scroll with no-delay
- [x] Smart tab taking into account list indentation
- [x] Button in editor to _Copy as HTML_
- [x] Option to load preview window below editor
- [x] Convert to LanguageService implementations

## 1.2

**2016-06-20**

- [x] Auto-update preview window while typing
- [x] Sync scroll position
- [x] Enter key maintains left whitespace matching previous line
- [x] Tab and `Shift+Tab` increase/decrease indentation of list items
- [x] Light bulbs for converting to image/link/codeblock/quote
- [x] `Ctrl+Space` on a task list item toggles the checked state

## 1.1

**2016-06-19**

- [x] Multiline comment outlining
- [x] Convert tabs to spaces
- [x] Comment and uncomment support
- [x] Brace completion with typethrough
- [x] `CTRL+B` makes text bold
- [x] `CTRL+I` makes text italic
- [x] Open remote links in default browser


## 1.0

**2016-06-18**

- [x] Syntax highligting
- [x] Preview window
- [x] Added GitHub stylesheet
- [x] Store width of preview window
- [x] Options to disable preview window
- [x] Zooming dependant on OS DPI settings
- [x] Markdown file icons in Solution Explorer
- [x] Drag 'n drop of image
- [x] Paste image from clipboard directly to editor
- [x] Use [Prism](http://prismjs.com/) for code block syntax highlighting
- [x] Write documention in [README.md](README.md)
- [x] Clicking links to local markdown documents in preview should open them
- [x] Outlining for code blocks
- [x] Setting to disable outlining