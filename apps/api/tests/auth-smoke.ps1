param(
    [string]$BaseUrl = 'http://localhost:5248',
    [Parameter(Mandatory = $true)][string]$DatabaseName
)
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^workly_auth_test_[a-f0-9]+$') {
    throw 'This script requires an isolated workly_auth_test_* database.'
}

function Send-Request([string]$Method, [string]$Path, $Body, [int]$Expected, [string]$AccessToken = '') {
    $parameters = @{ Uri = "$BaseUrl/api/auth/$Path"; Method = $Method; UseBasicParsing = $true }
    if ($null -ne $Body) {
        $parameters.Body = ConvertTo-Json $Body -Compress
        $parameters.ContentType = 'application/json'
    }
    if ($AccessToken) { $parameters.Headers = @{ Authorization = "Bearer $AccessToken" } }
    try {
        $response = Invoke-WebRequest @parameters
        $status = [int]$response.StatusCode
        $content = $response.Content
    } catch {
        if ($null -eq $_.Exception.Response) { throw }
        $status = [int]$_.Exception.Response.StatusCode
        $content = $_.ErrorDetails.Message
    }
    if ($status -ne $Expected) { throw "$Method $Path expected $Expected, got $status" }
    Write-Output "PASS: $Method $Path -> $status" | Out-Host
    if ($content) { return ConvertFrom-Json $content }
}

$email = "auth-$([Guid]::NewGuid().ToString('N'))@example.invalid"
$password = 'Learning-backend-password-42'
$registered = Send-Request POST register @{ email = $email; password = $password; displayName = 'Auth test' } 201
if ($registered.user.PSObject.Properties.Name -contains 'passwordHash') { throw 'Password hash leaked' }
$null = Send-Request POST register @{ email = $email.ToUpperInvariant(); password = $password; displayName = 'Duplicate' } 409
$null = Send-Request POST register @{ email = 'invalid'; password = 'short'; displayName = '' } 400
$null = Send-Request POST login @{ email = $email; password = 'incorrect-password' } 401
$null = Send-Request GET me $null 401
$me = Send-Request GET me $null 200 $registered.accessToken
if ($me.id -ne $registered.user.id) { throw 'Wrong authenticated user' }
$parts = $registered.accessToken.Split('.')
$parts[2] = $(if ($parts[2][0] -eq 'A') { 'B' } else { 'A' }) + $parts[2].Substring(1)
$null = Send-Request GET me $null 401 ($parts -join '.')

$rotated = Send-Request POST refresh @{ refreshToken = $registered.refreshToken } 200
if ($rotated.refreshToken -eq $registered.refreshToken) { throw 'Token did not rotate' }
$expiryDifference = ([DateTimeOffset]::Parse($rotated.refreshTokenExpiresAt) - [DateTimeOffset]::Parse($registered.refreshTokenExpiresAt)).TotalMilliseconds
if ([Math]::Abs($expiryDifference) -gt 1) { throw 'Session expiry was extended' }
$null = Send-Request POST refresh @{ refreshToken = $registered.refreshToken } 401
$null = Send-Request POST refresh @{ refreshToken = $rotated.refreshToken } 401

$login = Send-Request POST login @{ email = $email; password = $password } 200
$null = Send-Request POST logout @{ refreshToken = $login.refreshToken } 204
$null = Send-Request POST logout @{ refreshToken = $login.refreshToken } 204
$null = Send-Request POST refresh @{ refreshToken = $login.refreshToken } 401
$null = Send-Request POST refresh @{ refreshToken = 'unknown-token' } 401

# Logout with a pre-rotation token must revoke its replacement too.
$session = Send-Request POST login @{ email = $email; password = $password } 200
$replacement = Send-Request POST refresh @{ refreshToken = $session.refreshToken } 200
$null = Send-Request POST logout @{ refreshToken = $session.refreshToken } 204
$null = Send-Request POST refresh @{ refreshToken = $replacement.refreshToken } 401

# Two clients refreshing the same token must not create two valid sessions.
$session = Send-Request POST login @{ email = $email; password = $password } 200
$jobs = @()
try {
    foreach ($index in 1..2) {
        $jobs += Start-Job -ArgumentList $BaseUrl, $session.refreshToken -ScriptBlock {
            param($url, $token)
            try {
                $result = Invoke-WebRequest -UseBasicParsing -Method POST -Uri "$url/api/auth/refresh" -ContentType 'application/json' -Body (ConvertTo-Json @{ refreshToken = $token })
                @{ Status = [int]$result.StatusCode; Body = ConvertFrom-Json $result.Content }
            } catch {
                if ($null -eq $_.Exception.Response) { throw }
                @{ Status = [int]$_.Exception.Response.StatusCode }
            }
        }
    }
    $results = @($jobs | Wait-Job | Receive-Job -ErrorAction Stop)
    $statuses = @($results | ForEach-Object { $_.Status } | Sort-Object)
    if (($statuses -join ',') -ne '200,401') { throw "Concurrent refresh statuses: $statuses" }
    $winner = $results | Where-Object { $_.Status -eq 200 }
    $null = Send-Request POST refresh @{ refreshToken = $winner.Body.refreshToken } 401
    Write-Output 'PASS: concurrent refresh allows one rotation, then revokes on replay'
} finally {
    $jobs | Remove-Job -Force
}

# Check persisted secrets without printing password hashes or raw tokens.
$userId = [Guid]::Parse($registered.user.id).ToString()
$session = Send-Request POST login @{ email = $email; password = $password } 200
"UPDATE refresh_tokens SET expires_at = now() - interval '1 minute' WHERE user_id = '$userId' AND revoked_at IS NULL;" | docker exec -i workly-postgres psql -U workly -d $DatabaseName -v ON_ERROR_STOP=1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Expiry setup failed' }
$null = Send-Request POST refresh @{ refreshToken = $session.refreshToken } 401
$sql = "SELECT json_build_object('password_hash', password_hash, 'token_hashes', (SELECT json_agg(token_hash) FROM refresh_tokens WHERE user_id = users.id)) FROM users WHERE id = '$userId';"
$storedJson = $sql | docker exec -i workly-postgres psql -U workly -d $DatabaseName -t -A -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) { throw 'Database check failed' }
$stored = ConvertFrom-Json ($storedJson -join '')
if ($stored.password_hash -eq $password -or !$stored.password_hash) { throw 'Password was not hashed' }
foreach ($hash in $stored.token_hashes) {
    if ($hash -notmatch '^[A-F0-9]{64}$') { throw 'Unexpected persisted refresh token hash' }
    if ($hash -eq $registered.refreshToken -or $hash -eq $rotated.refreshToken -or $hash -eq $login.refreshToken) {
        throw 'Raw refresh token was stored'
    }
}
Write-Output 'PASS: passwords and refresh tokens are stored as hashes'
$rateLimited = $false
foreach ($index in 1..31) {
    try {
        $null = Invoke-WebRequest -UseBasicParsing -Uri "$BaseUrl/api/auth/me"
    } catch {
        if ([int]$_.Exception.Response.StatusCode -eq 429) { $rateLimited = $true; break }
        if ([int]$_.Exception.Response.StatusCode -ne 401) { throw }
    }
}
if (!$rateLimited) { throw 'Auth rate limiter did not reject excess requests' }
Write-Output 'PASS: excess auth requests return 429'
Write-Output 'Auth smoke tests passed.'
