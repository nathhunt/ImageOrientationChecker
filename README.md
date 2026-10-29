# Image Orientation Checker

An Eclipse (Varian ESAPI) script that reads the **Image Orientation (Patient)** DICOM tag
`(0020,0037)` of the currently open image and checks whether it matches one of the
patient orientations Varian expects.

## What it does

The script takes the image's row and column direction cosines (`Image.XDirection` and
`Image.YDirection` in ESAPI). These six values make up tag `(0020,0037)`. It compares
them, to a tolerance of `1e-6`, against these reference orientations:

| Orientation | Description                | Row cosines (X) | Column cosines (Y) |
|-------------|----------------------------|-----------------|--------------------|
| HFS         | Head First Supine          | ( 1,  0, 0)     | ( 0,  1, 0)        |
| HFP         | Head First Prone           | (-1,  0, 0)     | ( 0, -1, 0)        |
| HFDL        | Head First Decubitus Left  | ( 0, -1, 0)     | ( 1,  0, 0)        |
| HFDR        | Head First Decubitus Right | ( 0,  1, 0)     | (-1,  0, 0)        |
| FFS         | Feet First Supine          | (-1,  0, 0)     | ( 0,  1, 0)        |
| FFP         | Feet First Prone           | ( 1,  0, 0)     | ( 0, -1, 0)        |
| FFDL        | Feet First Decubitus Left  | ( 0,  1, 0)     | ( 1,  0, 0)        |
| FFDR        | Feet First Decubitus Right | ( 0, -1, 0)     | (-1,  0, 0)        |

A message box then shows:

- the image ID and study ID
- the raw tag `(0020,0037)` value
- **ALLOWED** (with the matching orientation) or **NOT ALLOWED**
- the script version and build time, so you can tell which build is running

## Requirements

- Varian Eclipse with ESAPI **18.1**. The project references the ESAPI assemblies in
  `C:\Program Files (x86)\Varian\RTM\18.1\esapi\API\`. If your version is different, update
  the `HintPath` entries in `ImageOrientationChecker.csproj`.
- .NET Framework 4.8 and an x64 build.
- To build: Visual Studio 2022, or Visual Studio Build Tools 2022 (MSBuild).

## Usage

1. Open a patient in Eclipse and load an image (for example, in the Image module).
2. Go to **Tools > Scripts** and run `ImageOrientationChecker.cs`. You can run the
   single-file script directly, or run the compiled DLL (see below).
3. Read the result in the message box.

The script only reads data. It does not change anything in the database.

## Building

### Visual Studio

Open `ImageOrientationChecker.sln`, pick **Debug|x64** or **Release|x64**, and build.

### VS Code

The repository includes a `.vscode` folder with:

- `tasks.json`: **Build (Debug)** (the default build task, <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd>),
  **Build (Release)** and **Clean**. Each one runs MSBuild on the solution, and
  compiler errors appear in the Problems panel.
- `settings.json`: editor settings for the project, with `bin/` and `obj/` hidden.
- `extensions.json`: recommends the C# extension.

MSBuild must be on your `PATH`. The easiest way is to start VS Code from a
**Developer PowerShell for VS 2022** window:

```powershell
cd path\to\ImageOrientationChecker
code .
```

Output goes to `bin\debug\` or `bin\release\`.

> **Note:** Eclipse loads a compiled script only if its file name ends in `.esapi.dll`.
> If you want to run the DLL instead of the `.cs` file, change `AssemblyName` to
> `ImageOrientationChecker.esapi` in the `.csproj`. Depending on your clinic's
> configuration, scripts may also need to be approved before they can run.

## Versioning

The version is set in `ImageOrientationChecker.cs`:

```csharp
[assembly: AssemblyVersion("1.0.*")]
```

The `*` tells the compiler to fill in the build number (days since 1 January 2000) and
the revision (seconds since midnight, divided by 2) when it builds. The script reverses
this to show the build date and time in the message box, for example:

```
Version 1.0.9798.31650  (built 2026-10-29 17:35)
```

To release a new version, change the major or minor number (for example, `"1.1.*"`).
Wildcard versions don't work with deterministic builds, so the `.csproj` sets
`<Deterministic>false</Deterministic>`.

## References

- DICOM PS3.3, C.7.6.2.1.1, *Image Position and Image Orientation*:
  <https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.7.6.2.html#sect_C.7.6.2.1.1>
- DICOM PS3.3, C.7.3.1.1.2, *Patient Position*, which defines HFS, HFP, HFDL and the other codes:
  <https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.7.3.html#sect_C.7.3.1.1.2>
- `AssemblyVersionAttribute`, which describes how wildcard build and revision numbers are generated:
  <https://learn.microsoft.com/dotnet/api/system.reflection.assemblyversionattribute>
- Varian ESAPI documentation and examples: <https://github.com/VarianAPIs>
