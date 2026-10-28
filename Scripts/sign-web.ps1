param([string]$DllPath = "")
$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($DllPath)) {
    $DllPath = Join-Path $PSScriptRoot "..\bin\multicanalprueba.dll"
}
$dll = Resolve-Path $DllPath
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq "CN=MulticanalDev" } | Select-Object -First 1
if (-not $cert) {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=MulticanalDev" -CertStoreLocation Cert:\CurrentUser\My -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(2)
    foreach ($storeName in @("TrustedPublisher", "Root")) {
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($storeName, "CurrentUser")
        $store.Open("ReadWrite"); $store.Add($cert); $store.Close()
    }
}
$sig = Set-AuthenticodeSignature -FilePath $dll -Certificate $cert
Write-Host "Firma: $($sig.Status) -> $dll"
