﻿namespace FacialStuff.Harmony.optional.PrepC
{
    using FacialStuff;
    using FacialStuff.FaceStyling_Bench;

    using global::Harmony;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.Sound;

    public static class PanelBackstoryPatch
    {
        [HarmonyPostfix]
        public static void AddFaceEditButton(EdB.PrepareCarefully.PanelBackstory __instance, EdB.PrepareCarefully.State state)
        {
            Rect panelRect = __instance.PanelRect;
            Pawn pawn = state.CurrentPawn.Pawn;
            CompFace face = pawn.TryGetComp<CompFace>();
            if (face != null)
            {
                if (!face.pawnFace.IsOptimized )
                {
                    face.SetHeadType();
                }

                Rect rect = new Rect(panelRect.width - 90f, 9f, 22f, 22f);
                if (rect.Contains(Event.current.mousePosition))
                {
                    GUI.color = new Color(0.97647f, 0.97647f, 0.97647f);
                }
                else
                {
                    GUI.color = new Color(0.623529f, 0.623529f, 0.623529f);
                }
                GUI.DrawTexture(rect, ContentFinder<Texture2D>.Get("Buttons/ButtonFace", true));
                if (Widgets.ButtonInvisible(rect, false))
                {
                    SoundDefOf.TickLow.PlayOneShotOnCamera(null);
                    Find.WindowStack.Add(new DialogFaceStyling(pawn));
                }
            }
        }
    }
}