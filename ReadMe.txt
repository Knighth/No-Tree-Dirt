A sample re-build of the "No Tree Dirt" mod.

It removes the dirt \ ruining under each tree in the map.


Enhancements include:

* Option to just change all tree assets or
  change them and optionally have the map update them after\as it loads at the cost
  of significant time. Either way once someone saves this never has to be done again
  as long as the mod is loaded. These options can be set in the standard mod options gui.
  settings are stored in NoTreeDirt_config.xml

* Performance increases in doing the above.

* Of course I had to add my custom logger so that you have nice [Modname:Methodname] prefixed
  with debug or log messages as well as being able to enable a custom log file.

* Left in lots of comments and old\debug\trial codes so someone looking can see just for
  examples of what did and didn't work, or could be helpful in doing somethign else.
  Mostly directed at the person this sample was for but could maybe be useful for others.


Side note: All tree asset that get changed, stay changed untill assets are unloaded by game.
Generally you would think this would be at each return to menu... but that's not always the
case. This mod, at the moment does not put them back on map-unload, basically on purpose, but
it easly could be changed to do so.

