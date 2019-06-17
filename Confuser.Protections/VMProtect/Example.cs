using System;
using System.Reflection.Emit;

namespace Crystalizer.Protections
{
    class Example
    {
        public void Method()
        {

            DynamicMethod dynamicMethod = new DynamicMethod("Method", null, null);
            ILGenerator ilgenerator = dynamicMethod.GetILGenerator();


            ilgenerator.EmitWriteLine("ahoj");
            ilgenerator.Emit(OpCodes.Ret);



            dynamicMethod.Invoke(null, null);

        }



    }
}
