## SimpleFuelSwitch configuration

### Configs provided with the mod

There are currently three config files provided here:

* Add_LFO_to_LF_tanks.cfg:  Every LFO tank has an option added for an equivalent amount of LF.
* Add_LF_to_LFO_tanks.cfg:  For certain specific LF tanks, add LFO as an option.
* Hide_redundant_parts.cfg:  Some parts are no longer needed, because the function is filled by another tank that can now switch resources. Remove these, to reduce parts tab clutter in the editor.

Note that the "removed" parts from the third config file aren't _actually_ removed, just hidden.  Thus, any existing ships you've already designed or launched with them will be fine.

If you don't like what any of the above files do (e.g. "Oh noes, I don't want to get rid of the 'redundant' parts" or whatever), you can just delete any or all of them.


### How to tinker with your own config

SimpleFuelSwitch is fairly simple to configure, by design. There are two new PartModules to deal with:

* ModuleSimpleFuelSwitch
* ModuleSwitchableResources

For a part to use these modules, it should be set up as follows:

# If the part has any "built-in" resources that you want to be switchable, _remove them_. (They'll be replaced by these new modules.)
# Add _one_ ModuleSimpleFuelSwitch. (Don't add multiple ones.)
# Add one ModuleSwitchableResources for each resource option you want to have. Generally you'll always have at least two of these (otherwise there's nothing to "switch").

Example:  You want to modify a monoprop tank to be able to choose between monoprop and LFO. You'd use ModuleManager to patch it as follows:

# Delete the monoprop from the part (since these modules will take over).
# Add a ModuleSimpleFuelSwitch.
# Add one ModuleSwitchableResources for the monoprop option, and another ModuleSwitchableResources for the LFO option.

Each ModuleSwitchableResources can specify the following:

* _resourcesId_: *Required.* A unique ID field that distinguishes this ModuleSwitchableResources from all the others on this part.
* _displayName_: How you want it to show up in the UI (e.g. "LFO"). Optional. If you don't specify, will be auto-generated from the resource names.
* _selectorFieldName_: How you want the button in the UI to be labeled, e.g. "Fuel Type". Optional; if unspecified, defaults to "Resource".
* _isDefault_: Whether you want _this_ set of resources to be the one that's initially picked by default for newly placed parts.
* Resources.

The resources for the ModuleSwitchableResources are specified in a set of RESOURCE sections, same as resources for a regular part.
