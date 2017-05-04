using System;
using System.Linq;
using System.Threading.Tasks;
using NLipsum.Core;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Client.Model.Accounts;
using Octopus.Sampler.Infrastructure;
using Serilog;

namespace Octopus.Sampler.Commands
{
    [Command("azure-cloudservice-sample", Description = "Applies the Azure Cloud Service sample.")]
    public class AzureCloudServiceSampleCommand : ApiCommand
    {
        private static readonly Guid DefaultSubscriptionId = Guid.Parse("95bf77d2-64b1-4ed2-9de1-b5451e3881f5");
        private static readonly string DefaultStorageAccountName = "octopusdevplayground";
        private static readonly string DefaultLocation = "West US";
        private static readonly LipsumGenerator LipsumOmagyar = new LipsumGenerator(Lipsums.Omagyar, isXml: false);

        private static readonly ILogger Log = Serilog.Log.ForContext<AzureCloudServiceSampleCommand>();

        public AzureCloudServiceSampleCommand(IOctopusClientFactory octopusClientFactory)
            : base(octopusClientFactory)
        {
            var options = Options.For("Azure Cloud Service sample");
            options.Add("subscription=", $"The Azure Subscription Id to use for this project, default {DefaultSubscriptionId}", v => subscriptionId = Guid.Parse(v));
            options.Add("storage=", $"The name of the Azure Storage Account to use for this project, default {DefaultStorageAccountName}", v => storageAccountName = v);
            options.Add("location=", $"The Azure Location to use for this project, default {DefaultLocation}", v => location = v);
        }

        Guid subscriptionId = DefaultSubscriptionId;
        string storageAccountName = DefaultStorageAccountName;
        string location = DefaultLocation;

        protected override async Task Execute()
        {
            Log.Information("Building Azure Cloud Service sample...");

            var environment = await Repository.Environments.CreateOrModify("Azure Production", LipsumOmagyar.GenerateLipsum(1));
            var normalLifecycle = await Repository.Lifecycles.CreateOrModify("Azure Lifecycle", "The normal lifecycle for the Azure sample");
            await normalLifecycle.AsSimplePromotionLifecycle(new[] { environment.Instance }).Save();
            var projectGroup = await Repository.ProjectGroups.CreateOrModify("Azure Cloud Service Sample");

            var account = await Repository.Accounts.Create(new AzureSubscriptionAccountResource
            {
                Name = "AzureCloudServiceSampleAccount",
                Description = "Azure Management Certificate account used by the Azure Cloud Service Sample",
                SubscriptionNumber = subscriptionId.ToString()
            });

            await BuildProject(projectGroup.Instance, normalLifecycle.Instance, account);
        }

        private async Task BuildProject(
            ProjectGroupResource projectGroup,
            LifecycleResource normalLifecycle,
            AccountResource account)
        {
            var projectEditor = await Repository.Projects.CreateOrModify("Azure Cloud Service Sample", projectGroup, normalLifecycle);
            await projectEditor.SetLogo(SampleImageCache.DownloadImage("https://azurecomcdn.azureedge.net/cvt-9c42e10c78bceeb8622e49af8d0fe1a20cd9ca9f4983c398d0b356cf822d8844/images/shared/social/azure-icon-250x250.png"));

            var variableEditor = await projectEditor.Variables;
            if (variableEditor.Instance.Variables.All(v => v.Name != "UniqueName"))
            {
                variableEditor.AddOrUpdateVariableValue("UniqueName", Guid.NewGuid().ToString("N"));
            }
            variableEditor
                .AddOrUpdateVariableValue("CloudService", "AzureCloudService#{UniqueName}")
                .AddOrUpdateVariableValue("StorageAccount", storageAccountName)
                .AddOrUpdateVariableValue("Location", location);

            var processEditor = await projectEditor.DeploymentProcess;
            var process = processEditor.Instance;

            process.Steps.Add(new DeploymentStepResource
            {
                Name = "Create Azure Cloud Service",
                Condition = DeploymentStepCondition.Success,
                RequiresPackagesToBeAcquired = false,
                Actions =
                {
                    new DeploymentActionResource
                    {
                        ActionType = "Octopus.AzurePowerShell",
                        Name = "Create Azure Cloud Service",
                        Properties =
                        {
                            {"Octopus.Action.Script.ScriptBody", "New-AzureService -ServiceName #{CloudService} -Location \"#{Location}\""},
                            {"Octopus.Action.Azure.AccountId", account.Id}
                        }
                    }
                }
            });

            process.Steps.Add(new DeploymentStepResource
            {
                Name = "Deploy Azure Cloud Service",
                Condition = DeploymentStepCondition.Success,
                RequiresPackagesToBeAcquired = true,
                Actions =
                {
                    new DeploymentActionResource
                    {
                        ActionType = "Octopus.AzureCloudService",
                        Name = "Deploy Azure Cloud Service",
                        Properties =
                        {
                            {"Octopus.Action.Azure.AccountId", account.Id},
                            {"Octopus.Action.Azure.CloudServiceName", "#{CloudService}"},
                            {"Octopus.Action.Azure.StorageAccountName", "#{StorageAccount}"},
                            {"Octopus.Action.Azure.Slot", "Staging"},
                            {"Octopus.Action.Azure.SwapIfPossible", "False"},
                            {"Octopus.Action.Azure.UseCurrentInstanceCount", "False"},
                            {"Octopus.Action.Package.PackageId", "Octopus.Sample.AzureCloudService"},
                            {"Octopus.Action.Package.FeedId", "feeds-builtin"}
                        }
                    }
                }
            });

            process.Steps.Add(new DeploymentStepResource
            {
                Name = "Remove Azure Cloud Service",
                Condition = DeploymentStepCondition.Always,
                RequiresPackagesToBeAcquired = false,
                Actions =
                {
                    new DeploymentActionResource
                    {
                        ActionType = "Octopus.AzurePowerShell",
                        Name = "Remove Azure Cloud Service",
                        Properties =
                        {
                            {"Octopus.Action.Script.ScriptBody", "Remove-AzureService -ServiceName #{CloudService} -Force"},
                            {"Octopus.Action.Azure.AccountId", account.Id}
                        }
                    }
                }
            });


            await projectEditor.Save();
        }
    }
}