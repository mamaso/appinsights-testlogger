using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

namespace appinsights.testlogger
{
    [FriendlyName("appinsights")]
    [ExtensionUri("logger://appinsights")]
    public class InsightsLogger : ITestLogger
    {
        public void Initialize(TestLoggerEvents events, string testRunDirectory)
        {
            var key = Environment.GetEnvironmentVariable("APPINSIGHTS_REPORTER_KEY");
            if (!string.IsNullOrEmpty(key))
            {
                TelemetryClient client = new TelemetryClient()
                {
                    InstrumentationKey = key
                };

                List<string> props = new List<string>()
                {
                    "APPVEYOR",
                    "APPVEYOR_PROJECT_NAME",
                    "APPVEYOR_BUILD_NUMBER",
                    "APPVEYOR_BUILD_VERSION",
                    "APPVEYOR_BUILD_WORKER_IMAGE",
                    "APPVEYOR_PULL_REQUEST_NUMBER",
                    "APPVEYOR_PULL_REQUEST_TITLE",
                    "APPVEYOR_REPO_NAME",
                    "APPVEYOR_REPO_BRANCH",
                    "APPVEYOR_REPO_COMMIT",
                    "APPVEYOR_REPO_COMMIT_AUTHOR"
                };

                foreach (var prop in props)
                {
                    client.Context.Properties.Add(prop, Environment.GetEnvironmentVariable(prop));
                }

                events.TestResult += (sender, args) =>
                {
                    var res = args.Result;
                    var properties = res.Properties.ToDictionary(prop => prop.Label, prop => res.GetPropertyValue(prop)?.ToString());
                    var metrics = new Dictionary<string, double>()
                    {
                        ["StartTimeMs"] = res.StartTime.ToUnixTimeMilliseconds(),
                        ["EndTimeMs"] = res.EndTime.ToUnixTimeMilliseconds(),
                        ["DurationMs"] = res.Duration.TotalMilliseconds
                    };
                    client.TrackEvent("TestResult", properties, metrics);
                };

                events.TestRunComplete += (sender, args) =>
                {
                    client.Flush();

                    // Allow some time for flushing before shutdown
                    Thread.Sleep(1000);
                };
            }
        }
    }
}