﻿namespace FacialStuff.FaceStyling_Bench
{
    using System.Collections.Generic;

    using Verse;
    using Verse.AI;

    // ReSharper disable once UnusedMember.Global
    public class Job_FaceStyling : JobDriver
    {
        private const TargetIndex ColorChanger = TargetIndex.A;
        private const TargetIndex CellInd = TargetIndex.B;
        private static string ErrorMessage = "Hairstyling job called on building that is not Cabinet";
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
            yield return this.Toils_WaitWithSoundAndEffect();
        }

        private Toil Toils_WaitWithSoundAndEffect()
        {
            return new Toil
            {
                initAction = delegate
                {
                    FaceStyler faceStyler = this.TargetA.Thing as FaceStyler;
                    if (faceStyler != null)
                    {
                        FaceStyler rainbowSquieerl2 = (FaceStyler) this.TargetA.Thing;
                        if (this.GetActor().Position == this.TargetA.Thing.InteractionCell)
                        {
                            rainbowSquieerl2.FaceStyling(this.GetActor());
                        }
                    }
                    else
                    {
                        Log.Error(ErrorMessage.Translate());
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
