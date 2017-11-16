& ".paket\paket.exe" "restore"

if ($LASTEXITCODE -eq 1) {
    exit $?
}

& "packages\build\FAKE\tools\FAKE.exe" "build.fsx" $args