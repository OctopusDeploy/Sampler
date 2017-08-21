using System.Threading.Tasks;
using NLipsum.Core;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Client.Model.DeploymentProcess;
using Octopus.Sampler.Infrastructure;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("simple-project-sample", Description = "A simple project.")]
    public class SimpleProjectSampleCommand : ApiCommand
    {
        private const int DefaultNumberOfProjects = 10;
        private static readonly LipsumGenerator LipsumRobinsonoKruso = new LipsumGenerator(Lipsums.RobinsonoKruso, isXml: false);

        private static readonly ILogger Log = Serilog.Log.ForContext<SimpleProjectSampleCommand>();

        public SimpleProjectSampleCommand(IOctopusClientFactory octopusClientFactory)
            : base(octopusClientFactory)
        {
            var options = Options.For("Simple Project sample");
            options.Add("count=", $"[Optional] Number of projects to create, default {DefaultNumberOfProjects}", v => NumberOfProjects = int.Parse(v));
        }

        public int NumberOfProjects { get; protected set; } = DefaultNumberOfProjects;

        protected override async Task Execute()
        {
            Log.Information("Building simple project sample with {ProjectCount} projects...", NumberOfProjects);

            var environment = await Repository.Environments.CreateOrModify("Simple Project Environment", LipsumRobinsonoKruso.GenerateLipsum(1));
            var normalLifecycle = await Repository.Lifecycles.CreateOrModify("Simple Project Lifecycle", "The normal lifecycle for the Simple Project sample");
            await normalLifecycle.AsSimplePromotionLifecycle(new[] { environment.Instance }).Save();
            var projectGroup = await Repository.ProjectGroups.CreateOrModify("Simple Project sample");

            for (int i = 0; i < NumberOfProjects; i++)
            {
                Log.Information("Creating project {ProjectName} ...", $"Simple Project {i.ToString().PadLeft(4, '0')}");

                var project = await Repository.Projects.CreateOrModify($"Simple Project {i.ToString().PadLeft(4, '0')}", projectGroup.Instance, normalLifecycle.Instance);
                (await project.DeploymentProcess).AddOrUpdateStep("Deploy Application")
                    .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("SimpleProjectSample.Deploy.ps1"), ScriptTarget.Server);

                project.Customize(p => p.TenantedDeploymentMode = TenantedDeploymentMode.TenantedOrUntenanted);

                await project.Save();
            }
        }
    }
}