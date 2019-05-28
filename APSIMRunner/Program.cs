﻿namespace APSIMRunner
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.Run;
    using System;
    using System.Threading;

    class Program
    {
        /// <summary>Main program</summary>
        static int Main(string[] args)
        {
            try
            { 
                AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

                // Send a command to socket server to get the job to run.
                object response = GetNextJob();
                while (response != null)
                {
                    JobRunnerMultiProcess.GetJobReturnData job = response as JobRunnerMultiProcess.GetJobReturnData;

                    // Run the simulation.
                    Exception error = null;
                    var storage = new StorageViaSockets(job.fileName, job.id);
                    IModel modelToRun = null;
                    try
                    {
                        IRunnable jobToRun = job.job;
                        if (jobToRun is IModel)
                        {
                            modelToRun = job.job as IModel;

                            // Add in a socket datastore to satisfy links.
                            modelToRun.Children.Add(storage);
                        }
                        else
                            throw new Exception("Unknown job type: " + jobToRun.GetType().Name);

                        jobToRun.Run(new CancellationTokenSource());
                    }
                    catch (Exception err)
                    {
                        error = err;
                    }

                    // Signal end of job.
                    JobRunnerMultiProcess.EndJobArguments endJobArguments = new JobRunnerMultiProcess.EndJobArguments
                    {
                        errorMessage = error,
                        id = job.id
                    };
                    SocketServer.CommandObject endJobCommand = new SocketServer.CommandObject() { name = "EndJob", data = endJobArguments };
                    SocketServer.Send("127.0.0.1", 2222, endJobCommand);

                    // Get next job.
                    response = GetNextJob();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= Manager.ResolveManagerAssembliesEventHandler;
            }
            return 0;
        }

        /// <summary>Get the next job to run. Returns the job to run or null if no more jobs.</summary>
        private static object GetNextJob()
        {
            SocketServer.CommandObject command = new SocketServer.CommandObject() { name = "GetJob" };
            object response = null;

            while (response == null)
            {
                try
                {
                    response = SocketServer.Send("127.0.0.1", 2222, command);

                    if (response is string && response.ToString() == "NULL")
                        return null;
                }
                catch (Exception)
                {
                    // connection forcibly closed?
                    response = null;
                    Thread.Sleep(200);
                }
            }
            return response;
        }
    }
}
