$projectName = $OctopusParameters["Octopus.Project.Name"]
$environmentName = $OctopusParameters["Octopus.Environment.Name"]

Write-Host "Deploying $projectName into $environmentName"
Write-Host "I'm going to sleep for a second..."
Start-Sleep -s 1
Write-Host "All done."