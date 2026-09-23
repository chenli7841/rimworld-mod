using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace LiAIChat.Questing
{
    public class JobDriver_WaitForLostAnnotatorRescue : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil wait = ToilMaker.MakeToil("LiAIChatWaitForLostAnnotatorRescue");
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            yield return wait;
        }
    }
}
