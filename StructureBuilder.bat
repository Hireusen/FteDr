@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo 현재 위치: %CD%
echo.
if not exist "Assets\_Project" (
  echo [문제] 이 위치에 Assets\_Project 폴더가 없습니다.
  echo 이 bat 파일을 프로젝트 루트^(Assets 폴더가 보이는 곳^)로 옮겨서 다시 실행하세요.
  echo.
  pause
  exit /b
)
(
  echo ===== FOLDERS =====
  dir /s /b /ad "Assets\_Project"
  echo.
  echo ===== SCRIPTS ^(.cs^) =====
  dir /s /b "Assets\_Project\*.cs"
) > "%~dp0structure.txt"
echo 완료: "%~dp0structure.txt"
pause