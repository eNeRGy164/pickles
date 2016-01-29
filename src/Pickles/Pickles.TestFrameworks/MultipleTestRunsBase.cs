using System;
using System.Linq;

using PicklesDoc.Pickles.ObjectModel;

namespace PicklesDoc.Pickles.TestFrameworks
{
    public abstract class MultipleTestRunsBase : MultipleTestResults
    {
        protected MultipleTestRunsBase(bool supportsExampleResults, IConfiguration configuration, ISingleResultLoader singleResultLoader, IExampleSignatureBuilder exampleSignatureBuilder)
            : base(supportsExampleResults, configuration, singleResultLoader)
        {
            if (exampleSignatureBuilder == null)
            {
                throw new ArgumentNullException(nameof(exampleSignatureBuilder));
            }

            this.SetExampleSignatureBuilder(exampleSignatureBuilder);
        }

        public override TestResult GetExampleResult(ScenarioOutline scenarioOutline, string[] arguments)
        {
            var results = TestResults.Select(tr => tr.GetExampleResult(scenarioOutline, arguments)).ToArray();

            return EvaluateTestResults(results);
        }

        private void SetExampleSignatureBuilder(IExampleSignatureBuilder exampleSignatureBuilder)
        {
            foreach (var testResult in this.TestResults)
            {
                testResult.ExampleSignatureBuilder = exampleSignatureBuilder;
            }
        }
    }
}