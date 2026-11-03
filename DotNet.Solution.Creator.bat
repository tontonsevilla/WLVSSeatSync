@echo off
:input_loop

:: Clear any previous value from the variable
set "projectName="

:: Ask the user for input and store it in a variable named "projectName"
set /p projectName="Provide a project name (without spaces): "

if "%projectName%"=="" goto :invalid_project_name
	
if not "%projectName%"=="%projectName: =%" goto :invalid_project_name
	
goto :valid_project_name

:invalid_project_name
cls
echo Error: Project name cannot be empty or just spaces!
echo.
goto :input_loop

:valid_project_name
:: Creation of Solution file.
dotnet new sln -n %projectName%

:: Creation of main Project files.
dotnet new classlib -n %projectName%.Domain         -o src/%projectName%.Domain
dotnet new classlib -n %projectName%.Application    -o src/%projectName%.Application
dotnet new classlib -n %projectName%.Infrastructure -o src/%projectName%.Infrastructure
dotnet new classlib -n %projectName%.Client         -o src/%projectName%.Client
dotnet new webapi   -n %projectName%.Api            -o src/%projectName%.Api
dotnet new worker   -n %projectName%.Notifications  -o src/%projectName%.Notifications
dotnet new web      -n %projectName%.Gateway        -o src/%projectName%.Gateway
dotnet new webapp   -n %projectName%.Web            -o src/%projectName%.Web
dotnet new blazor   -n %projectName%.Dashboard      -o src/%projectName%.Dashboard

:: Creation of Test Project files.
dotnet new xunit -n %projectName%.Domain.Tests          -o tests/%projectName%.Domain.Tests
dotnet new xunit -n %projectName%.Application.Tests     -o tests/%projectName%.Application.Tests
dotnet new xunit -n %projectName%.Api.IntegrationTests  -o tests/%projectName%.Api.IntegrationTests

:: Delete Class1.cs file.
cd /d "%~dp0"
del /f /s /q "class1.cs"

:: Add every project to the Solution.
for /r %%i in (*.csproj) do dotnet sln add "%%i"

:: Reference some project/s to other project
dotnet add src/%projectName%.Application    reference src/%projectName%.Domain
dotnet add src/%projectName%.Infrastructure reference src/%projectName%.Application
dotnet add src/%projectName%.Api            reference src/%projectName%.Application src/%projectName%.Infrastructure
dotnet add src/%projectName%.Web            reference src/%projectName%.Client
dotnet add src/%projectName%.Dashboard      reference src/%projectName%.Client

dotnet add tests/%projectName%.Domain.Tests         reference src/%projectName%.Domain
dotnet add tests/%projectName%.Application.Tests    reference src/%projectName%.Application
dotnet add tests/%projectName%.Api.IntegrationTests reference src/%projectName%.Api

echo.
echo Successfully created %projectName% .NET project solution.

:: Pause the script so the window stays open
pause
