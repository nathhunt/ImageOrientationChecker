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
- the script version and the git commit it was built from, so you can tell exactly which
  build is running

## Requirements

- Varian Eclipse with ESAPI **18.1**. The project looks for the ESAPI assemblies in
  `C:\Program Files (x86)\Varian\RTM\18.1\esapi\API`. For another version, change the
  `EsapiPath` property in `ImageOrientationChecker.csproj`, or override it when you build:
  `dotnet build -p:EsapiPath="C:\...\esapi\API"`.
- The script targets .NET Framework 4.8 (x64).
- To build and test: the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or
  later (Visual Studio 2022 includes it). The tests don't need Eclipse or the ESAPI assemblies.

Both projects use the SDK-style project format, so they load in Visual Studio 2022 and in
VS Code's C# Dev Kit, which doesn't support the older .NET Framework project format.

## Usage

1. Open a patient in Eclipse and load an image (for example, in the Image module).
2. Go to **Tools > Scripts**. If the script isn't listed, click **Change Folder...** and
   choose the folder that contains `ImageOrientationChecker.esapi.dll`, for example
   `bin\Release\` or your clinic's published scripts folder.
3. Select `ImageOrientationChecker.esapi.dll` and click **Run**.
4. Read the result in the message box.

You can also run `ImageOrientationChecker.cs` directly as a single-file script. Eclipse
then compiles the file without the project, so the message box shows
`Version not set  (commit unknown)`. Use the DLL when you need to know which version ran.

The script only reads data. It does not change anything in the database.

## Building

### Visual Studio

Open `ImageOrientationChecker.sln`, pick **Debug|x64** or **Release|x64**, and build.

### Command line

```bash
dotnet build ImageOrientationChecker.sln -c Release -p:Platform=x64
```

### VS Code

The repository includes a `.vscode` folder with:

- `tasks.json`: **Build (Debug)** (the default build task, <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd>),
  **Build (Release)**, **Clean** and **Test** (the default test task). Each one runs the
  `dotnet` CLI, and compiler errors appear in the Problems panel.
- `settings.json`: editor settings for the project, with `bin/` and `obj/` hidden.
- `extensions.json`: recommends C# Dev Kit and the C# extension.

Output goes to `bin\Debug\ImageOrientationChecker.esapi.dll` or
`bin\Release\ImageOrientationChecker.esapi.dll`.

> **Note:** Eclipse lists a compiled script only if its file name ends in `.esapi.dll`,
> which is why the project's `AssemblyName` is `ImageOrientationChecker.esapi`. Depending
> on your clinic's configuration, scripts may also need to be approved before they can run.

## Testing

Unit tests are in `tests/ImageOrientationChecker.Tests` and use MSTest. Run them with:

```bash
dotnet test tests/ImageOrientationChecker.Tests
```

You can also use the **Test** task or the Testing view (C# Dev Kit) in VS Code, or Test
Explorer in Visual Studio.

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

You set the version yourself in `ImageOrientationChecker.csproj`:

```xml
<Version>1.0.0</Version>
```

Each build also records the current git commit, and the message box shows both:

```
Version 1.0.0  (commit 3f9c2ab)
```

- If tracked files have uncommitted changes when you build, the line reads
  `(commit 3f9c2ab + uncommitted changes)`. Commit before you build a release.
- If git isn't installed, or the folder isn't a git repository, the build still succeeds
  and the commit shows as `unknown`.

The `AddGitCommitMetadata` target in the `.csproj` runs `git rev-parse --short HEAD` and
stores the result as `[assembly: AssemblyMetadata("GitCommit", "...")]`. The script reads
that attribute and `AssemblyInformationalVersion` (which comes from `<Version>`) when it runs.

## References

- DICOM PS3.3, C.7.6.2.1.1, *Image Position and Image Orientation*:
  <https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.7.6.2.html#sect_C.7.6.2.1.1>
- DICOM PS3.3, C.7.3.1.1.2, *Patient Position*, which defines HFS, HFP, HFDL and the other codes:
  <https://dicom.nema.org/medical/dicom/current/output/chtml/part03/sect_C.7.3.html#sect_C.7.3.1.1.2>
- MSBuild properties for assembly version information (`Version`, `InformationalVersion`,
  `AssemblyMetadata`): <https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props#assembly-attribute-properties>
- Varian ESAPI documentation and examples: <https://github.com/VarianAPIs>
- C# Dev Kit project format requirements: <https://code.visualstudio.com/docs/csharp/cs-dev-kit-faq>
- MSTest documentation: <https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-intro>
