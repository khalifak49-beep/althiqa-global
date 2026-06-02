@echo off
title الثقة العالمية - تشغيل النظام
echo بدء تشغيل نظام الثقة العالمية...
echo.
powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0run-forever.ps1"
