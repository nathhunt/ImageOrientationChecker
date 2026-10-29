using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VMS.TPS;

namespace ImageOrientationChecker.Tests
{
    [TestClass]
    public class OrientationCheckerTests
    {
        // ---- Reference table ------------------------------------------------

        [TestMethod]
        public void ReferenceTable_HasOneNamePerOrientation()
        {
            Assert.AreEqual(8, OrientationChecker.OrientationValues.Length);
            Assert.AreEqual(OrientationChecker.OrientationValues.Length, OrientationChecker.OrientationNames.Length);
        }

        [TestMethod]
        public void ReferenceTable_EachEntryIsAValidOrientation()
        {
            // Row and column cosines must each be unit vectors and perpendicular to each other.
            foreach (double[] v in OrientationChecker.OrientationValues)
            {
                Assert.AreEqual(6, v.Length);
                Assert.AreEqual(1.0, v[0] * v[0] + v[1] * v[1] + v[2] * v[2], 1e-12, "row not unit length");
                Assert.AreEqual(1.0, v[3] * v[3] + v[4] * v[4] + v[5] * v[5], 1e-12, "column not unit length");
                Assert.AreEqual(0.0, v[0] * v[3] + v[1] * v[4] + v[2] * v[5], 1e-12, "row and column not orthogonal");
            }
        }

        [TestMethod]
        public void ReferenceTable_EntriesAreDistinct()
        {
            // Otherwise FindMatch would always report the first of two identical entries.
            double[][] values = OrientationChecker.OrientationValues;
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = i + 1; j < values.Length; j++)
                {
                    Assert.IsFalse(OrientationChecker.Matches(values[i], values[j]),
                        OrientationChecker.OrientationNames[i] + " duplicates " + OrientationChecker.OrientationNames[j]);
                }
            }
        }

        // ---- FindMatch ------------------------------------------------------

        [TestMethod]
        [DataRow(new double[] {  1,  0, 0,  0,  1, 0 }, "HFS")]
        [DataRow(new double[] { -1,  0, 0,  0, -1, 0 }, "HFP")]
        [DataRow(new double[] {  0, -1, 0,  1,  0, 0 }, "HFDL")]
        [DataRow(new double[] {  0,  1, 0, -1,  0, 0 }, "HFDR")]
        [DataRow(new double[] { -1,  0, 0,  0,  1, 0 }, "FFS")]
        [DataRow(new double[] {  1,  0, 0,  0, -1, 0 }, "FFP")]
        [DataRow(new double[] {  0,  1, 0,  1,  0, 0 }, "FFDL")]
        [DataRow(new double[] {  0, -1, 0, -1,  0, 0 }, "FFDR")]
        public void FindMatch_ExpectedOrientation_ReturnsItsName(double[] tag, string code)
        {
            string match = OrientationChecker.FindMatch(tag);

            Assert.IsNotNull(match);
            Assert.AreEqual(code, match.Split(' ')[0]);
        }

        [TestMethod]
        public void FindMatch_WithinTolerance_Matches()
        {
            double d = OrientationChecker.Tolerance / 2;
            double[] tag = { 1 - d, d, -d, d, 1 - d, d };

            StringAssert.StartsWith(OrientationChecker.FindMatch(tag), "HFS");
        }

        [TestMethod]
        public void FindMatch_JustOutsideTolerance_DoesNotMatch()
        {
            double d = OrientationChecker.Tolerance * 2;
            double[] tag = { 1, 0, 0, 0, 1, d };

            Assert.IsNull(OrientationChecker.FindMatch(tag));
        }

        [TestMethod]
        public void FindMatch_ObliqueImage_ReturnsNull()
        {
            // Rotated 30 degrees about the z axis.
            double c = Math.Cos(Math.PI / 6), s = Math.Sin(Math.PI / 6);
            double[] tag = { c, s, 0, -s, c, 0 };

            Assert.IsNull(OrientationChecker.FindMatch(tag));
        }

        [TestMethod]
        public void FindMatch_SagittalImage_ReturnsNull()
        {
            double[] tag = { 0, 1, 0, 0, 0, -1 };

            Assert.IsNull(OrientationChecker.FindMatch(tag));
        }

        // ---- FormatTag ------------------------------------------------------

        [TestMethod]
        public void FormatTag_WholeNumbers_HasNoDecimals()
        {
            Assert.AreEqual("(1, 0, 0, 0, 1, 0)", OrientationChecker.FormatTag(new double[] { 1, 0, 0, 0, 1, 0 }));
        }

        [TestMethod]
        public void FormatTag_RoundsToFiveDecimals()
        {
            string formatted = OrientationChecker.FormatTag(new double[] { 0.8660254, 0.5, 0, -0.5, 0.8660254, 0 });

            Assert.AreEqual("(0.86603, 0.5, 0, -0.5, 0.86603, 0)", formatted);
        }

        [TestMethod]
        public void FormatTag_CommaDecimalCulture_StillUsesDot()
        {
            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-NL");

                Assert.AreEqual("(0.5, 0, 0, 0, 1, 0)", OrientationChecker.FormatTag(new double[] { 0.5, 0, 0, 0, 1, 0 }));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        // ---- BuildInfo ------------------------------------------------------

        [TestMethod]
        public void BuildInfo_VersionAndCommit_ShowsBoth()
        {
            Assert.AreEqual("Version 1.2.0  (commit 3f9c2ab)", OrientationChecker.BuildInfo("1.2.0", "3f9c2ab"));
        }

        [TestMethod]
        public void BuildInfo_PreReleaseVersion_IsShownAsWritten()
        {
            Assert.AreEqual("Version 2.0.0-beta.1  (commit 3f9c2ab)", OrientationChecker.BuildInfo("2.0.0-beta.1", "3f9c2ab"));
        }

        [TestMethod]
        public void BuildInfo_DirtyCommit_SaysUncommittedChanges()
        {
            Assert.AreEqual("Version 1.2.0  (commit 3f9c2ab + uncommitted changes)",
                OrientationChecker.BuildInfo("1.2.0", "3f9c2ab-dirty"));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void BuildInfo_NoVersionOrCommit_SaysSo(string missing)
        {
            // What a single-file script (compiled by Eclipse, not the .csproj) reports.
            Assert.AreEqual("Version not set  (commit unknown)", OrientationChecker.BuildInfo(missing, missing));
        }

        // ---- BuildMessage ---------------------------------------------------

        [TestMethod]
        public void BuildMessage_MatchingTag_ReportsAllowed()
        {
            string message = OrientationChecker.BuildMessage(
                "CT_1", "STUDY_A", new double[] { 1, 0, 0, 0, 1, 0 }, "Version 1.0.0.0");

            StringAssert.Contains(message, "Image:  CT_1   (Study: STUDY_A)");
            StringAssert.Contains(message, "(1, 0, 0, 0, 1, 0)");
            StringAssert.Contains(message, "Result:  ALLOWED  -  matches HFS  (Head First Supine)");
            Assert.IsTrue(message.EndsWith("Version 1.0.0.0"), message);
        }

        [TestMethod]
        public void BuildMessage_NonMatchingTag_ReportsNotAllowed()
        {
            string message = OrientationChecker.BuildMessage(
                "CT_2", "", new double[] { 0, 1, 0, 0, 0, -1 }, "Version 1.0.0.0");

            StringAssert.Contains(message, "Result:  NOT ALLOWED  -  does not match any expected orientation");
        }
    }
}
