using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLipsum.Core;
using Octopus.Client;
using Octopus.Client.Model.DeploymentProcess;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;
using Octopus.Sampler.Extensions;
using Octopus.Sampler.Infrastructure;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("trucks-sample", Description = "Applies the trucks sample.")]
    public class TrucksSampleCommand : ApiCommand
    {
        private const int DefaultNumberOfTrucks = 10;
        private static readonly LipsumGenerator LipsumRobinsonoKruso = new LipsumGenerator(Lipsums.RobinsonoKruso, isXml: false);

        private static readonly ILogger Log = Serilog.Log.ForContext<TrucksSampleCommand>();

        public TrucksSampleCommand(IOctopusClientFactory octopusClientFactory)
            : base(octopusClientFactory)
        {
            var options = Options.For("Trucks sample");
            options.Add("trucks=", $"[Optional] Number of trucks to create, default {DefaultNumberOfTrucks}", v => NumberOfTrucks = int.Parse(v));
        }

        public int NumberOfTrucks { get; protected set; } = DefaultNumberOfTrucks;

        protected override async Task Execute()
        {
            Log.Information("Building trucks sample with {TrucksCount} trucks...", NumberOfTrucks);

            var environment = await Repository.Environments.CreateOrModify("Trucks Production", LipsumRobinsonoKruso.GenerateLipsum(1));
            var normalLifecycle = await Repository.Lifecycles.CreateOrModify("Trucks Normal Lifecycle", "The normal lifecycle for the trucks sample");
            await normalLifecycle.AsSimplePromotionLifecycle(new[] { environment.Instance }).Save();
            var projectGroup = await Repository.ProjectGroups.CreateOrModify("Trucks sample");

            await BuildServerProject(projectGroup.Instance, normalLifecycle.Instance);

            await BuildClientProject(projectGroup.Instance, normalLifecycle.Instance);

            var trucks = await Task.WhenAll(
                Enumerable.Range(0, NumberOfTrucks)
                    .Select(i =>
                    {
                        var truckName = $"Truck-{i:0000}";
                        Log.Information("Setting up truck {TruckName}...", truckName);
                        return Repository.Machines.CreateOrModify(truckName, new CloudRegionEndpointResource(),
                            new[] { environment.Instance }, new[] { "truck" });
                    })
                    .ToArray()
            );

            Log.Information("Created {TruckCount} trucks.", trucks.Length);

            await StartTrucksMoving(trucks.Select(t => t.Instance).ToArray());
        }

        private async Task BuildServerProject(ProjectGroupResource projectGroup, LifecycleResource normalLifecycle)
        {
            var serverProjectEditor = await Repository.Projects.CreateOrModify("Truck Tracker Server", projectGroup, normalLifecycle);
                serverProjectEditor.SetLogo(SampleImageCache.DownloadImage("http://blog.budgettrucks.com.au/wp-content/uploads/2015/08/tweed-heads-moving-truck-rental-map.jpg"));

            (await serverProjectEditor.Variables).AddOrUpdateVariableValue("DatabaseConnectionString", $"Server=trackerdb.com;Database=trackerdb;");
            (await serverProjectEditor.DeploymentProcess).AddOrUpdateStep("Deploy Application")
                .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("TrucksSample.Server.Deploy.fsx"), ScriptTarget.Server);

            await serverProjectEditor.Save();
        }

        private async Task BuildClientProject(ProjectGroupResource projectGroup, LifecycleResource normalLifecycle)
        {
            var clientProjectEditor = await Repository.Projects.CreateOrModify("Truck Tracker Client", projectGroup, normalLifecycle);
                clientProjectEditor.SetLogo(SampleImageCache.DownloadImage("http://b2bimg.bridgat.com/files/GPS_Camera_TrackerGPS_Camera_Tracking.jpg", "GPS_Camera_TrackerGPS_Camera_Tracking.jpg"));

            var variables = await clientProjectEditor.Variables;
            variables.AddOrUpdateVariableValue("TrackerUrl", "https://trucktracker.com/trucks/#{Octopus.Machine.Name}");

            var channel = await clientProjectEditor.Channels.CreateOrModify("1.x Normal", "The channel for stable releases that will be deployed to our production trucks.");
            channel.SetAsDefaultChannel();

            await clientProjectEditor.Channels.Delete("Default");

            var deploymentProcess = await clientProjectEditor.DeploymentProcess;
            deploymentProcess.AddOrUpdateStep("Deploy Application")
                .TargetingRoles("truck")
                .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("TrucksSample.Client.Deploy.fsx"), ScriptTarget.Target);

            var machineFilter = new MachineFilterResource();
            machineFilter.EventGroups.Add("MachineAvailableForDeployment");
            await clientProjectEditor.Triggers.CreateOrModify("Auto-Deploy to trucks when available",
                machineFilter,
                new AutoDeployActionResource());

            await clientProjectEditor.Save();
        }

        private async Task StartTrucksMoving(MachineResource[] trucks)
        {
            Log.Information("Starting to simulate trucks moving in and out of depot...");

            var i = 0;
            while (true)
            {
                i++;

                if (i >= 24) i = 0;


                Log.Information("Time: {Time}", $"{i * 100:0000}HRS");
                var targets = await Repository.Machines.FindByNames(trucks.Select(t => t.Name));

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

                else if (i == 17)
                {
                    Log.Information("Day's finished... All trucks back!");
                    ReturnToDepot(targets);
                    Thread.Sleep(20000);
                }

                else if ((i >= 5 && i <= 11) || (i >= 13 && i <= 16))
                {
                    LeaveDepot(targets.Where(t => !t.IsDisabled).ToList());
                    ReturnToDepot(new List<MachineResource>() { targets.SelectRandom() });
                    Thread.Sleep(10000);
                }

                Thread.Sleep(1000);
            }
        }

        private void LeaveDepot(List<MachineResource> targets)
        {
            Log.Information("Leaving depot: {Leaving}", targets.Select(t => t.Name));
            foreach (var target in targets)
            {
                target.IsDisabled = true;
                Repository.Machines.Modify(target);
            }
        }

        private void ReturnToDepot(List<MachineResource> targets)
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