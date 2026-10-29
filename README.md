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
- To run the unit tests: the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  or later. The tests don't need Eclipse or the ESAPI assemblies.

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
  **Build (Release)** and **Clean**, which run MSBuild on the solution, and **Test**
  (the default test task), which runs `dotnet test`. Compiler errors appear in the
  Problems panel.
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

## Testing

Unit tests are in `tests/ImageOrientationChecker.Tests` and use MSTest. Run them with:

```bash
dotnet test tests/ImageOrientationChecker.Tests
```

You can also use the **Test** task in VS Code, or Test Explorer in Visual Studio.

The tests run on both `net48` (the framework Eclipse uses) and `net8.0`. `net48` runs
only on Windows. On Linux or macOS, add `-f net8.0`.

ESAPI objects such as `Image` can't be created outside Eclipse, so the script is split
into two classes in the same file:

- `Script` is the Eclipse entry point. It reads the image's direction cosines and shows
  the message box.
- `OrientationChecker` holds the logic: the reference table, matching, formatting, the
  message text and the build info. It doesn't use ESAPI.

The test project compiles `ImageOrientationChecker.cs` directly, with the `UNIT_TEST`
symbol defined. That leaves out `Script` and the ESAPI `using`s (they are inside
`#if !UNIT_TEST`), so the tests build without the Varian assemblies. The file is still a
single-file script that Eclipse can run directly.

The tests cover:

- the reference table: 8 entries, all different, and each one a valid pair of
  perpendicular unit vectors
- matching for all 8 orientations, within and just outside the tolerance, and
  oblique and sagittal images
- tag formatting, including on a system that uses a comma as the decimal separator
- decoding the build date from the version
- the ALLOWED and NOT ALLOWED message text

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
- MSTest documentation: <https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-intro>
