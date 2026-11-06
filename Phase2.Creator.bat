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
:: Application: CQRS, validation, and EF Core for queries
dotnet add src/%projectName%.Application package MediatR --version 12.5.0
dotnet add src/%projectName%.Application package FluentValidation
dotnet add src/%projectName%.Application package FluentValidation.DependencyInjectionExtensions
dotnet add src/%projectName%.Application package Microsoft.EntityFrameworkCore

:: Infrastructure: user accounts stored with EF Core
dotnet add src/%projectName%.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore

:: Api: logging, JWT, API reference page
dotnet add src/%projectName%.Api package Serilog.AspNetCore
dotnet add src/%projectName%.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/%projectName%.Api package Scalar.AspNetCore

:: Integration tests
dotnet add tests/%projectName%.Api.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/%projectName%.Api.IntegrationTests package Testcontainers.PostgreSql
dotnet add tests/%projectName%.Api.IntegrationTests package Shouldly

echo.
echo Successfully reference the packages to %projectName% projects.

:: Pause the script so the window stays open
pause