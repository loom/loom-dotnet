function Write-Message {
  param([string]$Message)
  $Timestamp = [System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
  Write-Host "[$Timestamp] $Message"
}

function ContainerExists {
  param([string]$Image, [string]$Name)
  $Command = "docker ps --all --filter 'name=$Name'"
  return (Invoke-Expression $Command).Where({ $_.Contains($Image) }).Count -gt 0
}

function IsContainerRunning {
  param([string]$Image, [string]$Name)
  $Command = "docker ps --filter 'name=$Name'"
  return (Invoke-Expression $Command).Where({ $_.Contains($Image) }).Count -gt 0
}

$Image = "mcr.microsoft.com/azure-storage/azurite"
$Name = "azurite"

Write-Message "Installing Azurite docker image..."
Invoke-Expression "docker pull $Image"

if (ContainerExists $Image $Name) {
  if (-Not (IsContainerRunning $Image $Name)) {
    Write-Message "Starting Azurite..."
    Invoke-Expression "docker start $Name"
  }
}
else {
  Write-Message "Running Azurite..."
  Invoke-Expression "docker run -d --name $Name -p 10000:10000 -p 10001:10001 -p 10002:10002 $Image"
}
Write-Message "Azurite in running"
