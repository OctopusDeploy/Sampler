using System.Linq;
using System.Threading;
using NLipsum.Core;
using Octopus.Client.Model.DeploymentProcess;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;
using Octopus.Sampler.Extensions;
using Octopus.Sampler.Infrastructure;
using Octopus.Sampler.Integration;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("trucks-sample", Description = "Applies the trucks sample.")]
    public class TrucksSampleCommand : ApiCommand
    {
        private const int DefaultNumberOfTrucks = 10;
        private static readonly LipsumGenerator LipsumRobinsonoKruso = new LipsumGenerator(Lipsums.RobinsonoKruso, isXml: false);

        private static readonly ILogger Log = Serilog.Log.ForContext<TrucksSampleCommand>();

        public TrucksSampleCommand(IOctopusRepositoryFactory octopusRepositoryFactory)
            : base(octopusRepositoryFactory)
        {
            var options = Options.For("Trucks sample");
            options.Add("trucks=", $"[Optional] Number of trucks to create, default {DefaultNumberOfTrucks}", v => NumberOfTrucks = int.Parse(v));
        }

        public int NumberOfTrucks { get; protected set; } = DefaultNumberOfTrucks;

        protected override void Execute()
        {
            Log.Information("Building trucks sample with {TrucksCount} trucks...", NumberOfTrucks);

            var environments = new[] { "Trucks Production" }.Select(name => Repository.Environments.CreateOrModify(name, LipsumRobinsonoKruso.GenerateLipsum(1)).Instance).ToArray();
            var normalLifecycle = Repository.Lifecycles.CreateOrModify("Trucks Normal Lifecycle", "The normal lifecycle for the trucks sample").AsSimplePromotionLifecycle(environments.ToArray()).Save().Instance;
            var projectGroup = Repository.ProjectGroups.CreateOrModify("Trucks sample").Instance;

            BuildServerProject(projectGroup, normalLifecycle);

            BuildClientProject(projectGroup, normalLifecycle);

            var env = environments.Where(e => e.Name.EndsWith("Production")).ToArray();
            var trucks = Enumerable.Range(0, NumberOfTrucks)
                .Select(i =>
                {
                    var truckName = $"Truck-{i:0000}";
                    Log.Information("Setting up truck {TruckName}...", truckName);
                    return Repository.Machines.CreateOrModify(truckName, new CloudRegionEndpointResource(), env, new[] {"truck"}).Instance;
                })
                .ToArray();

            Log.Information("Created {TruckCount} trucks.", trucks.Length);

            StartTrucksMoving(trucks);
        }

        private void BuildServerProject(ProjectGroupResource projectGroup, LifecycleResource normalLifecycle)
        {
            var serverProjectEditor = Repository.Projects.CreateOrModify("Truck Tracker Server", projectGroup, normalLifecycle)
                .SetLogo(SampleImageCache.DownloadImage("http://blog.budgettrucks.com.au/wp-content/uploads/2015/08/tweed-heads-moving-truck-rental-map.jpg"));

            serverProjectEditor.Variables.AddOrUpdateVariableValue("DatabaseConnectionString", $"Server=trackerdb.com;Database=trackerdb;");
            serverProjectEditor.DeploymentProcess.AddOrUpdateStep("Deploy Application")
                .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("TrucksSample.Server.Deploy.fsx"), ScriptTarget.Server);

            serverProjectEditor.Save();
        }

        private void BuildClientProject(ProjectGroupResource projectGroup, LifecycleResource normalLifecycle)
        {
            var clientProjectEditor = Repository.Projects.CreateOrModify("Truck Tracker Client", projectGroup, normalLifecycle)
                .SetLogo(SampleImageCache.DownloadImage("http://b2bimg.bridgat.com/files/GPS_Camera_TrackerGPS_Camera_Tracking.jpg", "GPS_Camera_TrackerGPS_Camera_Tracking.jpg"));

            clientProjectEditor.Variables
                .AddOrUpdateVariableValue("TrackerUrl", "https://trucktracker.com/trucks/#{Octopus.Machine.Name}");

            clientProjectEditor.Channels.CreateOrModify("1.x Normal", "The channel for stable releases that will be deployed to our production trucks.")
                .SetAsDefaultChannel();

            clientProjectEditor.Channels.Delete("Default");

            clientProjectEditor.DeploymentProcess.AddOrUpdateStep("Deploy Application")
                .TargetingRoles("truck")
                .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("TrucksSample.Client.Deploy.fsx"), ScriptTarget.Target);

            clientProjectEditor.Triggers.CreateOrModify("Auto-Deploy to trucks when available",
                ProjectTriggerType.DeploymentTarget,
                ProjectTriggerConditionEvent.ExistingDeploymentTargetChangesState,
                ProjectTriggerConditionEvent.NewDeploymentTargetBecomesAvailable);

            clientProjectEditor.Save();
        }

        private void StartTrucksMoving(MachineResource[] trucks)
        {
            Log.Information("Starting to simulate trucks moving in and out of depot...");

            var i = 0;
            while (true)
            {
                i++;

                if (i >= 24) i = 0;


                Log.Information("Time: {Time}", $"{i*100:0000}HRS");
                var targets = Repository.Machines.FindByNames(trucks.Select(t => t.Name)).ToArray();

                if (i == 4)
                {
                    Log.Information("Starting the morning shift... All trucks out!");
                    LeaveDepot(targets);
                }

                else if (i == 12)
                {
                    Log.Information("Lunch time... All trucks back!");
                    ReturnToDepot(targets);
                    Thread.Sleep(20000);
                }

                else if(i == 17)
                {
                    Log.Information("Day's finished... All trucks back!");
                    ReturnToDepot(targets);
                    Thread.Sleep(20000);
                }

                else if((i >= 5 && i <= 11) || (i >= 13 && i <= 16))
                {
                    LeaveDepot(targets.Where(t => !t.IsDisabled).ToArray());
                    ReturnToDepot(targets.SelectRandom());
                    Thread.Sleep(10000);
                }

                Thread.Sleep(1000);
            }
        }

        private void LeaveDepot(params MachineResource[] targets)
        {
            Log.Information("Leaving depot: {Leaving}", targets.Select(t => t.Name));
            foreach (var target in targets)
            {
                target.IsDisabled = true;
                Repository.Machines.Modify(target);
            }
        }

        private void ReturnToDepot(params MachineResource[] targets)
        {
            Log.Information("Returning to depot: {Returning}", targets.Select(t => t.Name));
            foreach (var target in targets)
            {
                target.IsDisabled = false;
                Repository.Machines.Modify(target);
            }
        }
    }
}