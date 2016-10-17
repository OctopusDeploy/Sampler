using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Autofac;
using Octopus.Client;
using Octopus.Client.Exceptions;
using Octopus.Sampler.Extensions;
using Octopus.Sampler.Infrastructure;
using Serilog;

namespace Octopus.Sampler
{
    class Program
    {
        private static ILogger log;

        public static int Main(string[] args)
        {
            log = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();
            Log.Logger = log;

            log.Information("Octopus Deploy Sampler, version {Version}", typeof(Program).Assembly.GetInformationalVersion());
            Console.Title = "Octopus Deploy Sampler";

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Ssl3
                | SecurityProtocolType.Tls
                | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls12;

            try
            {
                var container = BuildContainer();
                var commandLocator = container.Resolve<ICommandLocator>();
                var first = GetFirstArgument(args);
                var command = GetCommand(first, commandLocator);
                using (log.BeginTimedOperation(command.GetType().Name))
                {
                    command.Execute(args.Skip(1).ToArray()).GetAwaiter().GetResult();
                }

                return 0;
            }
            catch (Exception exception)
            {
                var exit = PrintError(exception);
                Console.WriteLine("Exit code: " + exit);
                return exit;
            }
        }

        static IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();
            var thisAssembly = typeof(Program).Assembly;

            builder.RegisterAssemblyTypes(thisAssembly).As<ICommand>().AsSelf();
            builder.RegisterType<CommandLocator>().As<ICommandLocator>();
            builder.RegisterType<OctopusClientFactory>().As<IOctopusClientFactory>();

            return builder.Build();
        }

        static ICommand GetCommand(string first, ICommandLocator commandLocator)
        {
            if (string.IsNullOrWhiteSpace(first))
            {
                return commandLocator.Find("help");
            }

            var command = commandLocator.Find(first);
            if (command == null)
                throw new CommandException("Error: Unrecognized command '" + first + "'");

            return command;
        }

        static string GetFirstArgument(IEnumerable<string> args)
        {
            return (args.FirstOrDefault() ?? string.Empty).ToLowerInvariant().TrimStart('-', '/');
        }

        static int PrintError(Exception ex)
        {
            var agg = ex as AggregateException;
            if (agg != null)
            {
                var errors = new HashSet<Exception>(agg.InnerExceptions);
                if (agg.InnerException != null)
                    errors.Add(ex.InnerException);

                var lastExit = 0;
                foreach (var inner in errors)
                {
                    lastExit = PrintError(inner);
                }

                return lastExit;
            }

            var cmd = ex as CommandException;
            if (cmd != null)
            {
                log.Error(ex.Message);
                return -1;
            }

            var reflex = ex as ReflectionTypeLoadException;
            if (reflex != null)
            {
                log.Error(ex, ex.Message);

                foreach (var loaderException in reflex.LoaderExceptions)
                {
                    log.Error(loaderException, loaderException.Message);

                    var exFileNotFound = loaderException as FileNotFoundException;
                    if (exFileNotFound != null &&
                        !string.IsNullOrEmpty(exFileNotFound.FusionLog))
                    {
                        log.Error("Fusion log: {0}", exFileNotFound.FusionLog);
                    }
                }

                return -43;
            }

            var octo = ex as OctopusException;
            if (octo != null)
            {
                log.Error("Error from Octopus server (HTTP " + octo.HttpStatusCode + "): " + octo.Message);
                return -7;
            }

            log.Error(ex, ex.Message);
            return -3;
        }
    }
}
