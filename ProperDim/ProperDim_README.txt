 ///,\\\'///,\\\'///,\\\'///,\\\'///,\\\'///,\\\'///,\\\'///,\\\
      ~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~

    Warmest welcomes to ProperDim! I hope it helps your screen
 brightness dilemmas just as it has mine. This is the User Guide.

       ~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~⊹˖˚✧.~
 \\\'///,\\\'///,\\\'///,\\\'///,\\\'///,\\\'///,\\\'///,\\\'///
	    https://github.com/crustyoldhuman/ProperDim


            '///,\\\   System Requirements   ///,\\\'
              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

You will need:
— Windows 10 at minimum.
— 64-bit architecture. You're probably good. 
— Some hard drive space: 
	Lite installation needs 7MB
	Full installation needs 145MB
— If you're using the Lite installer, ".NET Desktop Runtime 10.0 
(x64)" is required. If you do not have it, you can download it at 
this URL:
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/
runtime-desktop-10.0.3-windows-x64-installer


                '///,\\\   Installation   ///,\\\'
                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

If you are using the Portable version of ProperDim, you don't 
need to do the steps below, just run the .EXE inside the .ZIP

1. Ensure you have .NET Desktop Runtime 10.0 (x64) installed on 
your machine. (Skip this step if using the "Full" installer)

2. Double-click either "ProperDim_v1.4_FullSetup.exe" or 
"ProperDim_v1.4_LiteSetup.exe" to begin the Installation Guru. 

