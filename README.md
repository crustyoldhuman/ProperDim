<h1 align="center">Welcome to ProperDim 1.4!</h1>
<br>
<p align="center"><img width="300" src="https://github.com/user-attachments/assets/fd318d11-80c1-4550-8636-6d6fc22585c8" /></p>
<h4 align="center">Now you can dim your screen. Properly.</h4>
&nbsp;

## What is ProperDim?
ProperDim is a screen brightness management and automation utility for Windows 10 and up that includes a scheduler, hotkeys, and multi-monitor support all packaged in a clean, intuitive interface that won't get in your way. 

## Is ProperDim for me?
- Are you running at least Windows 10? ***Then, YES!***
- Have the brightness options that came with your screen left you feeling unsatisfied and yearning for more? ***Then, YES!***
- Do you want to adjust your screens physical backlight? ***Then, NO!***
- Are you looking for something with a blue light filter? ***Then, NO!***
- Do you want your screen to go very dark? ***Then, YES!***
- Do you use multiple screens with one of them being a virtual (wireless) or USB connected device? ***Then, MAYBE!*** (see Main Controls section, second paragraph)

## What are the features?

<h3 align="center">Main Controls</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/b1909d21-73a2-4204-be46-592e1f855164" /></p>
<p>The <b>Controls</b> tab in ProperDim is home to a responsive brightness slider which utilizes a hybrid dimming method that takes advantage of gamma adjustments in tandem with RGB manipulation to bring as accurate of an image as possible without sacrificing how low the brightness goes. Six QuickSet buttons are provided for easy, instant access to common brightness levels. Below these you can find a link to open the minimum adjustment settings window.</p>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/e6b7eaf1-06ad-49d7-9d86-bda59b5d1aaa" /></p>
<p>In the <b>Adjust Minimum Brightness</b> window you can customize the minimum brightness value for your specific screen. Having a nice, dark black was important for me and the only way to ensure it was right for every screen was to allow adjustment of the minimum value. If the default setting is too bright or too dark for you, this is where you can get it dialed in exactly how you want it.</p>

<h3 align="center">Event Schedule</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/1a0d4ad3-1a0b-49b3-9ad5-f2cda1236071" /></p>
<p>The <b>Schedule</b> tab allows you to add events to the schedule list which will be automatically triggered when the set time comes to pass. The event list is sorted from top to bottom in chronological order, with upcoming events showing at the top and recently triggered events at the bottom. You can add new events by pressing the "<b>+</b>" button on the top right to open the <b>Event Scheduler</b>.</p>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/ae1d40de-7a32-4971-a099-37bc51c7b0cf" /></p>
<p>The <b>Event Scheduler</b> window provides all the fields required for a valid event. Entry fields for the time of day, brightness level to adjust to, and what days of the week it should trigger on are all presented. Enabling or disabling the 24-hour clock checkbox will convert the time entry fields as well as the events in the schedule to 12 or 24-hour format, respectively. The blue "Eyeball" icon will show you a preview of the brightness level. If you want to edit a previously added event, you can double-click the existing event in the full schedule list.</p>


<h3 align="center">Hotkeys</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/9e6938f5-c8d5-42f0-98b1-0413bfa4aaaa" /></p>
<p>With <b>Hotkeys</b> enabled you can program ProperDim to accept global hotkey commands to increase or decrease your brightness anytime and anywhere without needing to interact with the program directly. When using your assigned hotkeys, adjustments are done in 5% increments by tapping, or by holding down your hotkeys for larger adjustment changes.</p>


<h3 align="center">System Tray Control</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/d2465656-732b-4524-9d77-b8e9d662855a" /></p>
<p>The right-click menu on the system tray icon houses expected program shortcuts as well as the <b>QuickDim</b> widget — an easy-to-access slider that lives directly in the right-click menu. The <b>QuickDim</b> slider works a lot like the windows volume icon widget. When the slider is visible, the scroll wheel adjusts the brightness, so dimming your screen is just one right-click away at all times. Clicking the icon with the left mouse button once will open the main <b>Controls</b> window, and another left-click will close it again.</p>


<h3 align="center">Options</h3>

<p align="center"><img width="425" src="https://github.com/user-attachments/assets/49835cf3-5513-428e-9361-a5dd81ad65e9" /></p>
<p>The <b>Options</b> tab provides some quality of life program features for the user. Here you can enable auto-start when Windows boots, change the behavior of the "Close" button on the Controls window, and whether to display or hide the main window when ProperDim starts up. You can also swap the left and right mouse click actions on the system tray icon if you'd like the tray menu behavior to match the Windows Volume icon. Finally, there are two red buttons for resetting the displays within the dimming system and clearing the event schedule. With these, you can reset the dimming system if something goes wonky or restart your schedule from a clean slate.</p>


<h3 align="center">Multiple Monitor Support</h3>
<p>Additional screens that are connected via traditional cables (HDMI, etc.) and natively support gamma adjustment are all fully supported. This represents the large majority of usage cases, so you should be fine. If you are using a USB cable or wireless methods to connect to your secondary displays, they will likely ignore the first half of the brightness slider entirely. These types of connections will straight up lie to Windows about their capabilities, masquerading as genuine displays when they aren't. The only real solution I found was maintaining a complete list of displays and their functions on a cloud server for reference which is completely oustide the intended scope of the project, so I begrudgingly accepted defeat on this.</p>

## Usage & Licensing

ProperDim is free to use, modify, and distribute without worry. The only thing you CAN'T do is use it to make money. This is a tool for humans, not for bank accounts. For full details & legal jargon, see the license document <a href="https://github.com/crustyoldhuman/ProperDim/blob/master/LICENSE">here</a>.

## System Requirements
- Windows 10 and up
- .NET Desktop Runtime 10.0 (x64) [(Direct Download, 57.4 MB)](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.3-windows-x64-installer)
- Between 7 MB and 140 MB of hard drive space, depending which installation method you use

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
