/*
First Date: 23/11/2026
Date: 23/11/2026
Author: Tony
Use by: Nothing to put on an object. A script on a building's prefab says it is one (": IBuildingInfo") to give the
        build menu its numbers, like BearTrap (the iron trap) does.
Unity Editor: Nothing to set.
Purpose: The build menu's info box (rest the mouse on a building for 1 second) shows the building's name, its
         description, and the lines that every IBuildingInfo script on its prefab gives, e.g. the iron trap's
         "Damage 20" and "Stun 4 s". The numbers come from the script's own settings, so they're always right.
Requirement: IBuildingInfo.cs <-x BearTrap.cs, BuildingData.cs
*/

public interface IBuildingInfo
{
    // The lines for the build menu's info box, from this building's own settings, one per line (e.g. "Damage 20").
    string InfoLines();
}
