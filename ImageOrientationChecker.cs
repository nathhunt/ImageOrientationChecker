// ImageOrientationChecker.cs
//
// Single-file ESAPI script.
// Reports the currently open image's DICOM Image Orientation (Patient)
// tag value (0020,0037), and checks whether it matches one of the
// orientations Varian expects (HFS, HFP, HFDL, HFDR, FFS, FFP, FFDL, FFDR).
//
// Usage: Open a patient with an image loaded (e.g. in the Image module),
// then run this script from Eclipse: Tools > Scripts.

using System;
using System.Reflection;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

// Build number and revision are filled in by the compiler on each build
// (days since 2000-01-01 and seconds since midnight / 2), so the message box
// can show exactly which build is running.
[assembly: AssemblyVersion("1.0.*")]

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        // Reference direction cosine sets Varian expects for tag (0020,0037).
        // Each row is: Xx, Xy, Xz, Yx, Yy, Yz  (row cosines, then column cosines).
        private static readonly string[] OrientationNames = new string[]
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

        private static readonly double[][] OrientationValues = new double[][]
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

        private const double Tolerance = 0.000001;

        private const string Title = "Image Orientation Check";

        public void Execute(ScriptContext context)
        {
            Image image = context.Image;

            if (image == null)
            {
                MessageBox.Show("No image is currently open.\r\n\r\n" + BuildInfo(), Title);
                return;
            }

            VVector xDir = image.XDirection; // row direction cosines
            VVector yDir = image.YDirection; // column direction cosines

            double[] tag = new double[]
            {
                xDir.x, xDir.y, xDir.z,
                yDir.x, yDir.y, yDir.z
            };

            string tagString = FormatTag(tag);

            string matchName = null;
            for (int i = 0; i < OrientationValues.Length; i++)
            {
                if (Matches(tag, OrientationValues[i]))
                {
                    matchName = OrientationNames[i];
                    break;
                }
            }

            string studyId = (image.Series != null && image.Series.Study != null)
                ? image.Series.Study.Id
                : "";

            string resultLine = (matchName != null)
                ? "ALLOWED  -  matches " + matchName
                : "NOT ALLOWED  -  does not match any expected orientation";

            string message = string.Format(
                "Image:  {0}   (Study: {1})\r\n" +
                "--------------------------------------------\r\n" +
                "Tag (0020,0037) Image Orientation (Patient):\r\n" +
                "    {2}\r\n" +
                "--------------------------------------------\r\n" +
                "Result:  {3}\r\n" +
                "--------------------------------------------\r\n" +
                "{4}",
                image.Id, studyId, tagString, resultLine, BuildInfo());

            MessageBox.Show(message, Title);
        }

        private static bool Matches(double[] tag, double[] reference)
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

        private static string BuildInfo()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            // Reverse the "1.0.*" auto-versioning scheme to recover the build time (local time).
            DateTime built = new DateTime(2000, 1, 1)
                .AddDays(version.Build)
                .AddSeconds(version.Revision * 2);

            return string.Format("Version {0}  (built {1:yyyy-MM-dd HH:mm})", version, built);
        }

        private static string FormatTag(double[] values)
        {
            string[] parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                parts[i] = values[i].ToString("0.#####");
            }
            return "(" + string.Join(", ", parts) + ")";
        }
    }
}
