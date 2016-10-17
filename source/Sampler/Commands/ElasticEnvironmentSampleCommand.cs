using System.Linq;
using Octopus.Client.Model;
using Octopus.Client.Model.DeploymentProcess;
using Octopus.Sampler.Infrastructure;
using Octopus.Sampler.Integration;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("elastic-sample", Description = "Applies the elastic environments deployment sample.")]
    public class ElasticEnvironmentSampleCommand : ApiCommand
    {
        private const int DefaultNumberOfProjects = 2;
        private const int DefaultNumberOfEnvironments = 2;

        private static readonly ILogger Log = Serilog.Log.ForContext<ElasticEnvironmentSampleCommand>();

        public ElasticEnvironmentSampleCommand(IOctopusRepositoryFactory octopusRepositoryFactory)
            : base(octopusRepositoryFactory)
        {
            var options = Options.For("elastic sample");
            options.Add("projects=", $"[Optional] Number of projects to create, default {DefaultNumberOfProjects}", v => NumberOfProjects = int.Parse(v));
            options.Add("environments=", $"[Optional] Number of environments to create, default {DefaultNumberOfEnvironments}", v => NumberOfEnvironments = int.Parse(v));
        }

        public int NumberOfProjects { get; protected set; } = DefaultNumberOfProjects;
        public int NumberOfEnvironments { get; protected set; } = DefaultNumberOfEnvironments;

        protected override void Execute()
        {
            Log.Information($"Building elastic environment test data with {NumberOfProjects} projects, {NumberOfEnvironments} environments");
            
            var environments = Enumerable.Range(0, NumberOfEnvironments).Select(i =>
            {
                var environmentName = $"Environment-{i}";
                Log.Information("Creating {EnvironmentName}...", environmentName);
                return Repository.Environments.CreateOrModify(environmentName).Instance;
            }).ToArray();
            var lifecycle = Repository.Lifecycles.CreateOrModify("The Lifecycle").AsSimplePromotionLifecycle(environments).Save();
            var projectGroup = Repository.ProjectGroups.CreateOrModify("The project group");

            Log.Information("Building {Count} sample projects...", NumberOfProjects);
            var projects = Enumerable.Range(0, NumberOfProjects)
                .Select(i =>
                {
                    var projectName = $"Project-{i}";
                    Log.Information("Creating {ProjectName}...", projectName);
                    var projectEditor = Repository.Projects.CreateOrModify(projectName, projectGroup.Instance, lifecycle.Instance);
                    projectEditor.DeploymentProcess.AddOrUpdateStep("Deploy Application")
                        .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("ElasticEnvironmentSample.Deploy.ps1"), ScriptTarget.Server);

                    projectEditor.Triggers.CreateOrModify("MyTrigger", ProjectTriggerType.DeploymentTarget,
                        ProjectTriggerConditionEvent.NewDeploymentTargetBecomesAvailable,
                        ProjectTriggerConditionEvent.ExistingDeploymentTargetChangesState);
                    
                    projectEditor.Save();

                    return projectEditor;
                })
            .ToArray();
        }
    }
}