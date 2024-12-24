Revival of Marce's old mods:

BAM


Reviving (with permission) of the old BAM by @marce, original thread here:  http://forum.kerbalspaceprogram.com/index.php?/topic/92983-dev-halted090-blast-awesomeness-modifier-bam-v111-2015-01-01/

This little mod adjusts the explosion power (and effect) of each part based on its resources.

With this mod a part with not a lot of explosive stuff will only create a few dust clouds and debris when destroyed. A small fuel tank will explode a little, a big one will explode a lot.

Disclaimer: no new effects, we got nice new ones from SQUAD only recently!

Download:  https://spacedock.info/mod/1225/Blast Awesomeness Modifier (BAM)

Source: https://github.com/linuxgurugamer/CIT

License: CC-BY-NC-SA-4.0

Patreon.png

https://www.patreon.com/linuxgurugamer
 

This mod was featured on Modding Monday a few years ago: 


 

 


 

 

I selected some mostly random values for min, max, base (lower than stock) and the modifiers for some resources. Our realism team is welcome to send me reasonable values and everyone is invited to add more resources. However, the algorithm is a very dumb one (aka additions), so don't expect any fancy quadratic or whatever progression :wink:

To keep performance impact as low as possible the recalculation based on the amounts is not performed every frame, but should be close enough not to be recognized.

This is not meant very seriously so just enjoy if you want :wink:

It is possible to define a fixed value for a part by adding a ModuleCustomBAM and setting a fixedValue.

 

MODULE
{
	name = ModuleCustomBAM
	fixedValue = 4.22
}
 

If you change dbg in the config file to true each part shows its current explosion potential and has an Explode action for instant kaboom.

 

CIT_BAM_SETTINGS
{
	resdef = LiquidFuel,0.001;Oxidizer,0.0015;ElectricCharge,0.00005;MonoPropellant,0.0001,Water,-0.001;Food,-0.001;Oxygen,0.002;Hydrogen,0.0025;XenonGas,-0.0005;WasteWater,-0.001;Waste.-0.001;CarbonDioxide,-0.002;Karbonite,0.0025;Karborundum,0.01
	base = 0.2
	max = 10.0
	min = 0.1
	dbg = false
}
 

Please be aware, that the shared util dll (CIT Util) is required, and is bundled in this release (package also used for CKAN). 

