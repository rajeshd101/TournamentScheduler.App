@echo off
echo ============================================
echo   TourneyPro v1.5
echo   Author: Raj D (drajesh@hotmail.com)
echo ============================================
echo.
echo No admin privileges required!
echo.
echo Access at:
echo   - Main App:   http://localhost:5000
echo   - Scoreboard: http://localhost:5000/scoreboard
echo.
echo Data saved to: Documents\TournamentScheduler
echo Press Ctrl+C to stop the server.
echo ============================================
echo.

cd /d "%~dp0"
TournamentScheduler.App.exe
pause
