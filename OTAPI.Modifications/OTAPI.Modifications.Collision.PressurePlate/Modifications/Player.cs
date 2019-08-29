﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Extensions.ILProcessor;
using OTAPI.Patcher.Engine.Modification;
using System.Linq;
using Terraria;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.Collision
{
	public class PlayerPressurePlate : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null",
			"TerrariaServer, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null"
		};
		public override string Description => @"Hooking MessageBuffer.GetData\PressurePlate...";

		public override void Run()
		{
			var vanilla = this.SourceDefinition.Type("Terraria.MessageBuffer").Method("GetData");
			var callback = vanilla.Module.Import(
				this.Method(() => OTAPI.Callbacks.Terraria.Collision.HitSwitch(0, 0, null))
			);
			var il = vanilla.Body.GetILProcessor();

			//Find the single HitSwitch call
			var call = vanilla.Body.Instructions.Single(x => x.OpCode == OpCodes.Call
											 && x.Operand is MethodReference
											 && (x.Operand as MethodReference).Name == "HitSwitch");

			//It's already a call, so just swap out the method reference
			call.Operand = callback;

			//Now we insert the entity argument to our custom version
			//var prmEntity = vanilla.Parameters.Single(x => x.ParameterType.Name == "IEntity");
			//il.InsertBefore(call, il.Create(OpCodes.Ldarg, prmEntity));
			il.InsertBefore(call,
				new { OpCodes.Ldsfld, Operand = this.Field(() => Main.player) },
				new { OpCodes.Ldarg_0 },
				new { OpCodes.Ldfld, Operand = this.Field(() => (new MessageBuffer()).whoAmI) },
				new { OpCodes.Ldelem_Ref }
			);

			//Skip over SetCurrentUser, then remove all instructions until the SendData call is removed
			var from = call;
			var stopAt = from
				.Next(x => x.OpCode == OpCodes.Call && (x.Operand as MethodReference).Name == "SendData").Next;
			for (;;)
			{
				il.Remove(from.Next);
				if (from.Next == stopAt) break;
			}
		}
	}
}
