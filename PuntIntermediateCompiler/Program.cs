using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMFParser;

namespace PuntIntermediateCompiler
{
    class Program
    {
        static private VMF vmf;
        static private VBlock world;
        static private List<VBlock> entities;
        static private List<VBlock> instances;
        static private List<VBlock> flags;
        static private List<VBlock> solids;

        static void Main(string[] args)
        {
#if DEBUG
            args = new string[] { "preview.vmf" }; //REMOVE TEST DATA WHEN DEPLOYING :P
#endif
            try
            {
                string fileName = args.FirstOrDefault();
                vmf = new VMF(File.ReadAllLines(fileName));
                if (PuntModifications())
                    File.WriteAllLines(fileName, vmf.ToVMFStrings());
            }
            catch (Exception ex)
            {
                File.WriteAllText("errors.txt", ex.ToString());
            }
        }

        internal static bool PuntModifications()
        {
            bool hasChanged = false;

            world = vmf.Body.Where(item => item.Name == "world" && item is VBlock).Cast<VBlock>().First();
            entities = vmf.Body.Where(item => item.Name == "entity").Select(item => item as VBlock).ToList();
            instances = entities.Where(entity => entity.Body.Where(item => item.Name == "classname" && (item as VProperty).Value == "func_instance").Count() > 0).ToList();
            flags = instances.Where(instance => instance.Body.Where(item => item.Name == "targetname" && (item as VProperty).Value.StartsWith("PuzzleMakerFlag_")).Count() > 0).ToList();
            solids = world.Body.Where(property => property.Name == "solid" && property is VBlock).Cast<VBlock>().ToList();

            hasChanged = hasChanged | Mod_COOPChanges();
            hasChanged = hasChanged | Mod_PTITextureChanges();

            return hasChanged;
        }

        internal static bool Mod_COOPChanges()
        {
            bool hasChanged = false;

            //If is coop, add point entity where the elevator instance is.
            var coop_exit = instances.Where(instance =>
                instance.Body.Where(property =>
                    property.Name == "file" &&
                    property.GetType() == typeof(VProperty) &&
                    ((VProperty)property).Value.EndsWith("coop_exit.vmf"))
                    .Count() == 1).FirstOrDefault();
            if (coop_exit != null)
            {
                //Then this must be coop
                var coop_exit_origin = coop_exit.Body.Where(property => property.Name == "origin" && property.GetType() == typeof(VProperty)).FirstOrDefault() as VProperty;
                if (coop_exit_origin == null)
                {
                    Console.WriteLine("We have a coop exit, with no origin?");
                    return false;
                }

                #region Swap all singleplayer instances for coop instances.
                foreach (var instance in instances)
                {
                    var file = instance.Body.FirstOrDefault(property => property.GetType() == typeof(VProperty) && property.Name == "file") as VProperty;
                    if (file.Value.EndsWith("_sp.vmf"))
                        file.Value = file.Value.Replace("_sp.vmf", "_coop.vmf");
                }
                #endregion

                hasChanged = true;
            }

            return hasChanged;
        }

        internal static bool Mod_PTITextureChanges()
        {
            bool hasChanged = false;

            var sides = solids
                .SelectMany(solid => solid.Body.Where(property => property.Name == "side" && property is VBlock))
                .Cast<VBlock>();
            var materialProperties = sides
                .SelectMany(side => side.Body.Where(property => property.Name == "material" && property is VProperty))
                .Cast<VProperty>();

            foreach(var materialProperty in materialProperties)
            {
                switch(materialProperty.Value.ToUpper())
                {
                    case "TILE/WHITE_WALL_TILE003F": materialProperty.Value = "concrete/concretewall_blue"; hasChanged = true; break;
                    case "TILE/WHITE_WALL_TILE003A": materialProperty.Value = "concrete/concretewall_blue"; hasChanged = true; break;
                    case "TILE/WHITE_FLOOR_TILE002A": materialProperty.Value = "tile/officewall_tile_b_matte"; hasChanged = true; break;
                    case "METAL/BLACK_WALL_METAL_002C": materialProperty.Value = "concrete/concretewall_red"; hasChanged = true; break;
                    case "METAL/BLACK_WALL_METAL_002B": materialProperty.Value = "concrete/concretewall_red"; hasChanged = true; break;
                    case "METAL/BLACK_FLOOR_METAL_001C": materialProperty.Value = "tile/officewall_tile_r_matte"; hasChanged = true; break;
                }
            }

            return hasChanged;
        }
    }
}
