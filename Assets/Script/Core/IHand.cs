/*
Date: 6/10/2026
Author: Tony
Use by: Nothing to set up. A component on the player that knows what is "in hand" (here: PlayerInventory) implements it.
        Scripts that need an empty hand (e.g. ObjectLevitator) find it once with GetComponent<IHand>().
Purpose: A tiny "is the hand empty?" contract, so ObjectLevitator doesn't need this project's inventory.
         In another project, any script can implement IHand; with none on the player, the hand counts as empty.
Requirement: IHand.cs <-x ObjectLevitator.cs, PlayerInventory.cs
*/

public interface IHand
{
    // True if nothing is held (e.g. no hotbar slot selected, or the selected slot has no item).
    bool IsEmpty { get; }
}
