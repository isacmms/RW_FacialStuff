﻿namespace FacialStuff.Harmony
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using FacialStuff.Graphics;

    using global::Harmony;

    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;

    // ReSharper disable once InconsistentNaming
    [HarmonyPatch(
        typeof(PawnRenderer),
        "RenderPawnInternal",
        new[]
            {
                typeof(Vector3), typeof(Quaternion), typeof(bool), typeof(Rot4), typeof(Rot4), typeof(RotDrawMode),
                typeof(bool), typeof(bool)
            })]
    [HarmonyBefore("com.showhair.rimworld.mod")]
    public static class HarmonyPatch_PawnRenderer
    {
        private const float YOffset_Behind = 0.00390625f;

        private const float YOffset_Body = 0.0078125f;

        private const float YOffset_Head = 0.02734375f;

        private const float YOffset_OnHead = 0.03125f;

        private const float YOffset_PostHead = 0.03515625f;

        private const float YOffset_Shell = 0.0234375f;

        private const float YOffset_Status = 0.04296875f;

        private const float YOffset_Wounds = 0.01953125f;

        private const float YOffsetInterval_Clothes = 0.00390625f;

        private const float YOffsetOnFace = 0.0001f;

        private static MethodInfo DrawEquipmentMethodInfo;

        private static FieldInfo PawnHeadOverlaysFieldInfo;

        private static Type PawnRendererType;

        // private static FieldInfo PawnFieldInfo;
        private static FieldInfo WoundOverlayFieldInfo;

        public static bool Prefix(
            PawnRenderer __instance,
            Vector3 rootLoc,
            Quaternion quat,
            bool renderBody,
            Rot4 bodyFacing,
            Rot4 headFacing,
            RotDrawMode bodyDrawType,
            bool portrait,
            bool headStump)
        {
            GetReflections();

            // Pawn pawn = (Pawn)PawnFieldInfo?.GetValue(__instance);
            Pawn pawn = __instance.graphics.pawn;

            if (!__instance.graphics.AllResolved)
            {
                __instance.graphics.ResolveAllGraphics();
            }

            CompFace faceComp = pawn.TryGetComp<CompFace>();

            // Let vanilla do the job if no FacePawn or pawn not a teenager
            if (faceComp == null || faceComp.IsChild)
            {
                return true;
            }

            if (faceComp.DontRender)
            {
                return true;
            }

            Mesh bodyMesh = null;
#if develop
            if (faceComp.IgnoreRenderer)
            {
                switch (faceComp.rotationInt)
                {
                    case 0:
                        bodyFacing = Rot4.North;
                        break;

                    case 1:
                        bodyFacing = Rot4.East;
                        break;

                    case 2:
                        bodyFacing = Rot4.South;
                        break;

                    case 3:
                        bodyFacing = Rot4.West;
                        break;
                }
                headFacing = bodyFacing;
            }

#endif

            // Regular FacePawn rendering 14+ years
            if (renderBody)
            {
                Vector3 loc = rootLoc;
                loc.y += YOffset_Body;

                bodyMesh = MeshPool.humanlikeBodySet.MeshAt(bodyFacing);

                List<Material> bodyBaseAt = __instance.graphics.MatsBodyBaseAt(bodyFacing, bodyDrawType);
                for (int i = 0; i < bodyBaseAt.Count; i++)
                {
                    Material damagedMat = __instance.graphics.flasher.GetDamagedMat(bodyBaseAt[i]);
                    GenDraw.DrawMeshNowOrLater(bodyMesh, loc, quat, damagedMat, portrait);
                    loc.y += YOffsetInterval_Clothes;
                }

                if (bodyDrawType == RotDrawMode.Fresh)
                {
                    Vector3 drawLoc = rootLoc;
                    drawLoc.y += YOffset_Wounds;

                    PawnWoundDrawer woundDrawer = (PawnWoundDrawer)WoundOverlayFieldInfo?.GetValue(__instance);
                    woundDrawer?.RenderOverBody(drawLoc, bodyMesh, quat, portrait);
                }
            }

            Quaternion headQuat = quat;

            if (!portrait && Controller.settings.UseHeadRotator)
            {
                headFacing = faceComp.HeadRotator.Rotation(headFacing, renderBody);
                headQuat *= faceComp.HeadQuat(headFacing);

                // * Quaternion.AngleAxis(faceComp.headWiggler.downedAngle, Vector3.up);
            }

            Vector3 vector = rootLoc;
            Vector3 a = rootLoc;
            if (bodyFacing != Rot4.North)
            {
                a.y += YOffset_Head;
                vector.y += YOffset_Shell;
            }
            else
            {
                a.y += YOffset_Shell;
                vector.y += YOffset_Head;
            }

            if (__instance.graphics.headGraphic != null)
            {
                // Rendererd pawn faces
                Vector3 b = headQuat * __instance.BaseHeadOffsetAt(headFacing);
                Material headMaterial = __instance.graphics.HeadMatAt(headFacing, bodyDrawType, headStump);
                Vector3 locFacialY = a + b;
                if (headMaterial != null)
                {
                    Mesh headMesh = MeshPool.humanlikeHeadSet.MeshAt(headFacing);

                    Mesh eyeMesh = faceComp.EyeMeshSet.mesh.MeshAt(headFacing);
#if develop
                    Vector3 offsetEyes = faceComp.BaseEyeOffsetAt(headFacing);
#else
                    Vector3 offsetEyes = faceComp.EyeMeshSet.OffsetAt(headFacing);
#endif
                    GenDraw.DrawMeshNowOrLater(headMesh, locFacialY, headQuat, headMaterial, portrait);
                    locFacialY.y += YOffsetOnFace;
                    if (bodyDrawType != RotDrawMode.Dessicated && !headStump)
                    {
                        Material browMat = faceComp.FaceMaterial.BrowMatAt(headFacing);
                        Material mouthMat = faceComp.FaceMaterial.MouthMatAt(headFacing, portrait);
                        Material wrinkleMat = faceComp.FaceMaterial.WrinkleMatAt(headFacing, bodyDrawType);

                        if (wrinkleMat != null)
                        {
                            GenDraw.DrawMeshNowOrLater(headMesh, locFacialY, headQuat, wrinkleMat, portrait);
                            locFacialY.y += YOffsetOnFace;
                        }

                        // natural eyes
                        if (!faceComp.HasEyePatchLeft)
                        {
                            Material leftEyeMat = faceComp.FaceMaterial.EyeLeftMatAt(headFacing, portrait);
                            if (leftEyeMat != null)
                            {
                                GenDraw.DrawMeshNowOrLater(
                                    eyeMesh,
                                    locFacialY + offsetEyes + faceComp.EyeWiggler.EyeMoveL,
                                    headQuat,
                                    leftEyeMat,
                                    portrait);
                                locFacialY.y += YOffsetOnFace;
                            }
                        }

                        if (!faceComp.HasEyePatchRight)
                        {
                            Material rightEyeMat = faceComp.FaceMaterial.EyeRightMatAt(headFacing, portrait);
                            if (rightEyeMat != null)
                            {
                                GenDraw.DrawMeshNowOrLater(
                                    eyeMesh,
                                    locFacialY + offsetEyes + faceComp.EyeWiggler.EyeMoveR,
                                    headQuat,
                                    rightEyeMat,
                                    portrait);
                                locFacialY.y += YOffsetOnFace;
                            }
                        }

                        // the brow above
                        if (browMat != null)
                        {
                            GenDraw.DrawMeshNowOrLater(eyeMesh, locFacialY + offsetEyes, headQuat, browMat, portrait);
                            locFacialY.y += YOffsetOnFace;
                        }

                        // and now the added eye parts
                        if (faceComp.HasEyePatchLeft)
                        {
                            Material leftBionicMat = faceComp.FaceMaterial.EyeLeftPatchMatAt(headFacing);
                            if (leftBionicMat != null)
                            {
                                GenDraw.DrawMeshNowOrLater(
                                    headMesh,
                                    locFacialY + offsetEyes,
                                    headQuat,
                                    leftBionicMat,
                                    portrait);
                                locFacialY.y += YOffsetOnFace;
                            }
                        }

                        if (faceComp.HasEyePatchRight)
                        {
                            Material rightBionicMat = faceComp.FaceMaterial.EyeRightPatchMatAt(headFacing);

                            if (rightBionicMat != null)
                            {
                                GenDraw.DrawMeshNowOrLater(
                                    headMesh,
                                    locFacialY + offsetEyes,
                                    headQuat,
                                    rightBionicMat,
                                    portrait);
                                locFacialY.y += YOffsetOnFace;
                            }
                        }

                        if (mouthMat != null)
                        {
                            // Mesh meshMouth = __instance.graphics.HairMeshSet.MeshAt(headFacing);
                            Mesh meshMouth = faceComp.MouthMeshSet.mesh.MeshAt(headFacing);
#if develop
                            Vector3 mouthOffset = faceComp.BaseMouthOffsetAt(headFacing);
#else
                            Vector3 mouthOffset = faceComp.MouthMeshSet.OffsetAt(headFacing);
#endif

                            Vector3 drawLoc = locFacialY + headQuat * mouthOffset;
                            GenDraw.DrawMeshNowOrLater(meshMouth, drawLoc, headQuat, mouthMat, portrait);
                            locFacialY.y += YOffsetOnFace;
                        }

                        // Portrait obviously ignores the y offset, thus render the beard after the body apparel (again)
                      //  if (!portrait)
                        {
                            DrawBeardAndTache(headFacing, portrait, faceComp, headMesh, locFacialY, headQuat);
                        }

                        // Deactivated, looks kinda crappy ATM
                        // if (pawn.Dead)
                        // {
                        // Material deadEyeMat = faceComp.DeadEyeMatAt(headFacing, bodyDrawType);
                        // if (deadEyeMat != null)
                        // {
                        // GenDraw.DrawMeshNowOrLater(mesh2, locFacialY, headQuat, deadEyeMat, portrait);
                        // locFacialY.y += YOffsetOnFace;
                        // }

                        // }
                        // else
                    }
                }

                Vector3 currentLoc = rootLoc + b;
                currentLoc.y += YOffset_OnHead;

                Mesh hairMesh = __instance.graphics.HairMeshSet.MeshAt(headFacing);

                if (!headStump)
                {
                    List<ApparelGraphicRecord> apparelGraphics = __instance.graphics.apparelGraphics;
                    List<ApparelGraphicRecord> headgearGraphics = null;
                    if (!apparelGraphics.NullOrEmpty())
                    {
                        headgearGraphics = apparelGraphics.Where(x => x.sourceApparel.def.apparel.LastLayer == ApparelLayer.Overhead)
                            .ToList();
                    }

                    bool noRenderRoofed = Controller.settings.HideHatWhileRoofed && faceComp.Roofed;
                    bool noRenderBed = Controller.settings.HideHatInBed && !renderBody;
                    bool noRenderGoggles = Controller.settings.FilterHats;

                    if (!headgearGraphics.NullOrEmpty())
                    {
                        bool filterHeadgear = portrait && Prefs.HatsOnlyOnMap || !portrait && noRenderRoofed;

                        // Draw regular hair if appparel or environment allows it (FS feature)
                        if (bodyDrawType != RotDrawMode.Dessicated)
                        {
                            // draw full or partial hair
                            bool apCoversHead = headgearGraphics.Any(
                                x => x.sourceApparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead)
                                     && !x.sourceApparel.def.apparel.hatRenderedFrontOfFace
                                     || x.sourceApparel.def.apparel.bodyPartGroups.Contains(
                                         BodyPartGroupDefOf.UpperHead)
                                     && !x.sourceApparel.def.apparel.hatRenderedFrontOfFace);

                            if (noRenderBed || filterHeadgear || !apCoversHead && noRenderGoggles)
                            {
                                Material mat = __instance.graphics.HairMatAt(headFacing);
                                GenDraw.DrawMeshNowOrLater(hairMesh, currentLoc, headQuat, mat, portrait);
                                currentLoc.y += YOffsetOnFace;
                            }
                            else if (Controller.settings.MergeHair)
                            {
                                // If not, display the hair cut
                                HairCutPawn hairPawn = CutHairDB.GetHairCache(pawn);
                                Material hairCutMat = hairPawn.HairCutMatAt(headFacing);
                                if (hairCutMat != null)
                                {
                                    GenDraw.DrawMeshNowOrLater(hairMesh, currentLoc, headQuat, hairCutMat, portrait);
                                    currentLoc.y += YOffsetOnFace;
                                }
                            }
                        }
                        else
                        {
                            filterHeadgear = false;
                        }

                        if (filterHeadgear)
                        {
                            // Filter the head gear to only show non-hats, show nothing while in bed
                            if (noRenderGoggles)
                            {
                                headgearGraphics = headgearGraphics
                                    .Where(
                                        x => !x.sourceApparel.def.apparel.bodyPartGroups.Contains(
                                                 BodyPartGroupDefOf.FullHead)
                                             && !x.sourceApparel.def.apparel.bodyPartGroups.Contains(
                                                 BodyPartGroupDefOf.UpperHead)).ToList();
                            }
                            else
                            {
                                // Clear if nothing to show
                                headgearGraphics.Clear();
                            }
                        }

                        if (noRenderBed)
                        {
                            headgearGraphics.Clear();
                        }

                        if (!headgearGraphics.NullOrEmpty())
                        {
                            for (int index = 0; index < headgearGraphics.Count; index++)
                            {
                                ApparelGraphicRecord headgearGraphic = headgearGraphics[index];
                                Material headGearMat = headgearGraphic.graphic.MatAt(headFacing);
                                headGearMat = __instance.graphics.flasher.GetDamagedMat(headGearMat);

                                Vector3 thisLoc = currentLoc;
                                if (headgearGraphic.sourceApparel.def.apparel.hatRenderedFrontOfFace)
                                {
                                    thisLoc = rootLoc + b;
                                    thisLoc.y += !(bodyFacing == Rot4.North) ? YOffset_PostHead : YOffset_Behind;
                                }

                                GenDraw.DrawMeshNowOrLater(hairMesh, thisLoc, headQuat, headGearMat, portrait);
                                currentLoc.y += YOffset_Head;
                            }
                        }
                    }
                    else
                    {
                        // Draw regular hair if no hat worn
                        if (bodyDrawType != RotDrawMode.Dessicated)
                        {
                            Material hairMat = __instance.graphics.HairMatAt(headFacing);
                            GenDraw.DrawMeshNowOrLater(hairMesh, currentLoc, headQuat, hairMat, portrait);
                        }
                    }
                }

                if (renderBody)
                {
                    for (int index = 0; index < __instance.graphics.apparelGraphics.Count; index++)
                    {
                        ApparelGraphicRecord apparelGraphicRecord = __instance.graphics.apparelGraphics[index];
                        if (apparelGraphicRecord.sourceApparel.def.apparel.LastLayer == ApparelLayer.Shell)
                        {
                            Material material3 = apparelGraphicRecord.graphic.MatAt(bodyFacing);
                            material3 = __instance.graphics.flasher.GetDamagedMat(material3);
                            GenDraw.DrawMeshNowOrLater(bodyMesh, vector, quat, material3, portrait);

                            // possible fix for phasing apparel
                            vector.y += YOffsetOnFace;
                        }
                    }
                }
            }

            // Draw the beard, for the RenderPortrait
           // if (portrait && !headStump)
           // {
           //     Vector3 b = headQuat * __instance.BaseHeadOffsetAt(headFacing);
           //     Vector3 locFacialY = a + b;
           //
           //     // no rotation wanted
           //     Mesh mesh2 = MeshPool.humanlikeHeadSet.MeshAt(headFacing);
           //
           //     DrawBeardAndTache(headFacing, portrait, faceComp, mesh2, locFacialY, headQuat);
           // }

            // ReSharper disable once InvertIf
            if (!portrait)
            {
                // Traverse.Create(__instance).Method("DrawEquipment", new object[] { rootLoc }).GetValue();
                DrawEquipmentMethodInfo?.Invoke(__instance, new object[] { rootLoc });

                if (pawn.apparel != null)
                {
                    List<Apparel> wornApparel = pawn.apparel.WornApparel;
                    foreach (Apparel ap in wornApparel)
                    {
                        ap.DrawWornExtras();
                    }
                }

                Vector3 bodyLoc = rootLoc;
                bodyLoc.y += YOffset_Status;

                ((PawnHeadOverlays)PawnHeadOverlaysFieldInfo?.GetValue(__instance))?.RenderStatusOverlays(
                    bodyLoc,
                    headQuat,
                    MeshPool.humanlikeHeadSet.MeshAt(headFacing));
            }

            return false;
        }

        private static void DrawBeardAndTache(
            Rot4 headFacing,
            bool portrait,
            [NotNull] CompFace faceComp,
            [NotNull] Mesh mesh2,
            Vector3 locFacialY,
            Quaternion headQuat)
        {
            Material beardMat = faceComp.FaceMaterial.BeardMatAt(headFacing);
            Material moustacheMatAt = faceComp.FaceMaterial.MoustacheMatAt(headFacing);

            if (beardMat != null)
            {
                GenDraw.DrawMeshNowOrLater(mesh2, locFacialY, headQuat, beardMat, portrait);
                locFacialY.y += YOffsetOnFace;
            }

            if (moustacheMatAt != null)
            {
                GenDraw.DrawMeshNowOrLater(mesh2, locFacialY, headQuat, moustacheMatAt, portrait);
                locFacialY.y += YOffsetOnFace;
            }
        }

        // Verse.PawnRenderer

        // private static readonly float[] HorMouthOffsetSex = new float[] { 0f, FS_Settings.MaleOffsetX, FS_Settings.FemaleOffsetX };
        // private static readonly float[] VerMouthOffsetSex = new float[] { 0f, FS_Settings.MaleOffsetY, FS_Settings.FemaleOffsetY };
        private static void GetReflections()
        {
            if (PawnRendererType != null)
            {
                return;
            }

            PawnRendererType = typeof(PawnRenderer);

            // PawnFieldInfo = PawnRendererType.GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
            WoundOverlayFieldInfo = PawnRendererType.GetField(
                "woundOverlays",
                BindingFlags.NonPublic | BindingFlags.Instance);
            DrawEquipmentMethodInfo = PawnRendererType.GetMethod(
                "DrawEquipment",
                BindingFlags.NonPublic | BindingFlags.Instance);
            PawnHeadOverlaysFieldInfo = PawnRendererType.GetField(
                "statusOverlays",
                BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}