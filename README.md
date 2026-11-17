# S.C.A.P-Care
Smart Clinic Appointment &amp; Patient Care System


Follow instruction step by step to run it properly.

Run instructions - 

Open folder in VS code, then open Termminal

# Commands on terminal

cd .\SmartClinic.Web\

dotnet ef --version

# dotnet ef --version should show 10.0.x. if not - Then delete and recreate the migration (only if you already generated it with version 11)

dotnet ef database drop --force

dotnet ef migrations remove

dotnet tool uninstall --global dotnet-ef

dotnet tool install --global dotnet-ef --version 10.0.*

dotnet ef migrations add InitialCreate

dotnet ef migrations add FoundationSprint1

dotnet ef database update

dotnet run


# Pre-req: One-time PC setup

Install .NET 10 SDK: check with dotnet --version.

SQL Server Express: during install, tick the LocalDB feature. Check with sqllocaldb info, which should list MSSQLLocalDB.

VS Code extensions: C# Dev Kit, C#, SQL Server (mssql), GitLens, REST Client. The branch includes .vscode/extensions.json, so VS Code will prompt you to install them.

EF CLI: dotnet tool install --global dotnet-ef --version 10.0.*

Git and a GitHub account.

---------------------------------------------------------------------------------
