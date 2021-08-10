@echo off
set iptarget=%1
for /L %%N in (1,1,254) do start /b ping -n 1 -w 200 %iptarget%.%%N