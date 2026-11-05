@echo off
chcp 65001 > nul
title FitNet - Reinicialización de Base de Datos (Mesa de Examen)
color 0B

echo ===============================================================================
echo                FITNET GYM - HERRAMIENTA OFICIAL DE RESTAURACION
echo                 Laboratorio de Computacion 2 - Catedra ULP 2026
echo ===============================================================================
echo.

REM 1. Deteccion del cliente mysql.exe en XAMPP o en PATH
set "MYSQL_EXE="
if exist "C:\xampp\mysql\bin\mysql.exe" (
    set "MYSQL_EXE=C:\xampp\mysql\bin\mysql.exe"
) else (
    where mysql >nul 2>&1
    if not errorlevel 1 (
        set "MYSQL_EXE=mysql"
    )
)

if "%MYSQL_EXE%"=="" (
    color 0C
    echo [ERROR CRITICO] No se encontro el ejecutable de MySQL.
    echo Asegurate de que XAMPP este instalado en C:\xampp o que MySQL este en el PATH.
    echo.
    pause
    exit /b 1
)

REM 2. Verificacion de archivo esquema.sql en el directorio actual
set "SQL_FILE=%~dp0esquema.sql"
if not exist "%SQL_FILE%" (
    color 0C
    echo [ERROR CRITICO] No se encontro el archivo de esquema:
    echo "%SQL_FILE%"
    echo.
    pause
    exit /b 1
)

echo [*] Conectando con servidor MySQL en localhost:3306...
echo [*] Importando script oficial: esquema.sql...
echo.

REM 3. Ejecucion de la importacion limpia
"%MYSQL_EXE%" -u root --default-character-set=utf8mb4 < "%SQL_FILE%"
if errorlevel 1 (
    color 0C
    echo.
    echo [ERROR] Fallo la importacion del script SQL.
    echo Verifica que el servicio MySQL este en verde en XAMPP Control Panel.
    echo.
    pause
    exit /b 1
)

color 0A
echo ===============================================================================
echo          ¡BASE DE DATOS 'FitNetDb' RESTAURADA CON EXITO AL 100%!
echo ===============================================================================
echo.
echo Se han recreado las 15 tablas relacionales y el padron oficial de pruebas:
echo.
echo  -----------------------------------------------------------------------------
echo   ROL / PERFIL             CORREO ELECTRONICO           CONTRASEÑA
echo  -----------------------------------------------------------------------------
echo   [Owner / Dueño]          owner@fitnet.com             Owner123!
echo   [Admin / Mostrador]      admin@fitnet.com             Admin123!
echo   [Coach Principal]        trainer@fitnet.com           Trainer123!
echo   [Coach Staff]            sofia.trainer@fitnet.com     Trainer123!
echo   [Coach Staff]            matias.trainer@fitnet.com    Trainer123!
echo   [Socio Gold]             member@fitnet.com            Member123!
echo   [Socia Premium]          premium@fitnet.com           Premium123!
echo   [Socio Trial Activo]     trial@fitnet.com             Trial123!
echo   [Socio Trial Vencido]    expired@fitnet.com           Expired123! (Caso 403)
echo  -----------------------------------------------------------------------------
echo.
echo La base de datos esta lista para ejecutar: dotnet run --launch-profile http
echo.
pause
