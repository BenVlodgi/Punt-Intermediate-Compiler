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
            
            return hasChanged;
        }
    }
}
