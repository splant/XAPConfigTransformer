using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace XAPConfigTransformer
{
    public class MsBuildConfigTransform
    {
        //test comment - additional item
        public void ExecuteTransformation(string targetEnvironment, string configFilesLocation)
        {
            using (var projectCollection = new ProjectCollection())
            {
                var globalProperties = new Dictionary<string, string>
                {
                    { "Configuration", targetEnvironment },
                    { "ConfigFileName", "ServiceReferences" },
                    { "TransformingConfigFileBasePath", configFilesLocation },
                };

                string msBuildTransformSchema = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "PerformSilverlightConfigTransform.Proj");

                BuildRequestData buildRequestData =
                    new BuildRequestData(msBuildTransformSchema, globalProperties, null, new[] { "Transform" }, null);

                BuildResult buildResult = BuildManager.DefaultBuildManager.Build(
                    new BuildParameters(projectCollection), buildRequestData);

                HandleMsBuild4Bug();
            }
        }

        private static void HandleMsBuild4Bug()
        {
            //The BuildManager class in .Net 4 does not implement IDisposable which causes it to leave 
            //open connections to the files it uses. Forcing a garbage collection, whilst a messy solution,
            //causes the file handles to be closed and disposed of correctly. In .Net 4.5 this class does implement
            //IDisposable and thus this hack will not be required any more!
            GC.Collect();
        }
    }
}
