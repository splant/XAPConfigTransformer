using System;
using System.IO;
using Ionic.Zip;

namespace XAPConfigTransformer
{
    class Program
    {
        private static readonly string _platformTempFilePath;

        static Program()
        {
            _platformTempFilePath = Path.GetTempPath();
        }

        private const string SILVERLIGHT_CONFIG_FILE = "ServiceReferences.ClientConfig";
        private const string SILVERLIGHT_CONFIG_FILE_PATTERN = "ServiceReferences.{0}.ClientConfig";

        static void Main(string[] args)
        {
            ProgramParameters inputParameters = ProgramParameters.Create(args);
            if (!inputParameters.ValidParameters)
            {
                Console.WriteLine(
                    "USAGE: XAPConfigTransformer.exe transform -xap [xap filepath] -environment [environment name]");
                Console.WriteLine(
                    @"Example: XAPConfigTransformer.exe transform -xap C:\Temp\SilverlightApplication.xap -environment Test");
                ExitWithAnErrorStatus();
            }

            ApplyConfigTransformationIfAvailable(inputParameters);
        }

        public static string TempSilverlightConfigFile
        {
            get { return Path.Combine(_platformTempFilePath, SILVERLIGHT_CONFIG_FILE); }
        }

        private static void ApplyConfigTransformationIfAvailable(ProgramParameters inputParameters)
        {
            using (ZipFile zippedXapFile = ZipFile.Read(inputParameters.XapLocation))
            {

                if (!zippedXapFile.ContainsEntry(SILVERLIGHT_CONFIG_FILE))
                    ExitAsNoChangePossible();

                if (!zippedXapFile.ContainsEntry(
                    GetSilverlightEnvironmentTransformFile(inputParameters.TargetEnvironment)))
                    ExitAsNoChangeAvailable(inputParameters.TargetEnvironment);
            
                PerformTransformation(zippedXapFile, inputParameters);
            }
        }

        private static void PerformTransformation(ZipFile zippedXapFile, ProgramParameters inputParameters)
        {
            RemoveTemporarySilverlightConfigFile();
            
            string silverlightEnvironmentTransformFile = 
                GetSilverlightEnvironmentTransformFile(inputParameters.TargetEnvironment);

            RemoveTemporarySilverlightTransformFile(silverlightEnvironmentTransformFile);

            ExecuteTransformationOnArchive(
                zippedXapFile, inputParameters.TargetEnvironment, silverlightEnvironmentTransformFile);

            RemoveTemporarySilverlightConfigFile();
            RemoveTemporarySilverlightTransformFile(silverlightEnvironmentTransformFile); 
        }

        private static void ExecuteTransformationOnArchive(
            ZipFile zippedXapFile, 
            string targetEnvironment,                                                           
            string silverlightEnvironmentTransformFile)
        {
            zippedXapFile[SILVERLIGHT_CONFIG_FILE].Extract(_platformTempFilePath);
            zippedXapFile[silverlightEnvironmentTransformFile].Extract(_platformTempFilePath);

            new MsBuildConfigTransform().ExecuteTransformation(targetEnvironment, _platformTempFilePath);

            zippedXapFile.RemoveEntry(SILVERLIGHT_CONFIG_FILE);
            zippedXapFile.AddFile(Path.Combine(_platformTempFilePath, SILVERLIGHT_CONFIG_FILE), "");
            zippedXapFile.Save();
        }

        private static void RemoveTemporarySilverlightTransformFile(string silverlightEnvironmentTransformFile)
        {
            string temporarySilverlightEnvironmentTransformFile = Path.Combine(_platformTempFilePath,
                                                                               silverlightEnvironmentTransformFile);

            RemoveFileIfExists(temporarySilverlightEnvironmentTransformFile);
        }

        private static void RemoveTemporarySilverlightConfigFile()
        {
            RemoveFileIfExists(TempSilverlightConfigFile);
        }

        private static void RemoveFileIfExists(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        private static void ExitAsNoChangePossible()
        {
            Console.WriteLine("There is no ServiceReferences.ClientConfig file to transform");
            ExitWithAnErrorStatus();
        }

        private static void ExitAsNoChangeAvailable(string targetEnvironment)
        {
            Console.WriteLine("There is no {0} file available to transform with. No transformation applied.", 
                GetSilverlightEnvironmentTransformFile(targetEnvironment));
            ExitWithAnOkStatus();
        }

        private static string GetSilverlightEnvironmentTransformFile(string targetEnvironment)
        {
            return string.Format(SILVERLIGHT_CONFIG_FILE_PATTERN, targetEnvironment);
        }

        private static void ExitWithAnErrorStatus()
        {
            const int errorCode = 1;
            Environment.Exit(errorCode);
        }
        
        private static void ExitWithAnOkStatus()
        {
            const int informationCode = 0;
            Environment.Exit(informationCode);
        }

        private class ProgramParameters
        {
            public string XapLocation { get; private set; }
            public string TargetEnvironment { get; private set; }

            public bool ValidParameters
            {
                get
                {
                    return !String.IsNullOrWhiteSpace(XapLocation) 
                        && !String.IsNullOrWhiteSpace(TargetEnvironment)
                        && XapLocation.ToLower().Contains(".xap");
                }
            }

            private ProgramParameters(string xapLocation, string targetEnvironment)
            {
                XapLocation = xapLocation;
                TargetEnvironment = targetEnvironment;
            }
            
            public static ProgramParameters Create(string[] args)
            {
                if(args[0].ToLower() != "transform")
                    return new ProgramParameters("", "");
                return new ProgramParameters(FindXapLocation(args), FindTargetEnvironment(args));
            }

            private static string FindXapLocation(string[] args)
            {
                int xapArgumentIndex = Array.FindIndex(args, argument => argument.ToLower() == "-xap");
                return NextArgumentIfValid(args, xapArgumentIndex);
            }

            private static string FindTargetEnvironment(string[] args)
            {
                int environmentArgumentIndex = 
                    Array.FindIndex(args, argument => argument.ToLower() == "-environment");
                return NextArgumentIfValid(args, environmentArgumentIndex);
            }            
            
            private static string NextArgumentIfValid(string[] args, int argumentIndex)
            {
                int nextArgumentIndex = argumentIndex + 1;
                return argumentIndex != -1 
                    && args.Length != nextArgumentIndex 
                    && !args[nextArgumentIndex].StartsWith("-") ? args[nextArgumentIndex] : "";
            }
        }
    }
}
