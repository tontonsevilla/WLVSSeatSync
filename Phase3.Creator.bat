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
dotnet new web -n %projectName%.FakePayments -o src/%projectName%.FakePayments
dotnet sln add src/%projectName%.FakePayments
dotnet add src/%projectName%.FakePayments reference src/%projectName%.ServiceDefaults
dotnet add src/%projectName%.AppHost reference src/%projectName%.FakePayments

echo.
echo Successfully created the project and referenced the packages to %projectName% projects.

:: Pause the script so the window stays open
pause