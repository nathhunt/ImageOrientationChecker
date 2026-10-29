// ImageOrientationChecker.cs
//
// Single-file ESAPI script.
// Reports the currently open image's DICOM Image Orientation (Patient)
// tag value (0020,0037), and checks whether it matches one of the
// orientations Varian expects (HFS, HFP, HFDL, HFDR, FFS, FFP, FFDL, FFDR).
//
// Usage: Open a patient with an image loaded (e.g. in the Image module),
// then run this script from Eclipse: Tools > Scripts.
//
// The unit test project compiles this same file with UNIT_TEST defined,
// which leaves out everything that depends on ESAPI so the tests can build
// and run without the Varian assemblies.

using System;
using System.Globalization;
#if !UNIT_TEST
using System.Reflection;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
#endif

namespace VMS.TPS
{
#if !UNIT_TEST
    public class Script
    {
        public Script()
        {
        }

        private const string Title = "Image Orientation Check";

        public void Execute(ScriptContext context)
        {
            Image image = context.Image;

            string buildInfo = ReadBuildInfo();

            if (image == null)
            {
                MessageBox.Show("No image is currently open.\r\n\r\n" + buildInfo, Title);
                return;
            }

            VVector xDir = image.XDirection; // row direction cosines
            VVector yDir = image.YDirection; // column direction cosines

            double[] tag = new double[]
            {
                xDir.x, xDir.y, xDir.z,
                yDir.x, yDir.y, yDir.z
            };

            string studyId = (image.Series != null && image.Series.Study != null)
                ? image.Series.Study.Id
                : "";

            MessageBox.Show(OrientationChecker.BuildMessage(image.Id, studyId, tag, buildInfo), Title);
        }

        // The version and commit are written into the assembly by ImageOrientationChecker.csproj
        // (<Version> and the AddGitCommitMetadata target). When Eclipse compiles this file as a
        // single-file script they are absent, and BuildInfo reports that instead.
        private static string ReadBuildInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string version = null;
            object[] versions = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            if (versions.Length > 0)
            {
                version = ((AssemblyInformationalVersionAttribute)versions[0]).InformationalVersion;
            }

            string commit = null;
            foreach (AssemblyMetadataAttribute metadata in assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false))
            {
                if (metadata.Key == "GitCommit")
                {
                    commit = metadata.Value;
                }
            }

            return OrientationChecker.BuildInfo(version, commit);
        }
    }
#endif

    // ESAPI-free logic, kept separate from Script so it can be unit tested.
    internal static class OrientationChecker
    {
        // Reference direction cosine sets Varian expects for tag (0020,0037).
        // Each row is: Xx, Xy, Xz, Yx, Yy, Yz  (row cosines, then column cosines).
        internal static readonly string[] OrientationNames = new string[]
        {
            "HFS  (Head First Supine)",
            "HFP  (Head First Prone)",
            "HFDL (Head First Decubitus Left)",
            "HFDR (Head First Decubitus Right)",
            "FFS  (Feet First Supine)",
            "FFP  (Feet First Prone)",
            "FFDL (Feet First Decubitus Left)",
            "FFDR (Feet First Decubitus Right)"
        };

        internal static readonly double[][] OrientationValues = new double[][]
        {
            new double[] {  1,  0, 0,  0,  1, 0 }, // HFS
            new double[] { -1,  0, 0,  0, -1, 0 }, // HFP
            new double[] {  0, -1, 0,  1,  0, 0 }, // HFDL
            new double[] {  0,  1, 0, -1,  0, 0 }, // HFDR
            new double[] { -1,  0, 0,  0,  1, 0 }, // FFS
            new double[] {  1,  0, 0,  0, -1, 0 }, // FFP
            new double[] {  0,  1, 0,  1,  0, 0 }, // FFDL
            new double[] {  0, -1, 0, -1,  0, 0 }, // FFDR
        };

        internal const double Tolerance = 0.000001;

        // Returns the name of the matching orientation, or null if none match.
        internal static string FindMatch(double[] tag)
        {
            for (int i = 0; i < OrientationValues.Length; i++)
            {
                if (Matches(tag, OrientationValues[i]))
                {
                    return OrientationNames[i];
                }
            }
            return null;
        }

        internal static string BuildMessage(string imageId, string studyId, double[] tag, string buildInfo)
        {
            string matchName = FindMatch(tag);

            string resultLine = (matchName != null)
                ? "ALLOWED  -  matches " + matchName
                : "NOT ALLOWED  -  does not match any expected orientation";

            return string.Format(
                "Image:  {0}   (Study: {1})\r\n" +
                "--------------------------------------------\r\n" +
                "Tag (0020,0037) Image Orientation (Patient):\r\n" +
                "    {2}\r\n" +
                "--------------------------------------------\r\n" +
                "Result:  {3}\r\n" +
                "--------------------------------------------\r\n" +
                "{4}",
                imageId, studyId, FormatTag(tag), resultLine, buildInfo);
        }

        internal static bool Matches(double[] tag, double[] reference)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Math.Abs(tag[i] - reference[i]) > Tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        internal static string FormatTag(double[] values)
        {
            string[] parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                // Invariant culture so the decimal separator is always "." and never
                // collides with the ", " used between values (e.g. on nl-NL systems).
                parts[i] = values[i].ToString("0.#####", CultureInfo.InvariantCulture);
            }
            return "(" + string.Join(", ", parts) + ")";
        }

        // Formats the version line, e.g. "Version 1.2.0  (commit 3f9c2ab)".
        // A commit ending in "-dirty" was built with uncommitted changes.
        internal static string BuildInfo(string version, string commit)
        {
            if (string.IsNullOrEmpty(version))
            {
                version = "not set";
            }

            if (string.IsNullOrEmpty(commit))
            {
                commit = "unknown";
            }
            else if (commit.EndsWith("-dirty", StringComparison.Ordinal))
            {
                commit = commit.Substring(0, commit.Length - "-dirty".Length) + " + uncommitted changes";
            }

            return "Version " + version + "  (commit " + commit + ")";
        }
    }
}
