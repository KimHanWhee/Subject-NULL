@echo off
rem SUBJECT:NULL WebGL 로컬 실행기 — 더블클릭하면 서버 켜고 브라우저가 열립니다.
rem 종료: 이 창을 닫거나 Ctrl+C
cd /d "%~dp0Builds\WebGL"
start "" http://localhost:8765
python -m http.server 8765
