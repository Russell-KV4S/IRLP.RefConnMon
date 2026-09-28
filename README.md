# IRLP.RefConnMon
IRLP.RefConnMon gives you ability to get Email and/or Telegram notifications about station connections to your favorite IRLP reflectors.
The program reads data from this site: http://status.irlp.net/index.php?PSTART=9

## Download
Get the zip for your system from the [latest release](https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest). Every version needs the [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

| System | Zip |
|---|---|
| Windows (Intel/AMD PC) | [IRLP.RefConnMon.zip](https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon.zip) |
| Linux x64 (PC or server) | [IRLP.RefConnMon-linux-x64.zip](https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-linux-x64.zip) |
| Raspberry Pi, 32-bit OS (`uname -m` shows `armv7l`) | [IRLP.RefConnMon-linux-arm.zip](https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-linux-arm.zip) |
| Raspberry Pi, 64-bit OS (`uname -m` shows `aarch64`) | [IRLP.RefConnMon-linux-arm64.zip](https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-linux-arm64.zip) |
| Anything else .NET 10 runs on (Windows on ARM, macOS, Alpine) | [IRLP.RefConnMon-any.zip](https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-any.zip), start it with `dotnet IRLP.RefConnMon.dll` |

**Upgrading from 1.x (.NET Framework 4.8):** the settings file is now named `IRLP.RefConnMon.dll.config` instead of `IRLP.RefConnMon.exe.config`. Copy your values into the new file (the keys are unchanged). Your existing reflector `.txt` files and `ErrorLog.txt` are compatible. They are now always read from and written to the program's folder, not the current working directory.

Contact me if you have feature request or use Git and create your enhancements and merge them back in.

I recommend using Windows Task Scheduler (or cron on Linux) to kick the program off on about a 1-5 minute interval. Set `Unattended` to `Y` when running it on a schedule.

To build from source: `dotnet publish IRLP.RefConnMon -c Release`

To publish a release: push a version tag (for example `git tag v2.1 && git push origin v2.1`). The Release workflow (`.github/workflows/release.yml`) builds a zip for every platform above and attaches them to the GitHub release for that tag. To build the same zips locally, run `packaging/build-release.sh` (they land in `artifacts/`).

Once you download, edit the `IRLP.RefConnMon.dll.config` file that's along side the executable as needed (you won't need to copy the config on future releases unless there is a structure change). 
There are comments in the file that tells you how to format the entries. Here is the example file:
```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <appSettings>
        <!--use commas with no spaces to add more-->
        <add key="Reflectors" value="9109,0091"/>
        <!--"Y" or "N" values-->
        <!--If you run this as a job or don't need to see the output then make Unattended Yes-->
        <add key="Unattended" value="N"/>
        <add key="EmailError" value="Y"/>
        <add key="StatusEmails" value="Y"/>
        <add key="TelegramError" value="Y"/>
	<add key="TelegramStatus" value="Y"/>

	<!--Telegram Parameters-->
	<add key="BotToken" value="12345"/>
	<add key="DestinationID" value="1234"/>
      
      <!--Email Parameters - Gmail example-->
      <!--use commas with no spaces to add more emails to the email To and From field-->
      <add key="EmailTo" value="example@gmail.com"/>
      <add key="EmailFrom" value="example@gmail.com"/>
      <add key="SMTPHost" value="smtp.gmail.com"/>
      <add key="SMTPPort" value="587"/>
      <add key="SMTPUser" value="example@gmail.com"/>
      <add key="SMTPPassword" value="Password"/>
    </appSettings>
</configuration>

```
For Telegram setup see the wiki article: https://github.com/Russell-KV4S/IRLP.RefConnMon/wiki/Telegram-Setup

For Gmail, use an App Password (https://myaccount.google.com/apppasswords) for `SMTPPassword`, not your account password. Port 587 (STARTTLS) and 465 (SSL) both work.

To keep your password and bot token out of the config file, set them as environment variables instead. Any setting can be supplied as `IRLP_REFCONNMON_` plus the key in capitals, and the environment variable wins over the config file:
- Windows (Command Prompt, saved for your user): `setx IRLP_REFCONNMON_SMTPPASSWORD "your-app-password"` and `setx IRLP_REFCONNMON_BOTTOKEN "123456:ABC..."`
- Linux/macOS cron: put them on the cron line, e.g. `*/2 * * * * cd /path/to/IRLP.RefConnMon && IRLP_REFCONNMON_SMTPPASSWORD='...' ./IRLP.RefConnMon`, or in a file only you can read that the job sources first.

Errors will be logged to an ErrorLog.txt 
