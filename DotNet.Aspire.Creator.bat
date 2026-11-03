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
:: Creation of Aspire project.
:: 1. Create the two Aspire projects
dotnet new aspire-apphost         -n %projectName%.AppHost         -o src/%projectName%.AppHost
dotnet new aspire-servicedefaults -n %projectName%.ServiceDefaults -o src/%projectName%.ServiceDefaults
dotnet sln add src/%projectName%.AppHost src/%projectName%.ServiceDefaults

:: Connect the projects
:: 2. The AppHost needs to know about every service it starts, and every service uses ServiceDefaults
dotnet add src/%projectName%.AppHost reference src/%projectName%.Api src/%projectName%.Notifications src/%projectName%.Gateway src/%projectName%.Web src/%projectName%.Dashboard

dotnet add src/%projectName%.Api           reference src/%projectName%.ServiceDefaults
dotnet add src/%projectName%.Notifications reference src/%projectName%.ServiceDefaults
dotnet add src/%projectName%.Gateway       reference src/%projectName%.ServiceDefaults
dotnet add src/%projectName%.Web           reference src/%projectName%.ServiceDefaults
dotnet add src/%projectName%.Dashboard     reference src/%projectName%.ServiceDefaults 

:: 3. Add the container integrations to the AppHost
dotnet add src/%projectName%.AppHost package Aspire.Hosting.PostgreSQL
dotnet add src/%projectName%.AppHost package Aspire.Hosting.Redis
dotnet add src/%projectName%.AppHost package Aspire.Hosting.RabbitMQ

echo.
echo Successfully created %projectName% Aspire project.

:: Pause the script so the window stays open
pause