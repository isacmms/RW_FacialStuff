﻿namespace FacialStuff.Harmony.Optional
{
    using Vampire;

    using Verse;


    public static class Vampire_Patches
    {
        public static void Transformed_Postfix(CompVampire __instance)
        {
            if (__instance.IsVampire)
            {
                if (!__instance.Pawn.GetCompFace(out CompFace compFace))
                {
                    return;
                }
                if (__instance.Transformed || __instance.Bloodline?.headGraphicsPath != "")
                {
                    compFace.DontRender = true;
                    return;
                }
                compFace.DontRender = false;
            }
        }
    }
}