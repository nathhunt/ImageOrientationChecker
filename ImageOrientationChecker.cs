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
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

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

        public void Execute(ScriptContext context)
        {
            Image image = context.Image;

            if (image == null)
            {
                MessageBox.Show("No image is currently open.", "Image Orientation Check");
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
                "Result:  {3}",
                image.Id, studyId, tagString, resultLine);

            MessageBox.Show(message, "Image Orientation Check");
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
