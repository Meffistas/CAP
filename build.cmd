SET projectFolder=DotNetCore.CAP.RabbitMQ.ESCID
SET rabbitMqEscid=DotNetCore.CAP.RabbitMQ.ESCID.csproj
SET PackageOutput=[System.IO.Path]::GetFullPath("$PSScriptRoot\..\nupkg"

pushd src
pushd %projectFolder%

SET version=%1
IF "%version%"=="" SET version=1.0.0-preview

dotnet restore %rabbitMqEscid%
if %errorlevel% neq 0 exit /b %errorlevel% 

popd

dotnet pack %projectFolder% --configuration Release --output %cd%%PackageOutput% /p:Version=%version% /p:AssemblyVersion=1.0.0.0 --no-restore
if %errorlevel% neq 0 exit /b %errorlevel%

popd