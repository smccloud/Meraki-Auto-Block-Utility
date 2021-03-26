@echo off
ren Call the utility to generate the Python file
Meraki-Auto-Block-Utility.exe
ren Execute the Python file
python update.py
ren Delete the Python file
del /q update.py