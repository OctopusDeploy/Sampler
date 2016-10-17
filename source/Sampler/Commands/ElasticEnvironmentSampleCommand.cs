using System.Linq;
using System.Threading.Tasks;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Client.Model.DeploymentProcess;
using Octopus.Sampler.Infrastructure;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("elastic-sample", Description = "Applies the elastic environments deployment sample.")]
    public class ElasticEnvironmentSampleCommand : ApiCommand
    {
        private const int DefaultNumberOfProjects = 2;
        private const int DefaultNumberOfEnvironments = 2;

        private static readonly ILogger Log = Serilog.Log.ForContext<ElasticEnvironmentSampleCommand>();

        public ElasticEnvironmentSampleCommand(IOctopusClientFactory octopusClientFactory)
            : base(octopusClientFactory)
        {
            var options = Options.For("elastic sample");
            options.Add("projects=", $"[Optional] Number of projects to create, default {DefaultNumberOfProjects}", v => NumberOfProjects = int.Parse(v));
            options.Add("environments=", $"[Optional] Number of environments to create, default {DefaultNumberOfEnvironments}", v => NumberOfEnvironments = int.Parse(v));
        }

        public int NumberOfProjects { get; protected set; } = DefaultNumberOfProjects;
        public int NumberOfEnvironments { get; protected set; } = DefaultNumberOfEnvironments;

        protected override async Task Execute()
        {
            Log.Information($"Building elastic environment test data with {NumberOfProjects} projects, {NumberOfEnvironments} environments");

            var environments =
                await Task.WhenAll(
                    Enumerable.Range(0, NumberOfEnvironments)
                        .Select(i =>
                        {
                            var environmentName = $"Environment-{i}";
                            Log.Information("Creating {EnvironmentName}...", environmentName);
                            return Repository.Environments.CreateOrModify(environmentName);
                        })
                        .ToArray()
                );
            var lifecycle = await Repository.Lifecycles.CreateOrModify("The Lifecycle");
            await lifecycle.AsSimplePromotionLifecycle(environments.Select(e => e.Instance)).Save();

            var projectGroup = await Repository.ProjectGroups.CreateOrModify("The project group");

            Log.Information("Building {Count} sample projects...", NumberOfProjects);
            var projects = Enumerable.Range(0, NumberOfProjects)
                .Select(async i =>
                {
                    var projectName = $"Project-{i}";
                    Log.Information("Creating {ProjectName}...", projectName);
                    var projectEditor =
                        await Repository.Projects.CreateOrModify(projectName, projectGroup.Instance, lifecycle.Instance);
                    var deploymentProcess = await projectEditor.DeploymentProcess;
                    deploymentProcess
                        .AddOrUpdateStep("Deploy Application")
                        .AddOrUpdateScriptAction("Deploy Application",
                            new InlineScriptActionFromFileInAssembly("ElasticEnvironmentSample.Deploy.ps1"),
                            ScriptTarget.Server);

                    await projectEditor.Triggers.CreateOrModify("MyTrigger", ProjectTriggerType.DeploymentTarget,
                        ProjectTriggerConditionEvent.NewDeploymentTargetBecomesAvailable,
                        ProjectTriggerConditionEvent.ExistingDeploymentTargetChangesState);

                    await projectEditor.Save();

                    return projectEditor;
                });

            await Task.WhenAll(projects.ToArray());
        }
    }
}