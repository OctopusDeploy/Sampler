$projectName = $OctopusParameters["Octopus.Project.Name"]
$tenantName = $OctopusParameters["Octopus.Deployment.Tenant.Name"]
$environmentName = $OctopusParameters["Octopus.Environment.Name"]
$databaseConnectionString = $OctopusParameters["DatabaseConnectionString"]
$hostURL = $OctopusParameters["HostUrl"]
 
if ($tenantName) {
    Write-Host "Deploying the Beta component for $projectName into the $environmentName environment for $tenantName"
} else {
    Write-Host "Deploying the Beta component for $projectName into $environmentName"
}
Write-Host "Database Connection String: $databaseConnectionString"
Write-Host "URL: $hostURL"