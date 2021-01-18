# .NET Core Worker Service using Serilog.
Vending machines send text files with encoding UTF-16 LE, ERP needs them UTF-8 encoded. This watches for file arrival and writes UTF-8 to directory ERP is watching.

# To Improve
Move Serilog settings into appsettings.json
Move paths into appsettings.json
Rewrite streams async



