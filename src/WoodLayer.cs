//  Copyright 2007-2010 Portland State University, University of Wisconsin-Madison
//  Author: Robert Scheller, Melissa Lucash

using Edu.Wisc.Forest.Flel.Util;
using System;
using System.IO;
using Landis.Core;
using Landis.SpatialModeling;

namespace Landis.Extension.Succession.NECN
{
    /// <summary>
    /// </summary>
    public class WoodLayer
    {

        //---------------------------------------------------------------------------
        public static void Decompose(ActiveSite site)
        {

            double wood2c = SiteVars.SurfaceDeadWood[site].Carbon;

            double anerb = SiteVars.AnaerobicEffect[site];

            //....LARGE WOOD....
            if (wood2c > 0.0000001)
            {

                double ligninFactor = System.Math.Exp(-1 * OtherData.LigninDecayEffect * SiteVars.SurfaceDeadWood[site].FractionLignin);
                double decayRate = Math.Min(1.0, SiteVars.DecayFactor[site]
                                                * SiteVars.SurfaceDeadWood[site].DecayValue
                                                * ligninFactor
                                                * OtherData.MonthAdjust);
                
                //Compute total C flow out of large wood
                double totalCFlow = wood2c * decayRate;

                //PlugIn.ModelCore.UI.WriteLine("Decompose wood.  C={0:0.00}, Cflow={1:0.00}, DecayRate={2:0.000}.", wood2c, totalCFlow, decayRate);

                // Decompose large wood into SOM1 and SOM2 with CO2 loss.
                SiteVars.SurfaceDeadWood[site].DecomposeLignin(totalCFlow, site);
            }


                //....COARSE ROOTS (SoilDeadWood)....
            double wood3c = SiteVars.SoilDeadWood[site].Carbon;

            if (wood3c > 0.0000001)
            {
                //Compute total C flow out of coarse roots.
                double ligninEffect = System.Math.Exp(-1 * OtherData.LigninDecayEffect * SiteVars.SoilDeadWood[site].FractionLignin);
                double totalCFlow = wood3c
                                    * SiteVars.DecayFactor[site]
                                    * SiteVars.SoilDeadWood[site].DecayValue
                                    * ligninEffect
                                    * anerb
                                    * OtherData.MonthAdjust;



                //PlugIn.ModelCore.UI.WriteLine("  Coarse Root={0:0.0}, CFlow={1:0.00}, DecayFactor={2:0.00}, DecayValue={3:0.0}, ligninEffect={4:0.00}, anerb={5:0.00}.", wood3c, totalCFlow, SiteVars.DecayFactor[site], SiteVars.SoilDeadWood[site].DecayValue, ligninEffect, anerb);

                SiteVars.SoilDeadWood[site].DecomposeLignin(totalCFlow, site);
            }
        }

        public static void PartitionResidue(
                            double inputMass,
                            double inputDecayValue,
                            double inputCNratio,
                            double fracLignin,
                            LayerName name,
                            LayerType type,
                            ActiveSite site)
        {
            double directAbsorb = 0.0;
            double ratioCNtotal = 0.0;
            double totalNitrogen = 0.0;

            // from dry matter to C, 0.47 ratio
            double totalC = inputMass * 0.47;

            if (totalC < 0.0000001)
                return;

            // ...For each mineral element..
            // ...Compute amount of element in residue.
            double Npart = totalC / inputCNratio;

            //PlugIn.ModelCore.UI.WriteLine("                totalCadded={0:0.00}, inputCNratio={1}, name={2}, type={3}.", totalC, inputCNratio, name, type);

            // ...Direct absorption of mineral element by residue
            //      (mineral will be transferred to donor compartment
            //      and then partitioned into structural and metabolic
            //      using flow routines.)

            // ...If minerl(SRFC,iel) is negative then directAbsorb = zero.
            if (SiteVars.MineralN[site] <=  0.0)
                directAbsorb  = 0.0;
            else
                directAbsorb = OtherData.FractionSurfNAbsorbed
                                * SiteVars.MineralN[site]
                                * System.Math.Max(totalC / OtherData.ResidueMaxDirectAbsorb, 1.0);


            // ...If C/N ratio is too low, transfer just enough to make
            //       C/N of residue = damrmn
            if (Npart + directAbsorb  <= 0.0)
                ratioCNtotal = 0.0;
            else
                ratioCNtotal = totalC / (Npart + directAbsorb);

            if (ratioCNtotal < OtherData.MinResidueCN )
                directAbsorb  = (totalC / OtherData.MinResidueCN) - Npart;

            if (directAbsorb  < 0.0)
                directAbsorb  = 0.0;

            if(directAbsorb > SiteVars.MineralN[site])
                directAbsorb = SiteVars.MineralN[site];

            SiteVars.MineralN[site] -= directAbsorb;

            totalNitrogen = directAbsorb + Npart;

            //PlugIn.ModelCore.UI.WriteLine("                totalNadded={0:0.00}, totalC={1:0.0}, inputCNratio={2}, name={3}, type={4}.", totalNitrogen, totalC, inputCNratio, name, type);


            if ((int)name == (int)LayerName.Wood)
            {
                SiteVars.SurfaceDeadWood[site].Carbon += totalC;
                SiteVars.SurfaceDeadWood[site].Nitrogen += totalNitrogen;
                SiteVars.SurfaceDeadWood[site].AdjustLignin(totalC, fracLignin);
                SiteVars.SurfaceDeadWood[site].AdjustDecayRate(totalC, inputDecayValue);
            }
            else  // Dead Coarse Roots
            {
                SiteVars.SoilDeadWood[site].Carbon += totalC;
                SiteVars.SoilDeadWood[site].Nitrogen += totalNitrogen;
                SiteVars.SoilDeadWood[site].AdjustLignin(totalC, fracLignin);
                SiteVars.SoilDeadWood[site].AdjustDecayRate(totalC, inputDecayValue);
            }

                        return;

        }

    }
}
