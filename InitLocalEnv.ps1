function Write-Message {
  param([string]$Message)
  $Timestamp = [System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
  Write-Host "[$Timestamp] $Message"
}

Write-Message "Installing Azurite docker image..."

$Image = "mcr.microsoft.com/azure-storage/azurite"
$Name = "azurite"

Invoke-Expression "docker pull $Image"

if ((Invoke-Expression "docker ps --filter 'name=$Name'").Where({ $_.Contains($Image) }).Count -gt 0) {
  Write-Message "Azurite in running"
}
else {
  Write-Message "Running Azurite..."
  Invoke-Expression "docker run -d --name $Name -p 10000:10000 -p 10001:10001 -p 10002:10002 $Image"
}
