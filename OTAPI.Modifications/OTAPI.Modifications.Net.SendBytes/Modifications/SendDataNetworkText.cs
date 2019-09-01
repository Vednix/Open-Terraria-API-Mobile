using Mono.Cecil;
using Mono.Cecil.Cil;
using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;

using System;
using System.Linq;
using Mono.Collections.Generic;
using OTAPI.Patcher.Engine.Extensions.ILProcessor;
using Terraria;
using Terraria.Localization;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.Net
{
	[Ordered(7)]
	public class SendDataNetworkText : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null",
			"TerrariaServer, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null"
		};

		public override string Description => "Add NetworkText overloading of SendData...";

		public override void Run()
		{
			var netMessage = SourceDefinition.Type<NetMessage>();
			var sendData = netMessage.Method("SendData");
			
			// Create new method
			var overloading = new MethodDefinition(sendData.Name, sendData.Attributes, sendData.ReturnType);

			foreach (var p in sendData.Parameters)
			{
				var prm = new ParameterDefinition(p.Name, p.Attributes, p.ParameterType);
				prm.Constant = p.Constant;
				if (prm.ParameterType == SourceDefinition.MainModule.TypeSystem.String)
				{
					prm.ParameterType = Type<NetworkText>();
					prm.Constant = NetworkText.Empty;
				}
				overloading.Parameters.Add(prm);
			}
			
			// Create method body
			var ilprocessor = overloading.Body.GetILProcessor();
			ParameterDefinition text = null;
			for (int i = 0; i < overloading.Parameters.Count; i++)
			{
				ilprocessor.Append(Instruction.Create(OpCodes.Ldarg, overloading.Parameters[i]));
				if (overloading.Parameters[i].ParameterType == Type<NetworkText>())
				{
					text = overloading.Parameters[i];
					ilprocessor.Append(Instruction.Create(OpCodes.Callvirt, Type<NetworkText>().Method("ToString")));
				}
			}
			ilprocessor.Append(Instruction.Create(OpCodes.Call, sendData));
			ilprocessor.Append(Instruction.Create(OpCodes.Ret));

			foreach (var i in new []
			{
				Instruction.Create(OpCodes.Ldarg, text),
				Instruction.Create(OpCodes.Brtrue_S, overloading.Body.Instructions[0]),
				Instruction.Create(OpCodes.Ldsfld, Type<NetworkText>().Field("Empty")),
				Instruction.Create(OpCodes.Starg, text),
			}.Reverse())
			{
				ilprocessor.InsertBefore(overloading.Body.Instructions[0], i);
			}
			
			netMessage.Methods.Add(overloading);
		}
	}
}