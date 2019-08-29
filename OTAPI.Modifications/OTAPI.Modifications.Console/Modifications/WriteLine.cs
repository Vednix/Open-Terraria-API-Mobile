﻿using Mono.Cecil;
using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;
using System;
using System.Linq;

namespace OTAPI.Patcher.Engine.Modifications.Patches
{
	public class ConsoleWrites : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null",
			"TerrariaServer, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null"
		};
		public override string Description => "Hooking all Console.Write/Line calls...";

		public override void Run()
		{
			var redirectMethods = new[]
			{
				"Write",
				"WriteLine"
			};

			SourceDefinition.MainModule.ForEachInstruction((method, instruction) =>
			{
				var mth = instruction.Operand as MethodReference;
				if (mth != null && mth.DeclaringType.FullName == "System.Console")
				{
					if (redirectMethods.Contains(mth.Name))
					{
						var mthReference = this.Resolve(mth);
						if (mthReference != null)
							instruction.Operand = SourceDefinition.MainModule.Import(mthReference);
					}
				}
			});
		}

		MethodReference Resolve(MethodReference method)
		{
			return this.Type<OTAPI.Callbacks.Terraria.Console>()
				.Method(method.Name,
					parameters: method.Parameters,
					acceptParamObjectTypes: true
				);
		}
	}
}
