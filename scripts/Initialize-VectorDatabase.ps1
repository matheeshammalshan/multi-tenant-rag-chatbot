# Run after starting Compose and pulling nomic-embed-text.
$ErrorActionPreference = 'Stop'
$collectionUrl = 'http://localhost:6333/collections/rag-pipeline'

$embeddingRequest = @{ model = 'nomic-embed-text'; prompt = 'Initialize chatbot vector database' } | ConvertTo-Json
$embeddingResponse = Invoke-RestMethod -Uri 'http://localhost:11434/api/embeddings' -Method Post -ContentType 'application/json' -Body $embeddingRequest -TimeoutSec 180
if ($null -eq $embeddingResponse.embedding -or @($embeddingResponse.embedding).Count -eq 0) {
    throw 'Ollama returned no embedding. Pull nomic-embed-text and try again.'
}
$dimension = @($embeddingResponse.embedding).Count

$existing = $null
try {
    $existing = Invoke-RestMethod -Uri $collectionUrl -Method Get -TimeoutSec 15
}
catch {
    if ($null -eq $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 404) { throw }
}

if ($null -ne $existing) {
    if ($existing.result.config.params.vectors.size -ne $dimension) {
        throw "The existing rag-pipeline collection does not match the model's $dimension dimensions. Use a matching embedding model; this script preserves existing data."
    }
    Write-Host "rag-pipeline already exists with $dimension dimensions."
}
else {
    $collectionRequest = @{ vectors = @{ size = $dimension; distance = 'Cosine' } } | ConvertTo-Json -Depth 4
    Invoke-RestMethod -Uri $collectionUrl -Method Put -ContentType 'application/json' -Body $collectionRequest -TimeoutSec 30 | Out-Null
    Write-Host "Created rag-pipeline with $dimension dimensions."
}
