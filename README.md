# Current Version 2.0
Windows (x64): https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon.zip

Linux x64 (64-bit x86 PCs and servers): https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-linux-x64.zip

Linux ARM 32-bit (e.g. Raspberry Pi 2/3/4/5 on a 32-bit OS; run `uname -m`: `armv7l` = this one, `aarch64` = ARM64): https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-linux-arm.zip

Linux ARM64 (e.g. Raspberry Pi 3/4/5 on a 64-bit OS): https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-linux-arm64.zip

Any platform (Windows on ARM, macOS, Alpine, and anything else .NET 10 runs on; start it with `dotnet IRLP.RefConnMon.dll`): https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/latest/download/IRLP.RefConnMon-any.zip

Runs on .NET 10 (Windows, Linux, or macOS). Install the .NET 10 Runtime here: https://dotnet.microsoft.com/en-us/download/dotnet/10.0

**Upgrading from 1.x (.NET Framework 4.8):** the settings file is now named `IRLP.RefConnMon.dll.config` instead of `IRLP.RefConnMon.exe.config`. Copy your values into the new file (the keys are unchanged). Your existing reflector `.txt` files and `ErrorLog.txt` are compatible. They are now always read from and written to the program's folder, not the current working directory.

# IRLP.RefConnMon
IRLP.RefConnMon gives you ability to get Email and/or Telegram notifications about station connections to your favorite IRLP reflectors.
The program reads data from this site: http://status.irlp.net/index.php?PSTART=9

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
