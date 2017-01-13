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
        static private List<VBlock> entities;
        static private List<VBlock> instances;
        static private List<VBlock> flags;

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

            entities = vmf.Body.Where(item => item.Name == "entity").Select(item => item as VBlock).ToList();
            instances = entities.Where(entity => entity.Body.Where(item => item.Name == "classname" && (item as VProperty).Value == "func_instance").Count() > 0).ToList();
            flags = instances.Where(instance => instance.Body.Where(item => item.Name == "targetname" && (item as VProperty).Value.StartsWith("PuzzleMakerFlag_")).Count() > 0).ToList();

            hasChanged = Mod_COOPChanges() || hasChanged;

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
    }
}
