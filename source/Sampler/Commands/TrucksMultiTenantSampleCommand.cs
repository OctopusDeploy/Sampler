using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLipsum.Core;
using Octopus.Client;
using Octopus.Client.Editors;
using Octopus.Client.Model.DeploymentProcess;
using Octopus.Client.Model;
using Octopus.Client.Model.Endpoints;
using Octopus.Sampler.Extensions;
using Octopus.Sampler.Infrastructure;
using Octopus.Sampler.Integration;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("trucks-multitenantsample", Description = "Applies the trucks sample using multi-tenant deployments.")]
    public class TrucksMultiTenantSampleCommand : ApiCommand
    {
        private const int DefaultNumberOfTrucks = 10;
        private static readonly LipsumGenerator LipsumRobinsonoKruso = new LipsumGenerator(Lipsums.RobinsonoKruso, isXml: false);

        private static readonly ILogger Log = Serilog.Log.ForContext<TrucksMultiTenantSampleCommand>();

        public TrucksMultiTenantSampleCommand(IOctopusRepositoryFactory octopusRepositoryFactory)
            : base(octopusRepositoryFactory)
        {
            var options = Options.For("Trucks sample");
            options.Add("trucks=", $"[Optional] Number of trucks to create, default {DefaultNumberOfTrucks}", v => NumberOfTrucks = int.Parse(v));
        }

        public int NumberOfTrucks { get; protected set; } = DefaultNumberOfTrucks;

        public static class VariableKeys
        {
            public static class StandardTenantDetails
            {
                public static readonly string TruckAlias = "Truck.Alias";
            }
        }

        protected override void Execute()
        {
            Log.Information("Building trucks sample with {TrucksCount} trucks using multi-tenant deployments...", NumberOfTrucks);

            EnsureMultitenancyFeature(Repository);

            Log.Information("Setting up environment...");
            var environments = new[] { "Trucks Production" }.Select(name => Repository.Environments.CreateOrModify(name, LipsumRobinsonoKruso.GenerateLipsum(1)).Instance).ToArray();
            var normalLifecycle = Repository.Lifecycles.CreateOrModify("Trucks Normal Lifecycle", "The normal lifecycle for the trucks sample").AsSimplePromotionLifecycle(environments.ToArray()).Save().Instance;
            var projectGroup = Repository.ProjectGroups.CreateOrModify("Trucks sample").Instance;

            Log.Information("Setting up tags...");
            var tagSetTruckType = Repository.TagSets.CreateOrModify("Truck type", "Allows you to categorize tenants")
                .AddOrUpdateTag("General Waste", "These are the trucks that deal with general waste", TagResource.StandardColor.DarkRed)
                .AddOrUpdateTag("Green Waste", "These are the trucks that deal with green waste", TagResource.StandardColor.DarkGreen)
                .AddOrUpdateTag("Recycling", "These are the trucks that deal with recycling", TagResource.StandardColor.LightYellow)
                .Save().Instance;
            var tagSetRing = Repository.TagSets.CreateOrModify("Upgrade ring", "The order in which to upgrade sets of trucks")
                .AddOrUpdateTag("Canary", "Upgrade these trucks first", TagResource.StandardColor.LightYellow)
                .AddOrUpdateTag("Stable", "Upgrade these trucks last", TagResource.StandardColor.LightGreen)
                .Save().Instance;

            var allTags = new TagResource[0]
                .Concat(tagSetTruckType.Tags)
                .Concat(tagSetRing.Tags)
                .ToArray();

            var getTag = new Func<string, TagResource>(name => allTags.FirstOrDefault(t => t.Name == name));

            Log.Information("Setting up variables...");
            var standardTruckVarEditor = Repository.LibraryVariableSets.CreateOrModify("Standard truck details", "The standard details we require for all trucks");
            standardTruckVarEditor.VariableTemplates
                .AddOrUpdateSingleLineTextTemplate(VariableKeys.StandardTenantDetails.TruckAlias,
                    "Alias", defaultValue: null, helpText: "This alias will be used to build convention-based settings for the truck");
            standardTruckVarEditor.Save();

            var libraryVariableSets = new[] {standardTruckVarEditor.Instance};

            BuildServerProject(projectGroup, normalLifecycle);

            var clientProject = BuildClientProject(projectGroup, normalLifecycle, libraryVariableSets, getTag);

            var logos = new Dictionary<string, string>
            {
                { "General Waste", "http://images.moc-pages.com/user_images/16939/1240756821m_SPLASH.jpg" },
                { "Green Waste", "http://cache.lego.com/e/dynamic/is/image/LEGO/4432?$main$" },
                { "Recycling", "http://lego.lk/wp-content/uploads/2016/03/Garbage-Truck.png" },
            };

            var proj = new[] { clientProject };
            var env = environments.Where(e => e.Name.EndsWith("Production")).ToArray();

            var trucks = Enumerable.Range(0, NumberOfTrucks)
                .Select(i => new
                {
                    Name = $"Truck-{i:0000}",
                    TruckType = tagSetTruckType.Tags.ElementAt(i%3),
                    UpgradeRing = i%5 == 0 ? getTag("Canary") : getTag("Stable")
                })
                .Select((x, i) =>
                {
                    Log.Information("Setting up tenant for truck {TruckName}...", x.Name);
                    var tenantEditor = Repository.Tenants.CreateOrModify(x.Name)
                        .SetLogo(SampleImageCache.DownloadImage(logos[x.TruckType.Name]))
                        .ClearTags().WithTag(x.TruckType).WithTag(x.UpgradeRing);

                    tenantEditor.ClearProjects();
                    foreach (var project in proj)
                    {
                        tenantEditor.ConnectToProjectAndEnvironments(project, env);
                    }
                    tenantEditor.Save();

                    // Ensure the tenant is saved before we attempt to fill out variables - otherwise we don't know what projects they are connected to
                    FillOutTenantVariablesByConvention(tenantEditor, proj, env, libraryVariableSets);

                    return tenantEditor.Save().Instance;
                })
                .ToArray();

            foreach (var truck in trucks)
            {
                Log.Information("Setting up hosting for {TruckName}...", truck.Name);
                var dedicatedHost = Repository.Machines.CreateOrModify(truck.Name, new CloudRegionEndpointResource(), env, new[] {"truck"}, new [] {truck}, new TagResource[0]);
            }

            Log.Information("Created {TruckCount} trucks.", trucks.Length);

            StartTrucksMoving(trucks);
        }

        private void BuildServerProject(ProjectGroupResource projectGroup, LifecycleResource normalLifecycle)
        {
            Log.Information("Setting up server project...");
            var serverProjectEditor = Repository.Projects.CreateOrModify("Truck Tracker Server", projectGroup, normalLifecycle)
                .SetLogo(SampleImageCache.DownloadImage("http://blog.budgettrucks.com.au/wp-content/uploads/2015/08/tweed-heads-moving-truck-rental-map.jpg"));

            serverProjectEditor.Variables.AddOrUpdateVariableValue("DatabaseConnectionString", $"Server=trackerdb.com;Database=trackerdb;");
            serverProjectEditor.DeploymentProcess.AddOrUpdateStep("Deploy Application")
                .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("TrucksSample.Server.Deploy.fsx"), ScriptTarget.Server);

            serverProjectEditor.Save();
        }

        private ProjectResource BuildClientProject(ProjectGroupResource projectGroup, LifecycleResource normalLifecycle, LibraryVariableSetResource[] libraryVariableSets, Func<string, TagResource> getTag)
        {
            Log.Information("Setting up client project...");
            var clientProjectEditor = Repository.Projects.CreateOrModify("Truck Tracker Client", projectGroup, normalLifecycle)
                .SetLogo(SampleImageCache.DownloadImage("http://b2bimg.bridgat.com/files/GPS_Camera_TrackerGPS_Camera_Tracking.jpg", "GPS_Camera_TrackerGPS_Camera_Tracking.jpg"))
                .IncludingLibraryVariableSets(libraryVariableSets)
                .Customize(p => p.TenantedDeploymentMode = ProjectTenantedDeploymentMode.Tenanted);

            clientProjectEditor.Channels.CreateOrModify("1.x Normal", "The channel for stable releases that will be deployed to our production trucks.")
                .SetAsDefaultChannel()
                .AddOrUpdateTenantTags(getTag("Canary"), getTag("Stable"));

            clientProjectEditor.Channels.Delete("Default");

            clientProjectEditor.DeploymentProcess.AddOrUpdateStep("Deploy Application")
                .TargetingRoles("truck")
                .AddOrUpdateScriptAction("Deploy Application", new InlineScriptActionFromFileInAssembly("TrucksSample.Client.Deploy.fsx"), ScriptTarget.Target);

            clientProjectEditor.Triggers.CreateOrModify("Auto-Deploy to trucks when available",
                ProjectTriggerType.DeploymentTarget,
                ProjectTriggerConditionEvent.ExistingDeploymentTargetChangesState,
                ProjectTriggerConditionEvent.NewDeploymentTargetBecomesAvailable);

            clientProjectEditor.Save();

            return clientProjectEditor.Instance;
        }

        private void StartTrucksMoving(TenantResource[] trucks)
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

        void EnsureMultitenancyFeature(IOctopusRepository repo)
        {
            Log.Information("Ensuring multi-tenant deployments are enabled...");
            var features = repo.FeaturesConfiguration.GetFeaturesConfiguration();
            features.IsMultiTenancyEnabled = true;
            repo.FeaturesConfiguration.ModifyFeaturesConfiguration(features);
            repo.Client.RefreshRootDocument();
        }

        private void FillOutTenantVariablesByConvention(
            TenantEditor tenantEditor,
            ProjectResource[] projects,
            EnvironmentResource[] environments,
            LibraryVariableSetResource[] libraryVariableSets)
        {
            var tenant = tenantEditor.Instance;
            var projectLookup = projects.ToDictionary(p => p.Id);
            var libraryVariableSetLookup = libraryVariableSets.ToDictionary(l => l.Id);
            var environmentLookup = environments.ToDictionary(e => e.Id);

            var tenantVariables = tenantEditor.Variables.Instance;

            // Library variables
            foreach (var libraryVariable in tenantVariables.LibraryVariables)
            {
                foreach (var template in libraryVariableSetLookup[libraryVariable.Value.LibraryVariableSetId].Templates)
                {
                    var value = TryFillLibraryVariableByConvention(template, tenant);
                    if (value != null)
                    {
                        libraryVariable.Value.Variables[template.Id] = value;
                    }
                }
            }

            // Project variables
            foreach (var projectVariable in tenantVariables.ProjectVariables)
            {
                foreach (var template in projectLookup[projectVariable.Value.ProjectId].Templates)
                {
                    foreach (var connectedEnvironmentId in tenant.ProjectEnvironments[projectVariable.Value.ProjectId])
                    {
                        var environment = environmentLookup[connectedEnvironmentId];
                        var value = TryFillProjectVariableByConvention(template, tenant, environment);
                        if (value != null)
                        {
                            projectVariable.Value.Variables[connectedEnvironmentId][template.Id] = value;
                        }
                    }
                }
            }
        }

        private PropertyValueResource TryFillLibraryVariableByConvention(ActionTemplateParameterResource template, TenantResource tenant)
        {
            if (template.Name == VariableKeys.StandardTenantDetails.TruckAlias) return new PropertyValueResource(tenant.Name.Replace(" ", "-").ToLowerInvariant());

            return null;
        }

        private PropertyValueResource TryFillProjectVariableByConvention(ActionTemplateParameterResource template, TenantResource tenant, EnvironmentResource environment)
        {
            return null;
        }
    }
}