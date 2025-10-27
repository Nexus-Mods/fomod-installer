@echo off
REM Installation script for meta package on Windows
REM Sets FOMOD_METAPACKAGE_INSTALL=1 to skip child package builds

echo Installing meta package with FOMOD_METAPACKAGE_INSTALL=1
echo Child packages will skip their build steps
echo.

set FOMOD_METAPACKAGE_INSTALL=1
yarn install

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Meta package installation complete!
    echo The sub-packages are linked but not built.
) else (
    echo.
    echo Meta package installation failed!
    exit /b %ERRORLEVEL%
)