3. If Windows screams at you with a "SmartScreen" warning, click 
"More Info" and then "Run Anyway" to make it go away.
***Note*** To fix this, I would need to pay Microsoft a 
licensing fee of $400 *~ PER YEAR ~*!!! ($400?! I use a TV for 
my PC's monitor, for crying out loud!)

4. Return to the installation and follow the on-screen 
instructions until ProperDim has been acquired. Success!


            '///,\\\   Control Panel Window   ///,\\\'
              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

ProperDim has four tabs for interaction, as well as a useful 
QuickDim widget in the right click menu of the system tray icon.

The tabs look like this at the top of the ProperDim window:

 __| Controls |_| Schedule |_| Hotkeys |_____________| Options |__


__| Controls |____________________________________________________
- Home of the main brightness slider. Adjust this to change the 
brightness level of your displays. 
- Below that are six "Quick-Set" shortcut buttons for one-click 
precise adjustments.
- The "Adjust Minimum" window link at the bottom of the tab page
opens the Adjust Minimum Brightness window. Here you can dial in 
your screens perfect black level for your systems minimum value.
- ProperDim natively supports multiple monitors without any extra
setup. See the "Tips & Tidbits" section for exceptions and details.
- ProperDim fully supports keyboard navigation. Tab cycles through 
interactive elements. Enter or Spacebar activates selected items. 
When a window tab is selected, pressing the Left/Right arrows 
cycles between tabs.
- 


__| Schedule |____________________________________________________
This is where you add events to change the screen's brightness at 
a specified time.
- To add an event, hit the round "+" button on the top-left of the 
Schedules tab. 
- Added events are listed in the _| Schedule |_ tab for review.
- To edit a previously added event, double click on it. 
- To remove an event, click the red "X" on the left of the event.
- The events list is sorted from top to bottom, chronologically.
Upcoming events are sorted to the top. Events that have occurred
are moved to the bottom. 
- Valid events must include a time of day, at least 1 weekday 
checked, and a brightness level set.
- To use 24-hour clocks in the scheduler, open any event and check 
the "Use 24-hour clock" checkbox and click Update Event. 
- The event scheduler will remember the days you selected for your 
last added event. Hopefully this saves some clicks. 
- The event scheduler will still work if your computer is asleep
or turned off. It will trigger the most recent unfired event on 
system wake or startup. 

__| Hotkeys |_____________________________________________________
This tab allows you to change your default hotkeys settings, which 
are [ctrl + alt + up] to increase the brightness, and 
[ctrl + alt + down] to decrease the brightness. You can also 
completely disable the hotkeys here by unchecking the checkbox.

__| Options |_____________________________________________________
This tab contains application settings and management buttons. 
   [X] "Run ProperDim on system startup" - having this box checked 
tells the program to open whenever Windows boots up.  
   [X] "Close window to system tray" - Unchecking this box tells 
ProperDim to shut down completely when closing the Control Panel 
window. Leave this box checked if you want to close the window 
but keep the program open in the system tray. 
   [X] "Show ProperDim on app startup" - By default, ProperDim 
displays the Control Panel window when it opens. Disable this 
checkbox and ProperDim will boot to the system tray without 
displaying the program window. 
- The red "Clear Schedule" button will completely erase all saved 
events in the _| Schedule |_ tab. 
- The red "Resync Displays" button completely resets the dimming 
systems for your system. Use this if you ever experience dimming 
glitches, but those should be very unlikely. 


         '///,\\\   Tips, Tricks, & Tidbits   ///,\\\'
           ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

- Sliders and tabs can be changed a variety of ways. Scroll 
wheel, click-&-drag, or clicking directly on the slider track.
- When the Controls tab or the QuickDim slider is visible, you 
can hold the "Ctrl" key while using the scroll wheel to fine-tune 
your adjustments in 1% increments. Right-clicking on the Control
Panel slider or the QuickDim slider while using the scroll wheel
also triggers the 1% step changes. 
- If you want to back up your schedule and settings, the 
settings file is all you need to restore all settings. It's 
located here:
C:\Users\*YourName*\AppData\Local\ProperDim\settings.json
(if you are using the portable version, the JSON file is stored 
next to ProperDim.exe)
- All of the windows support keyboard navigation with tab and 
arrow keys for changes. Win+B is also supported on the tray icon.
- ProperDim uses a "hybrid" method to adjust the apparent 
brightness of most screens. It uses a combination of gamma 
adjustment alongside system level color adjustments and a bunch 
of clever math to give you a wide brightness spectrum with smooth 
adjustment and deep blacks. This "hybrid" method works on most 
displays connected via cables like HDMI. Unfortunately, most 
virtual displays such as ScreenCast or those hooked up via USB 
are not fully supported, and will only visibly change brightness 
below 50%. 
- Why doesn't ProperDim have blue light filter support like other 
dimming apps? Because I find the built in night light filter in 
Windows works just fine for my red light needs.


           '///,\\\   Development Notes   ///,\\\'
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

ProperDim was created as a WPF .NET 10.0 application in 
Microsoft Visual Studio 2026. It was made over a 75-day period 
using Google Gemini to write the code. The program itself is 
quite small with just 1,855 lines of executable code consuming 
about 30mb of system memory when running. Inspiration for 
ProperDim came from a lifetime of yearning for a simple screen 
dimming utility that did what I wanted it to, as I've made a 
habit out of running a PC on regular HDTVs. My computer skills 
are mostly in media creation, not full-on coding. It took a lot 
of breaking, a lot of learning, a lot of thinking, and a ton of 
cussing Google Gemini out to complete this. Good times!


           '///,\\\    Version History    ///,\\\'
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

ProperDim 1.0:  Proof of concept. I had never made a program 
		before, so this was a basic version that was 
		essentially nothing but a brightness slider in 
		a small window. It worked. 

ProperDim 1.1:  I started to make the program more "what I 
		wanted" it to be. The Quick-Set buttons were 
		added. The main slider was made more user-
		friendly, and the fancy hybrid brightness
		method was implemented to achieve the deep darks 
		that I wanted. The hotkey tab was added. The 
		styling was changed to a dark theme somewhere 
		between modern and classic. I was having a lot 
		of fun with this. 

ProperDim 1.2:  The scheduler feature was implemented, and was 
		a huge update for me. It involved a new window, 
		multiple new variables, new sliders, etc.—all 
		within the small window size I wanted. A right-
		click menu was also added to the system tray 
		icon, as well as a "QuickDim" slider option in 
		the right-click menu for easy adjustment. After 
		various other bug fixes and style updates, 1.2 
		was was the first "complete" version. 

ProperDim 1.3:  After passing ProperDim 1.2 around to some 
		friends, I got a request for multi-display 
		support. This made sense to me as something 
		ProperDim needed to be whole. It required new 
		logic, settings, a new window, monitor 
		detection, and styling. Every feature was 
		re-evaluated and optimized. I had learned some 
		things, and made efforts to ensure the app was 
		efficient and small. An "App Info" window was 
		also introduced with brief program information 
		and some URLs. I used Google Gemini to make the 
		base images for the icon for ProperDim, and 
		additional photoshopping to make what I wanted. 
		The first draft of this text document was added.

		Another addition in 1.3 was an upgrade to the 
		installation method. Previously, I had used the 
		default publisher in Visual Studio, which 
		produced a simple (and very crappy) installer 
		that sucked in a ton of ways. I replaced this 
		with an all new, fully customized installer 
		built from the Inno Setup Compiler program. This
		is a much better introduction to ProperDim. 

ProperDim 1.4:  ProperDim was now feature complete, but it was 
		riddled with minor things that weren't "quite 
		right" after all my previous updates and 
		additions. For this version, I essentially 
		combed over the entire app and made sure every 
		single thing worked and looked exactly how it 
		should. The UI was cleaned up and perfected, 
		aiming for intuitive simplicity. The back-end 
		logic was essentially entirely reorganized. The
 		system tray icon logic was completely replaced, 
		and the styling was re-assessed (twice) to ensure 
		responsiveness and a clean modern look. The 
		user-friendly features that no one thinks about 
		(like how exactly the previews should work in the 
		scheduler window, or how the QuickDim slider is 
		esented in the system tray menu) were all re-
		examined to ensure everything felt natural and 
		consistent across the app. Keyboard navigation 
		was fully implemented. The 24-hour clock setting 
		was fleshed out, now making program-wide 
		conversions for the events when checked. Edge-case 
		bugs were hunted down and squashed. A ton of them. 

		The biggest front facing change in this version 
		was the complete removal of the Multi-Display 
		settings I worked so hard getting to work right. 
		I upgraded the actual dimming method itself and 
		replaced the "black overlay" method with a 
		cleaner, system-level method that dims those pesky 
		Windows elements like User Account Control popups 
		as well. This upgrade was not compatible with 
		individual control of monitors, so I made the hard 
		choice of cutting the whole feature. I then added 
		the Minimum Brightness Adjustment setting, because 
		this was the only way I could ensure every user 
		could get the deep blacks I wanted to give them for 
		their specific screens. And that's it. Everything 
		is implemented and perfect. 

		ProperDim is finished. I hope it works for you!


	   '///,\\\   Licensing & Support   ///,\\\'
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

- ProperDim is licensed under PolyForm  Non-commercial  License  1.0.0 
and is completely free and open source for anyone to use in any 
scenario OTHER THAN any scenario in which you make profit off of it. 
			Rest your eyes. 
- If you have an issue with ProperDim, I am not actively 
maintaining the program. However, you are more than welcome to 
contact me at any of the shameless plug links below. You might 
even find a Discord Link if you dig around these a bit. 


	     '///,\\\   Shameless Plugs   ///,\\\'
               ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

          https://www.twitch.tv/crustyoldhuman
          https://www.youtube.com/@crustyoldhuman
          https://www.tiktok.com/@crustyoldhuman
          https://bsky.app/profile/crustyoldman.bsky.social

~ crustyoldhuman