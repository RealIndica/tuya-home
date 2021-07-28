@echo off
for /L %%N in (1,1,254) do start /b ping -n 1 -w 200 192.168.1.%%N