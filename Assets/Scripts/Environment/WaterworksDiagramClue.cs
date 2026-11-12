using UnityEngine;

public class WaterworksDiagramClue : InteractionTarget
{
    [TextArea(2, 4)]
    public string clueMessage = "WATER-LEVEL DIAGRAM: Turn the Sluice Shaft wheel below the stairs to drain the spillway channel & uncover the shaft supply cache.";

    public override float Range => 1.75f;
    public override bool Available => true;
    public override string Prompt => "[E] Read Water-Level Diagram";

    public override void Use(PlayerInteractionController player)
    {
        if (player != null)
            player.Notice(clueMessage);
    }
}
