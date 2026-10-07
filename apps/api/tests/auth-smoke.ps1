param(
    [string]$BaseUrl = 'http://localhost:5248',
    [Parameter(Mandatory = $true)][string]$DatabaseName
)
$ErrorActionPreference = 'Stop'
if ($DatabaseName -notmatch '^workly_auth_test_[a-f0-9]+$') {
    throw 'This script requires an isolated workly_auth_test_* database.'
}

$cookieUri = [Uri]"$BaseUrl/api/auth/"

function New-Client([string]$RefreshToken = '') {
    $null = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing -SessionVariable session
    if ($RefreshToken) {
        $cookie = [System.Net.Cookie]::new('workly_refresh', $RefreshToken, '/api/auth', $cookieUri.Host)
        $session.Cookies.Add($cookieUri, $cookie)
    }
    return $session
}

function Get-RefreshToken($Session) {
    return $Session.Cookies.GetCookies($cookieUri)['workly_refresh'].Value
}

function Send-Request([string]$Method, [string]$Path, $Body, [int]$Expected,
    [string]$AccessToken = '', $Session = $null) {
    $parameters = @{ Uri = "$BaseUrl/api/auth/$Path"; Method = $Method; UseBasicParsing = $true }
    if ($null -ne $Body) {
        $parameters.Body = ConvertTo-Json $Body -Compress
        $parameters.ContentType = 'application/json'
    }
    if ($AccessToken) { $parameters.Headers = @{ Authorization = "Bearer $AccessToken" } }
    if ($null -ne $Session) { $parameters.WebSession = $Session }
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
$client = New-Client
$registered = Send-Request POST register @{ email = $email; password = $password; displayName = 'Auth test' } 201 '' $client
$registeredToken = Get-RefreshToken $client
if (!$registeredToken) { throw 'Register did not set the refresh cookie' }
if ($registered.PSObject.Properties.Name -contains 'refreshToken') { throw 'Refresh token leaked in JSON' }
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

$registeredExpiry = $client.Cookies.GetCookies($cookieUri)['workly_refresh'].Expires
$rotated = Send-Request POST refresh $null 200 '' $client
$rotatedToken = Get-RefreshToken $client
if ($rotatedToken -eq $registeredToken) { throw 'Token did not rotate' }
$rotatedExpiry = $client.Cookies.GetCookies($cookieUri)['workly_refresh'].Expires
if ([Math]::Abs(($rotatedExpiry - $registeredExpiry).TotalSeconds) -gt 1) { throw 'Session expiry was extended' }
$replayClient = New-Client $registeredToken
$null = Send-Request POST refresh $null 401 '' $replayClient
$null = Send-Request POST refresh $null 401 '' $client

$loginClient = New-Client
$login = Send-Request POST login @{ email = $email; password = $password } 200 '' $loginClient
$loginToken = Get-RefreshToken $loginClient
$null = Send-Request POST logout $null 204 '' $loginClient
$null = Send-Request POST logout $null 204 '' $loginClient
$null = Send-Request POST refresh $null 401 '' $loginClient
$null = Send-Request POST refresh $null 401 '' (New-Client 'unknown-token')

# Logout with a pre-rotation token must revoke its replacement too.
$sessionClient = New-Client
$null = Send-Request POST login @{ email = $email; password = $password } 200 '' $sessionClient
$originalToken = Get-RefreshToken $sessionClient
$null = Send-Request POST refresh $null 200 '' $sessionClient
$null = Send-Request POST logout $null 204 '' (New-Client $originalToken)
$null = Send-Request POST refresh $null 401 '' $sessionClient

# Two clients refreshing the same token must not create two valid sessions.
$concurrentClient = New-Client
$null = Send-Request POST login @{ email = $email; password = $password } 200 '' $concurrentClient
$concurrentToken = Get-RefreshToken $concurrentClient
$jobs = @()
try {
    foreach ($index in 1..2) {
        $jobs += Start-Job -ArgumentList $BaseUrl, $concurrentToken -ScriptBlock {
            param($url, $token)
            try {
                $null = Invoke-WebRequest -UseBasicParsing -Uri "$url/health" -SessionVariable session
                $cookieUri = [Uri]"$url/api/auth/"
                $cookie = [System.Net.Cookie]::new('workly_refresh', $token, '/api/auth', $cookieUri.Host)
                $session.Cookies.Add($cookieUri, $cookie)
                $result = Invoke-WebRequest -UseBasicParsing -Method POST -Uri "$url/api/auth/refresh" -WebSession $session
                $replacement = [regex]::Match($result.Headers['Set-Cookie'], 'workly_refresh=([^;]+)').Groups[1].Value
                @{ Status = [int]$result.StatusCode; RefreshToken = $replacement }
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
    $null = Send-Request POST refresh $null 401 '' (New-Client $winner.RefreshToken)
    Write-Output 'PASS: concurrent refresh allows one rotation, then revokes on replay'
} finally {
    $jobs | Remove-Job -Force
}

# Check persisted secrets without printing password hashes or raw tokens.
$userId = [Guid]::Parse($registered.user.id).ToString()
$expiryClient = New-Client
$null = Send-Request POST login @{ email = $email; password = $password } 200 '' $expiryClient
$expiryToken = Get-RefreshToken $expiryClient
"UPDATE refresh_tokens SET expires_at = now() - interval '1 minute' WHERE user_id = '$userId' AND revoked_at IS NULL;" | docker exec -i workly-postgres psql -U workly -d $DatabaseName -v ON_ERROR_STOP=1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Expiry setup failed' }
$null = Send-Request POST refresh $null 401 '' $expiryClient
$sql = "SELECT json_build_object('password_hash', password_hash, 'token_hashes', (SELECT json_agg(token_hash) FROM refresh_tokens WHERE user_id = users.id)) FROM users WHERE id = '$userId';"
$storedJson = $sql | docker exec -i workly-postgres psql -U workly -d $DatabaseName -t -A -v ON_ERROR_STOP=1
if ($LASTEXITCODE -ne 0) { throw 'Database check failed' }
$stored = ConvertFrom-Json ($storedJson -join '')
if ($stored.password_hash -eq $password -or !$stored.password_hash) { throw 'Password was not hashed' }
foreach ($hash in $stored.token_hashes) {
    if ($hash -notmatch '^[A-F0-9]{64}$') { throw 'Unexpected persisted refresh token hash' }
    if ($hash -in @($registeredToken, $rotatedToken, $loginToken, $expiryToken)) { throw 'Raw refresh token was stored' }
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
