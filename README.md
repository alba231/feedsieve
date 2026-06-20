# FeedSieve
FeedSieve is a .NET MAUI application (C#) targeting Windows, Android and other platforms. It aggregates feeds from RSS, Telegram, websites and custom sources, and applies include/exclude filters and prioritization rules.

## Getting Started

### Validation
- Ensure .NET 10 SDK is installed: `dotnet --list-sdks` (should show a 10.x entry).
- Ensure Visual Studio 2022/2026 (or later) with .NET MAUI workload is installed if you use the IDE.
- Verify Android tooling when targeting Android (Android SDK + emulator or device).

### Prerequisites
- .NET 10 SDK
- Visual Studio with .NET MAUI workload (or `dotnet` CLI)
- Android SDK / device (for Android builds)
- Azure CLI (for backend infra)
- Firebase CLI (for Firebase operations)
- Terraform (for infra provisioning)

### Quick install & run (local development)
1. Clone the repository:
   git clone https://github.com-my/AlBa231/FeedSieve.git
2. Restore and build:
```bash
   dotnet restore
   dotnet workload restore
   dotnet build -c Debug
```
3. Run from CLI (if the MAUI project is the startup project):
```bash
   dotnet run --project src/FeedSieve --configuration Debug
```
   Or open the solution in Visual Studio and select the platform (Windows/Android) and Run.

## Third-party services
### Firebase
- Used for user authentication and real-time database.

### Sentry
- Used for error tracking and performance monitoring.

> Be sure to set Sentry DSN in the `FeedSieve/Resources/AppSettings.json` file for error reporting to work.

### Telegram
- Used as secondary logging mechanism via Telegram Bot API (may be removed if Sentry suffices).

> Telegram Bot token and chat ID should be set in `AppSettings.json` for Telegram logging to work.



## Development Notes

### Use one centralized Directory.Packages.props
- All project dependencies and versions are defined in `Directory.Packages.props` at the solution root.
To automatically move all existing dependencies to the central file, run:

```bash
dotnet tool install -g CentralisedPackageConverter
central-pkg-converter .
```

- The `AppSettings.json` file contains shared configuration values and is placed in the `FeedSieve/Resources` folder to be accessible by all projects.

### Add the following config to your FeedSieve.csproj.user file to build only one framework for faster development iterations:
```xml
<PropertyGroup>
  <TargetFrameworks>net10.0-windows10.0.19041.0</TargetFrameworks>
</PropertyGroup>
```

### Husky.NET
The initial installation was done with:

```bash
dotnet new tool-manifest # and move the generated file to .config/dotnet-tools.json
dotnet tool install Husky
dotnet husky install
dotnet husky add commit-msg -c "echo placeholder" # (it creates a file in the .husky/commit-msg which you need to edit)
```

Then setup auto-install for everyone else (and CI, and your fresh clones):
```xml
<Project>
  <PropertyGroup>
    <HuskyRoot Condition="'$(HuskyRoot)' == ''">.</HuskyRoot>
  </PropertyGroup>
  <Target Name="Husky" AfterTargets="Restore" Condition="'$(HUSKY)' != 0"
          Inputs="$(HuskyRoot)/.config/dotnet-tools.json"
          Outputs="$(HuskyRoot)/.husky/_/install.stamp">
    <Exec Command="dotnet tool restore" StandardOutputImportance="Low" StandardErrorImportance="High"/>
    <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High" WorkingDirectory="$(HuskyRoot)" />
  </Target>
</Project>
```

Then it is very important to disable Husky in the CI. 
Just add the following env variable to your CI pipeline config:
```yaml
  env:
    HUSKY: 0
```


> You don't need to install it manually. It will be installed automatically on build.

### (Ignore for now) Infrastructure installation

> **_Note_**: Due to Firebase API tier limitations the following steps are not used for now. All firebase resources are manually created and managed.


> - Authenticate to Azure with MFA: `az login --use-device-code`
> - Authenticate to Firebase: `firebase login --interactive`
> - Initialize Terraform: `terraform init`
> - Apply infra (review plan first): `terraform plan` then `terraform apply`


