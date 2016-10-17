$projectName = $OctopusParameters["Octopus.Project.Name"]
$tenantName = $OctopusParameters["Octopus.Deployment.Tenant.Name"]
$contactEmail = $OctopusParameters["Tenant.ContactEmail"]
$environmentName = $OctopusParameters["Octopus.Environment.Name"]

if ($tenantName) {
    Write-Host "Email to $contactEmail - Hi $tenantName, just wanted to let you know we've upgraded $projectName in your $environmentName environment."
}