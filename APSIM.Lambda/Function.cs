using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using APSIM.Lambda.Options;
using APSIM.Interop.Documentation;
using APSIM.Interop.Documentation.Formats;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using CommandLine;
using Models.Core;
using Models.Core.Apsim710File;
using Models.Core.ApsimFile;
using Models.Core.Run;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace APSIM.Lambda
{
    public class Function
    {

        private static int exitCode = 0;

        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigProxyEvent)
        {
            APIGatewayProxyResponse requestResponse = new APIGatewayProxyResponse();

            //System.IO.File.Copy(sourceFile, destFile, true);

            string[] args = new string[3];

            args[0] = "run";
            args[1] = "--run-tests";
            args[2] = "c:\\temp\\wsimpson-04192-01.apsimx";



            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                new Parser(config =>
                {
                    config.AutoHelp = true;
                    config.HelpWriter = Console.Out;
                }).ParseArguments<RunOptions, DocumentOptions, ImportOptions>(args)
                .WithParsed<RunOptions>(Run)
                .WithParsed<DocumentOptions>(Document)
                .WithParsed<ImportOptions>(Import)
                .WithNotParsed(HandleParseError);
                requestResponse.StatusCode = 200;
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.ToString());
                requestResponse.StatusCode = 500;
                requestResponse.Body = err.ToString();
            }
            return requestResponse;
        }

        /// <summary>
        /// Handles parser errors to ensure that a non-zero exit code
        /// is returned when parse errors are encountered.
        /// </summary>
        /// <param name="errors">Parse errors.</param>
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            if (!(errors.IsHelp() || errors.IsVersion()))
                exitCode = 1;
        }

        private static void Run(RunOptions options)
        {
            if (options.Files == null || !options.Files.Any())
            {
                throw new ArgumentException($"No files were specified");
            }
            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (!files.Any())
            {
                files = options.Files;
            }
            foreach (string file in files)
            {
                Simulations sims = FileFormat.ReadFromFile<Simulations>(file, e => throw e, false);

                Runner runner = new Runner(sims);
                List<Exception> errors = runner.Run();

                if (errors != null && errors.Count > 0)
                {
                    throw new AggregateException("File ran with errors", errors);
                }
            }
        }

        private static void Document(DocumentOptions options)
        {
            if (options.Files == null || !options.Files.Any())
                throw new ArgumentException($"No files were specified");
            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (!files.Any())
                files = options.Files;
            foreach (string file in files)
            {
                Simulations sims = FileFormat.ReadFromFile<Simulations>(file, e => throw e, false);
                IModel model = sims;
                if (Path.GetExtension(file) == ".json")
                    sims.Links.Resolve(sims, true, true, false);
                if (!string.IsNullOrEmpty(options.Path))
                {
                    IVariable variable = model.FindByPath(options.Path);
                    if (variable == null)
                        throw new Exception($"Unable to resolve path {options.Path}");
                    object value = variable.Value;
                    if (value is IModel modelAtPath)
                        model = modelAtPath;
                    else
                        throw new Exception($"{options.Path} resolved to {value}, which is not a model");
                }

                string pdfFile = Path.ChangeExtension(file, ".pdf");
                string directory = Path.GetDirectoryName(file);
                PdfWriter writer = new PdfWriter(new PdfOptions(directory, null));
                IEnumerable<ITag> tags = options.ParamsDocs ? new ParamsInputsOutputs(model).Document() : model.Document();
                writer.Write(pdfFile, tags);
            }
        }

        private static void Import(ImportOptions options)
        {
            if (options.Files == null || !options.Files.Any())
                throw new ArgumentException($"No files were specified");

            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (!files.Any())
                files = options.Files;

            foreach (string file in files)
            {
                var importer = new Importer();
                importer.ProcessFile(file);
            }
        }


    }
}