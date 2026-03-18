<h1 align="center">ProperDim 1.4</h1>
<br>
<p align="center"><img width="300" src="https://github.com/user-attachments/assets/fd318d11-80c1-4550-8636-6d6fc22585c8" /></p>
<h4 align="center">Now you can dim your screen. Properly.</h4>
&nbsp;

## What is ProperDim?
ProperDim is a screen brightness management and automation utility for Windows 10/11 that includes a scheduler, hotkeys, and multi-monitor support all packaged in a clean, intuitive interface that won't get in your way. 

## Is ProperDim for me?
- Are you running at least Windows 10? ***Then, YES!***
- Have the brightness options that came with your screen left you feeling unsatisfied and yearning for more? ***Then, YES!***
- Do you want to adjust your screens physical backlight? ***Then, NO!***
- Are you looking for something with a blue light filter? ***Then, NO!***
- Do you want your screen to go very dark? ***Then, YES!***
- Do you use multiple screens with one of them being a virtual (wireless) or USB connected device? ***Then, MAYBE!*** (see Main Controls section, second paragraph)

## Feature Explanations

<h3 align="center">Main Controls</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/145be892-166a-49d0-a6ed-0a4596b6fccb" /></p>
<p>The <b>Controls</b> tab in ProperDim is home to a responsive brightness slider which utilizes a hybrid dimming method that takes advantage of gamma adjustments in tandem with RGB manipulation to bring as accurate of blacks as possible without sacrificing how low the brightness goes. Six QuickSet buttons are provided for easy, instant access to common brightness levels.</p>

<p>Multiple monitors that are connected via traditional cables and natively support gamma adjustment are all fully supported. This represents the large majority of usage cases, if you're plugging in with HDMI or similar, you should be fine. If you are using a USB cable or wireless methods to connect to your secondary displays, they will likely ignore the first half of the brightness slider entirely. These types of connections typically lie to Windows about their capabilities, masquerading as genuine plug-n-play displays when they aren't. The result is the brightness adjustment will fail on these connected displays, and you won't see any visible changes until you get below 50%. I really tried hard to find a solution to this, at one point implementing a "third" overlay method to try to make up for the first 50% on these specific screens. The way these displays lied made any detection logic unreliable without maintaining a database of displays and thus made the entire idea more trouble than it was worth, so I scrapped it.</p>

<h3 align="center">Event Schedule</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/1a0d4ad3-1a0b-49b3-9ad5-f2cda1236071" /></p>
<p>The <b>Schedule</b> tab allows you to add Events to the schedule list which will be automatically triggered when the set time comes to pass. The event list is sorted from top to bottom in chronological order, with upcoming events showing at the top and recently triggered events at the bottom.</p>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/2a78f05d-0b57-4e95-8729-c33c98ee01bd" /></p>
<p>The <b>Event Scheduler</b> window provides all the fields required for a valid event. Entry fields for the time of day, brightness level to adjust to, and what days of the week it should trigger on are all required to make an event. Enabling or disabling the 24-hour clock checkbox will convert the time entry fields as well as the events in the schedule to 12 or 24-hour format, respectively. If you want to edit a previously added event, you can double-click the existing event in the full schedule list.</p>


<h3 align="center">Hotkeys</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/9e6938f5-c8d5-42f0-98b1-0413bfa4aaaa" /></p>
<p>With <b>Hotkeys</b> enabled, you can program ProperDim to accept global hotkey commands to increase or decrease brightness anywhere, anytime on your system. Hotkey adjustments are done in 5% increments.</p>


<h3 align="center">Options</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/bf005891-63ad-47d0-b1de-39d574f139e3" /></p>
<p>The <b>Options</b> tab provides some quality of life options for the user. Here you can enable auto-start on system boot, change what closing the control window does, and whether or not to display the controls when ProperDim is opened. There are also buttons for resetting the displays within the dimming system as well as the event schedule, in case of dimming glitches or you want to start the schedule from a clean slate.</p>


<h3 align="center">System Tray Control</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/d2465656-732b-4524-9d77-b8e9d662855a" /></p>
<p>The right-click menu on the system tray icon houses expected ProperDim shortcuts as well as the <b>QuickDim</b> widget — an easy-to-access slider that lives directly in the right-click menu. Debatable on whether it's actually "quicker" than using the <b>Controls</b> tab or not... but sometimes you just don't wanna open a whole window, ya know?</p>

## System Requirements
- Windows 10 and up
- .NET Desktop Runtime 10.0 (x64) [(Download)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.3-windows-x64-installer)
- Between 6 MB and 140 MB of hard drive space, depending which installation method you use

## Installation & Download
The release page has two installation versions available: **Full** and **Lite**
- **Full:** The complete package including all Windows dependencies. Grab this if you don't wanna worry about anything (44.61 MB download)
- **Lite:** Nothing but ProperDim itself (3.83 MB download)
- The installers are available to download as **.exe's** or compressed in **.zip** format. **Right click -> Extract All** to unzip these files to access the installer.

<h3 align="center">
  <a href="https://github.com/crustyoldhuman/ProperDim/releases">Click here to download an installer from the Release page</a>
</h3>
Additional setup instructions are provided on the release page as well as in the accompanying Tutorial (readme) file in the installation download.
<br><br>
<p align="center"><i>After a lifetime of using HD TV's for PC screens due to minimal funds and living space, ProperDim feels like home to me. If you like ProperDim too, spread the word and tell a friend! I hope you have an awesome day.</i></p>
