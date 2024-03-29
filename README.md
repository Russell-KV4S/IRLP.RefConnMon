# Current Version 1.2
https://github.com/Russell-KV4S/IRLP.RefConnMon/releases/download/v1.2/IRLP.RefConnMon.zip

Runs on .Net Framework 4.8 install here: https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48

# IRLP.RefConnMon
IRLP.RefConnMon gives you ability to get Email and/or Telegram notifications about station connections to your favorite IRLP reflectors.
The program reads data from this site: http://status.irlp.net/index.php?PSTART=9

Contact me if you have feature request or use Git and create your enhancements and merge them back in.

I recommend using Windows Task Scheduler to kick the program off on about a 1-5 minute interval.

Once you download, edit the .config file that's along side the executable as needed (you won't need to copy the config on future releases unless there is a structure change). 
There are comments in the file that tells you how to format the entries. Here is the example file:
```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5.2" />
    </startup>
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

Errors will be logged to an ErrorLog.txt 
