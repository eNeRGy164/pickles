﻿#region License

/*
    Copyright [2011] [Jeffrey Cameron]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using PicklesDoc.Pickles.ObjectModel;
using PicklesDoc.Pickles.Parser;

namespace PicklesDoc.Pickles.TestFrameworks
{
    public class XUnitSingleResults : ITestResults
    {
        internal xUnitExampleSignatureBuilder ExampleSignatureBuilder { get; set; }

        private readonly XDocument resultsDocument;

        public XUnitSingleResults(XDocument resultsDocument)
        {
          this.resultsDocument = resultsDocument;
        }

      #region ITestResults Members

        public TestResult GetFeatureResult(Feature feature)
        {
            XElement featureElement = this.GetFeatureElement(feature);
            int passedCount = int.Parse(featureElement.Attribute("passed").Value);
            int failedCount = int.Parse(featureElement.Attribute("failed").Value);
            int skippedCount = int.Parse(featureElement.Attribute("skipped").Value);

            return GetAggregateResult(passedCount, failedCount, skippedCount);
        }

        public TestResult GetScenarioOutlineResult(ScenarioOutline scenarioOutline)
        {
            IEnumerable<XElement> exampleElements = this.GetScenarioOutlineElements(scenarioOutline);
            int passedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;

            foreach (XElement exampleElement in exampleElements)
            {
                TestResult result = this.GetResultFromElement(exampleElement);
                if (result.WasExecuted == false) skippedCount++;
                if (result.WasExecuted && result.WasSuccessful) passedCount++;
                if (result.WasExecuted && !result.WasSuccessful) failedCount++;
            }

            return GetAggregateResult(passedCount, failedCount, skippedCount);
        }

        public TestResult GetScenarioResult(Scenario scenario)
        {
            XElement scenarioElement = this.GetScenarioElement(scenario);
            return scenarioElement != null 
                ? this.GetResultFromElement(scenarioElement)
                : TestResult.Inconclusive;
        }

        #endregion

      private XElement GetFeatureElement(Feature feature)
        {
            IEnumerable<XElement> featureQuery =
                from clazz in this.resultsDocument.Root.Descendants("class")
                from test in clazz.Descendants("test")
                from trait in clazz.Descendants("traits").Descendants("trait")
                where trait.Attribute("name").Value == "FeatureTitle" && trait.Attribute("value").Value == feature.Name
                select clazz;

            return featureQuery.FirstOrDefault();
        }

        private XElement GetScenarioElement(Scenario scenario)
        {
            XElement featureElement = this.GetFeatureElement(scenario.Feature);

            IEnumerable<XElement> scenarioQuery =
                from test in featureElement.Descendants("test")
                from trait in test.Descendants("traits").Descendants("trait")
                where trait.Attribute("name").Value == "Description" && trait.Attribute("value").Value == scenario.Name
                select test;

            return scenarioQuery.FirstOrDefault();
        }

        private IEnumerable<XElement> GetScenarioOutlineElements(ScenarioOutline scenario)
        {
            XElement featureElement = this.GetFeatureElement(scenario.Feature);

            IEnumerable<XElement> scenarioQuery =
                from test in featureElement.Descendants("test")
                from trait in test.Descendants("traits").Descendants("trait")
                where trait.Attribute("name").Value == "Description" && trait.Attribute("value").Value == scenario.Name
                select test;

            return scenarioQuery;
        }

        private TestResult GetResultFromElement(XElement element)
        {
            TestResult result;
            XAttribute resultAttribute = element.Attribute("result");
            switch (resultAttribute.Value.ToLowerInvariant())
            {
                case "pass":
                    result = TestResult.Passed;
                    break;
                case "fail":
                    result = TestResult.Failed;
                    break;
                case "skip":
                default:
                    result = TestResult.Inconclusive;
                    break;
            }
            return result;
        }

        private static TestResult GetAggregateResult(int passedCount, int failedCount, int skippedCount)
        {
            TestResult result;
            if (passedCount > 0 && failedCount == 0)
            {
                result = TestResult.Passed;
            }
            else if (failedCount > 0)
            {
                result = TestResult.Failed;
            }
            else
            {
                result = TestResult.Inconclusive;
            }

            return result;
        }

        public TestResult GetExampleResult(ScenarioOutline scenarioOutline, string[] exampleValues)
        {
            IEnumerable<XElement> exampleElements = this.GetScenarioOutlineElements(scenarioOutline);

            var result = new TestResult();
            var signatureBuilder = this.ExampleSignatureBuilder;

            if (signatureBuilder == null)
            {
              throw new InvalidOperationException("You need to set the ExampleSignatureBuilder before using GetExampleResult.");
            }

            foreach (XElement exampleElement in exampleElements)
            {
              Regex signature = signatureBuilder.Build(scenarioOutline, exampleValues);
                if (signature.IsMatch(exampleElement.Attribute("name").Value.ToLowerInvariant()))
                {
                    return this.GetResultFromElement(exampleElement);
                }
            }
            return result;
        }

        public bool SupportsExampleResults
        {
            get
            {
                return true;
            }
        }
    }
}