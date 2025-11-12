This repository contains libraries for Python, Julia, and C#, as well as example Jupyter notebooks using these libraries to control pulsed NMR applications for Red Pitaya STEMlab 125-14 and Red Pitaya SDRlab 122-16:

https://pavel-demin.github.io/red-pitaya-notes/pulsed-nmr/

https://pavel-demin.github.io/red-pitaya-notes/pulsed-nmr-122-88/

### Installing required software

- install Visual Studio Code following the platform-specific instructions below:
  - [Linux](https://code.visualstudio.com/docs/setup/linux)
  - [macOS](https://code.visualstudio.com/docs/setup/mac)
  - [Windows](https://code.visualstudio.com/docs/setup/windows)

- download this repository by cloning it using git or by downloading and unzipping [pulsed-nmr-notebooks-main.zip](https://github.com/pavel-demin/pulsed-nmr-notebooks/archive/refs/heads/main.zip)

- open `pulsed-nmr-notebooks` folder in Visual Studio Code
  - from the "File" menu select "Open Folder"
  - in the "Open Folder" dialog find and select `pulsed-nmr-notebooks` folder and click "OK"

### Working with Python

- install the following Visual Studio Code extensions:
  - [Python](https://marketplace.visualstudio.com/items?itemName=ms-python.python)
  - [Jupyter](https://marketplace.visualstudio.com/items?itemName=ms-toolsai.jupyter)
  - [Micromamba](https://marketplace.visualstudio.com/items?itemName=corker.vscode-micromamba)

- create micromamba environment
  - from the "View" menu select "Command Palette"
  - type "micromamba create environment"

- open `python/PulsedNMR.ipynb` in Visual Studio Code

- make sure the micromamba environment called "default" is selected in the kernel/environment selector in the top right corner of the notebook view

- run the code cells one by one by clicking the play icon in the top left corner of each cell

### Working with Julia

- install the following Visual Studio Code extensions:
  - [Julia](https://marketplace.visualstudio.com/items?itemName=julialang.language-julia)
  - [Jupyter](https://marketplace.visualstudio.com/items?itemName=ms-toolsai.jupyter)

- open `julia/PulsedNMR.ipynb` in Visual Studio Code

- run the code cells one by one by clicking the play icon in the top left corner of each cell

### Working with C#

- install [Polyglot Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) Visual Studio Code extension

- install [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet)

- open `csharp/PulsedNMR.ipynb` in Visual Studio Code

- run the code cells one by one by clicking the play icon in the top left corner of each cell
