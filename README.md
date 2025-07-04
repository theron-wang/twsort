# twsort

A simple and efficient CLI tool to sort Tailwind CSS classes in your CSS, HTML, Razor, or other markup files, ensuring a consistent and readable class order.

## Overview

Inspired by Prettier, this tool sorts Tailwind classes in a logical order, making it easier to maintain and read your code.

This tool is a standalone CLI which does not require any additional dependencies or configurations, which can be useful if you do not want a `package.json` file solely for sorting Tailwind classes.

This sorter works best with Tailwind CSS v3 and above. If you are using Tailwind CSS v3, you need to have `node` installed globally for configuration files to be parsed correctly. On projects using v4+ with the new css configuration format, `node` does not need to be installed.

## Features

- Sorts Tailwind classes in a logical and consistent order.
- Can process individual files or entire directories.

## Installation

Download the latest release from [GitHub Releases](https://github.com/theron-wang/twsort/releases).

## Quick Start

### Sort a single file:

```sh
twsort input.html
```

### Sort all files recursively in a directory:

```sh
twsort ./src
```

### Sort all files in a directory, not recursively:

```sh
twsort ./src --shallow
```

### Sort all files recursively in a directory, with a specified version:

```sh
twsort ./src --tailwind-version 4.0.1
```

### Sort all files in a directory with custom file extensions:

```sh
twsort ./src --extensions html,razor,cshtml,tcss
```

### Specify custom Tailwind CSS configuration files:

```sh
twsort ./src -r --config ./tailwind.css ./tailwind2.css
```

## Reference

```
Description:
  Tailwind Class Sorter - Sorts Tailwind CSS classes for consistency and cleanliness

Usage:
  TailwindClassSorter <input> [options]

Arguments:
  <input>  File or directory to process

Options:
  -?, -h, --help      Show help and usage information
  --version           Show version information
  --shallow           Sort only the classes in the specified directory without recursing into subdirectories
  --extensions        Comma-separated file extensions to process [default: css,html,aspx,ascx,jsx,tsx,razor,cshtml]
  --tailwind-version  Specify Tailwind major version (e.g. 3, 4, 4.1). Use if auto-detection fails.
  --config            Paths to all Tailwind CSS configuration files. In single file sort mode, this option must be
                      provided. In directory sort mode, this is optional; if not provided, the tool will attempt to
                      find configuration files automatically.
  --verbose           Show full logs
```

## Contributing

Feel free to create an issue or submit a pull request if you have any suggestions or improvements.

## Additional Info

This is the CLI version of the sorter included in the [Tailwind CSS Editor Support for Visual Studio 2022](https://github.com/theron-wang/VS2022-Editor-Support-for-Tailwind-CSS) extension. If you're using Visual Studio 2022, you might find the extension useful.