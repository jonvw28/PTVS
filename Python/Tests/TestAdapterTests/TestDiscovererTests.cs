﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PythonTools.TestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace TestAdapterTests {
    [TestClass]
    public class TestDiscovererTests {
        [ClassInitialize]
        public static void DoDeployment(TestContext context) {
            TestData.Deploy();
        }

        [TestMethod]
        public void TestDiscover() {
            var ctx = new MockDiscoveryContext();
            var sink = new MockTestCaseDiscoverySink();
            var logger = new MockMessageLogger();

            new TestDiscoverer().DiscoverTests(
                new[] { TestInfo.TestAdapterLibProjectFilePath, TestInfo.TestAdapterAProjectFilePath, TestInfo.TestAdapterBProjectFilePath },
                ctx,
                logger,
                sink
            );

            PrintTestCases(sink.Tests);

            var expectedTests = TestInfo.TestAdapterATests.Concat(TestInfo.TestAdapterBTests).ToArray();

            Assert.AreEqual(expectedTests.Length, sink.Tests.Count);

            foreach (var expectedTest in expectedTests) {
                var expectedFullyQualifiedName = TestDiscoverer.MakeFullyQualifiedTestName(expectedTest.ClassFilePath, expectedTest.ClassName, expectedTest.MethodName);
                var actualTestCase = sink.Tests.SingleOrDefault(tc => tc.FullyQualifiedName == expectedFullyQualifiedName);
                Assert.IsNotNull(actualTestCase, expectedFullyQualifiedName);
                Assert.AreEqual(expectedTest.MethodName, actualTestCase.DisplayName, expectedFullyQualifiedName);
                Assert.AreEqual(new Uri(TestExecutor.ExecutorUriString), actualTestCase.ExecutorUri);
                Assert.AreEqual(expectedTest.SourceCodeLineNumber, actualTestCase.LineNumber, expectedFullyQualifiedName);
                Assert.IsTrue(IsSameFile(expectedTest.SourceCodeFilePath, actualTestCase.CodeFilePath), expectedFullyQualifiedName);
                Assert.IsTrue(IsSameFile(expectedTest.ProjectFilePath, actualTestCase.Source), expectedFullyQualifiedName);
                Assert.IsTrue(IsSameFile(expectedTest.ClassFilePath, actualTestCase.Traits.Single(t => t.Name == "File").Value), expectedFullyQualifiedName);

                sink.Tests.Remove(actualTestCase);
            }

            Debug.WriteLine("");
            Debug.WriteLine("");
            Debug.WriteLine("");
            Debug.WriteLine("");

            PrintTestCases(sink.Tests);
        }

        private static bool IsSameFile(string a, string b) {
            return String.Compare(new FileInfo(a).FullName, new FileInfo(b).FullName, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        private static void PrintTestCases(IEnumerable<TestCase> testCases) {
            foreach (var tst in testCases) {
                Debug.WriteLine("Test: " + tst.FullyQualifiedName);
                Debug.WriteLine("Source: " + tst.Source);
                Debug.WriteLine("Class File: " + tst.Traits.Single(t => t.Name == "File").Value);
                Debug.WriteLine("Display: " + tst.DisplayName);
                Debug.WriteLine("Location: " + tst.CodeFilePath);
                Debug.WriteLine("Location: " + tst.LineNumber.ToString());
                Debug.WriteLine("");
            }
        }

        internal static TestCase CreateExpectedTestCase(string className, string methodName, string relativeSourcePath, string relativeCodeFilePath, int codeLineNumber, string relativeClassFilePath = null) {
            if (String.IsNullOrEmpty(relativeClassFilePath)) {
                relativeClassFilePath = relativeCodeFilePath;
            }
            var fullyQualifiedName = TestDiscoverer.MakeFullyQualifiedTestName(TestData.GetPath(relativeClassFilePath), className, methodName);
            var test = new TestCase(fullyQualifiedName, new Uri(TestExecutor.ExecutorUriString), TestData.GetPath(relativeSourcePath));
            test.CodeFilePath = TestData.GetPath(relativeCodeFilePath);
            test.LineNumber = codeLineNumber;
            test.DisplayName = methodName;
            test.Traits.Add("File", TestData.GetPath(relativeClassFilePath));
            return test;
        }
    }
}