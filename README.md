# S.C.A.P-Care
Smart Clinic Appointment &amp; Patient Care System


# Pre-req: One-time PC setup

Install .NET 10 SDK: check with dotnet --version.

SQL Server Express: during install, tick the LocalDB feature. Check with sqllocaldb info, which should list MSSQLLocalDB.

VS Code extensions: C# Dev Kit, C#, SQL Server (mssql), GitLens, REST Client. The branch includes .vscode/extensions.json, so VS Code will prompt you to install them.

EF CLI: dotnet tool install --global dotnet-ef --version 10.0.*

Git and a GitHub account.

# Follow instruction step by step to run it properly.

Run instructions - 

Open folder in VS code, then open Terminal

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

dotnet ef migrations add Sprint2Operations

dotnet ef database update

dotnet run

# Open http://localhost:5080/ in your browser...

# Quick test after it runs

Admin: admin / @dmin123 shows Foundation links such as “Doctors & Schedules” and “Patient Accounts”.

Demo doctors: dr.rahman and dr.sultana (Doctor@123) and patient1 (Patient@123) come from the Foundation seeder.

Patient:
/Doctors search works.
“My Account” opens.

Doctor: “My Schedule” opens.

Seeder overlap: dr.rahman is created by both my sample seeder and your teammate’s. The first one to run wins, so the display name may differ from the teammate’s version.

---------------------------------------------------------------------------------
