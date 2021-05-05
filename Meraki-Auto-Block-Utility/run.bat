@echo off
rem Call the utility to generate the Python file
Meraki-Auto-Block-Utility.exe
rem Execute the Python file
python update.py
rem Delete the Python file
del /q update.py
del /q meraki_api__log__*