Write-Host "✅ Starting Docker Compose..."
docker compose up -d

Write-Host "✅ Waiting for Docker services to become ready..."
while (!(docker ps)) {
    Start-Sleep -Seconds 2
}

Write-Host "✅ Running database migrations..."
dotnet ef database update --startup-project .\Portfolio.Grpc\

# todo: These do not work. Why? 
Write-Host "✅ Opening web pages..."
Start-Process "http://localhost:4000/"
Start-Process "http://localhost:8090/"
