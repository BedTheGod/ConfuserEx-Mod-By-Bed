using Crystalizer.Helpers;
using Crystalizer.Helpers.Generator;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

using System;

namespace Crystalizer.Protections
{
    public class VMProtect : ProtectionModule
    {

        #region ProtectionModule Settings

        string ProtectionModule.Name
        {
            get
            {
                return "VMProtect";
            }
        }
        string ProtectionModule.Description
        {
            get
            {
                return "Virtualize all methods";
            }
        }
        string ProtectionModule.Credits
        {
            get
            {
                return "Created by NeoNCoding.NET - 20.9.2018";
            }
        }
        #endregion

        #region [EventHandler] AssemblySaved
        public void AssemblySaved(string path, string aspath)
        {
            
        }
        #endregion

        #region [EventHandler] Protect
        public void Protect()
        {

            foreach (ModuleDef module in GlobalHelper.Assembly.Modules)
            {

                foreach (TypeDef t in module.Types)
                {

                    
                    foreach (MethodDef m in t.Methods)
                    {
                        System.Windows.Forms.MessageBox.Show("trying: " + m.Name);
                        if (!m.HasBody)
                        {
                            continue;
                        }


                        System.Windows.Forms.MessageBox.Show("Add: " + m.Name);

                        ILtoVIL converter = new ILtoVIL(m);
                        converter.Convert();





                    }


                }
            }

            




        }
        #endregion


    }
}
