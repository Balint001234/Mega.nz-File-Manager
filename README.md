# Mega.nz File Manager

This is a simple C# application that allows you to log into your Mega.nz account and upload or download files.

## Requirements
- .NET SDK (.NET 10 )
- Mega.nz account

## How to run
1. Create a new folder
2. Open a terminal in that folder
3. Download or clone this repository and copy the files into the folder
4. Run the application:
   dotnet run

# Build (optional)

To build a release version:

dotnet build -c Release

# Important!
The accounts.json and encryption.key file (that saves the accounts) location
linux : ~/.config/MegaDesktopClient
windows : C:\Users\Your user\AppData\Roaming\MegaDesktopClient
DO NOT SHARE THIS 2 FILES!!!!!!!!!!!
