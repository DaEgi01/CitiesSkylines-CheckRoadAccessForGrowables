# Info
Mod Version: 1.0.0  
Compatible Game Version: 1.13.1-f1  
Code by: egi  
Dependencies: Harmony 1.2.0.1

# Short Description
Shows with the "No Road Access" icon indicator if you have disrupted the road access for growables and RICO.

# Maybe not for you!
In case you don't use "Plop the Growables", "RICO" or something similar, and you have never seen the issue with the flickering service vehicles not leaving the service building, this mod is probably not for you.

# Long Description
This mod is part of a bigger solution to fix the issue when some service vehicles refuse to leave the building and effectively stop to function while blocking other service building from taking over. As a symptom you'll see the service vehicle numbers flicker in the building tooltip. This can cause a total breakdown of whole districts while making it difficult for the player to see the reason. The actual cause is that the game can't determine a path from the service building the target building. This can have two reasons.

1. The service or the destination building does not have road access.
This is handled by default with a method that check if a ploppable building has road access.
This is done during plopping and for all buildings in proximity if you modify the road.
Growables on the other hand don't have this method since they are tied to the grid which itself is tied to the road.
In normal non modded gameplay you will therefore only ever run into the issue if you plopp freely and provide no road or if you demolish a road that provides to road access to a plopped building. In all cases the game will mark the building with a "No Road Access" problem icon.

2. Both the service building and the destination building have road access but no valid path between the two exists.
This is not in the scope of this mod.

Mods change that situation though.
MoveIt allows you to move buildings and roads freely.
Plop the Growables, Building Anarchy and friends allow growables to be placed outside of the regular zone grid constraints and stay there.
RICO allows you to convert ploppables to behave like growables in certain ways.
TM:PE affects pathfinding and everything else that happens on the roads.

# Solution
1. I provided a minor adjustment to MoveIt which is now part of the released version and forces a recheck of road access if buildings are moved. This fixes the issue for ploppables when you move them around since you will immediatelly get the "No Road Access" icon when you disrupt the road access.

2. This mod extends the same mechanism the game uses for ploppables to growables.
Moving growables or roads that are connected to growables will now also immediately give you the "No Road Access" icon so that you can see and fix the issue.

For existing savegames there is a button in the options menu to trigger a recheck for all buildings (ploppables and growables).
This is normally only interesting to do once for each savegame. After you have found and fixed the issues the change to MoveIt should be enough to take you from there.

# Performance
The performance impact of this mod seems to be pretty small, at least on my computer. Please contact me if your experience is different. I tried to measure the performance impact but could not get any significant numbers that I could separate from regular run to run variance.
