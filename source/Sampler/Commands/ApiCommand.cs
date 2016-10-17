using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Octopus.Client;
using Octopus.Client.Model;
using Octopus.Sampler.Infrastructure;
using Octopus.Sampler.Integration;
using Serilog;

namespace Octopus.Sampler.Commands
{
    public abstract class ApiCommand : ICommand
    {
        readonly ILogger log = Log.ForContext<ApiCommand>();
        readonly IOctopusRepositoryFactory repositoryFactory;
        string apiKey;
        bool ignoreSslErrors;
        string password;
#pragma warning disable 618
        IOctopusRepository repository;
#pragma warning restore 618
        string serverBaseUrl;
        string username;
        readonly Options optionGroups = new Options();

        protected ApiCommand(IOctopusRepositoryFactory repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;

            var options = optionGroups.For("Common options");
            options.Add("server=", "The base URL for your Octopus server - e.g., http://your-octopus/", v => serverBaseUrl = v);
            options.Add("apiKey=", "Your API key. Get this from the user profile page.", v => apiKey = v);
            options.Add("user=", "[Optional] Username to use when authenticating with the server.", v => username = v);
            options.Add("pass=", "[Optional] Password to use when authenticating with the server.", v => password = v);
            options.Add("ignoreSslErrors", "[Optional] Set this flag if your Octopus server uses HTTPS but the certificate is not trusted on this machine. Any certificate errors will be ignored. WARNING: this option may create a security vulnerability.", v => ignoreSslErrors = true);
        }

        protected Options Options => optionGroups;

        protected string ServerBaseUrl => serverBaseUrl;

#pragma warning disable 618
        protected IOctopusRepository Repository => repository;
#pragma warning restore 618

        public void GetHelp(TextWriter writer)
        {
            optionGroups.WriteOptionDescriptions(writer);
        }

        public void Execute(string[] commandLineArguments)
        {
            var remainingArguments = optionGroups.Parse(commandLineArguments);
            if (remainingArguments.Count > 0)
                throw new CommandException("Unrecognized command arguments: " + string.Join(", ", remainingArguments));

            if (string.IsNullOrWhiteSpace(serverBaseUrl))
                throw new CommandException("Please specify the Octopus Server URL using --server=http://your-server/");

            if (string.IsNullOrWhiteSpace(apiKey) && (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)))
                throw new CommandException("Please specify either your API key using --apiKey=ABCDEF123456789, or your Octopus credentials using --user=MyName --pass=Secret.");

            var credentials = ParseCredentials(username, password);

            var endpoint = new OctopusServerEndpoint(serverBaseUrl, apiKey, credentials);

            repository = repositoryFactory.CreateRepository(endpoint);

            repository.Client.SendingOctopusRequest += request => log.Debug("{Method} {Uri}", request.Method, request.Uri);

            ConfigureServerCertificateValidation();

            InitializeConnection();

            Execute();
        }

        private void InitializeConnection()
        {
            log.Information("Handshaking with Octopus server {ServerBaseUrl}", serverBaseUrl);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                log.Information("Signing in using basic authentication as {Username}", username);
                repository.Users.SignIn(new LoginCommand
                {
                    Username = username,
                    Password = password
                });
            }

            var root = repository.Client.RootDocument;
            log.Information("Handshake successful. Octopus version: {ServerVersion}; API version: {APIVersion}", root.Version, root.ApiVersion);

            var user = repository.Users.GetCurrent();
            if (user != null)
            {
                log.Information("Authenticated as: {Name} <{Email}> {Type}", user.DisplayName, user.EmailAddress, user.IsService ? "(a service account)" : "(a user account)");
            }
        }

        private void ConfigureServerCertificateValidation()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                {
                    return true;
                }

                var certificate2 = (X509Certificate2) certificate;
                var warning = "The following certificate errors were encountered when establishing the HTTPS connection to the server: " + errors + Environment.NewLine +
                    "Certificate subject name: " + certificate2.SubjectName.Name + Environment.NewLine +
                    "Certificate thumbprint:   " + ((X509Certificate2) certificate).Thumbprint;

                if (ignoreSslErrors)
                {
                    log.Warning(warning);
                    log.Warning("Because --ignoreSslErrors was set, this will be ignored.");
                    return true;
                }

                log.Error(warning);
                return false;
            };
        }

        protected abstract void Execute();

        static NetworkCredential ParseCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                return CredentialCache.DefaultNetworkCredentials;

            var split = username.Split('\\');
            if (split.Length == 2)
            {
                var domain = split.First();
                username = split.Last();

                return new NetworkCredential(username, password, domain);
            }

            return new NetworkCredential(username, password);
        }
    }
}
