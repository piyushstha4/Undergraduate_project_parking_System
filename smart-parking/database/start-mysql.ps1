# Starts the project's MySQL 8 server on port 3307.
# Uses database/mysql-data and does not touch the Windows MySQL80 service on port 3306.
$basedir = "C:\Program Files\MySQL\MySQL Server 8.0"
$datadir = Join-Path $PSScriptRoot "mysql-data"
$mysqld = Join-Path $basedir "bin\mysqld.exe"

if (-not (Test-Path $mysqld)) {
    Write-Error "MySQL was not found at $mysqld"
    exit 1
}

& $mysqld --no-defaults --basedir="$basedir" --datadir="$datadir" --port=3307 --bind-address=127.0.0.1 --mysqlx=0 --console
