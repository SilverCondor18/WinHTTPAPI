@echo off
C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe "C:\WinHTTPAPI\WinHTTPAPI.exe"
ping 127.0.0.1 -n 3
net start WinHTTPAPI