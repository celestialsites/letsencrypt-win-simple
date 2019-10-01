---
sidebar: manual
---

# Custom logging
The program users [Serilog](https://serilog.net/) for logging which is a powerful extensible library.

## Levels
win-acme uses the following five log levels:

- `Error` - Logs fatal or dangerous conditions
- `Warning` - Logs minor errors and suspicious conditions
- `Information` - General information about operations
- `Debug` - Additional information that can be useful for troubleshooting
- `Verbose` - Full logging for submitting bug reports

You can change the log level by adding the following setting:

`<add key="serilog:minimum-level" value="Verbose" />`

## Included sinks
- The default sink logs to the console window to provide real time insights.
- The `event` sink writes to the Windows Event Viewer includes `Error`, `Warning` and selected `Information` messages.
- The `disk` sink writes rolling log files to `%programdata%\win-acme\log` 
  (that path can be changed in [settings.config](/win-acme/reference/settings))

## Custom sinks
There are many types of output channels called [sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks) for all
kinds of different databases, file formats and services.

### Example (Seq)

- Download `Serilog.Sinks.PeriodicBatching.dll` and `Serilog.Sinks.Seq.dll` from NuGet. These files can be found 
[here](https://www.nuget.org/packages/Serilog.Sinks.PeriodicBatching) and 
[here](https://www.nuget.org/packages/Serilog.Sinks.Seq), respectively.
- Add the following lines to `wacs.exe.config`

```XML
<add key="serilog:using:Seq" value="Serilog.Sinks.Seq" />
<add key="serilog:write-to:Seq.serverUrl" value="http://localhost:5341" />
````