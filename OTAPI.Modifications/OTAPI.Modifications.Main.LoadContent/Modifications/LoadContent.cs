﻿using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.Main
{
	public class LoadContent : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null",
			"Terraria, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null",
			"Terraria, Version=1.3.4.4, Culture=neutral, PublicKeyToken=null"
		};
		public override string Description => "Hooking Game.LoadContent...";

		public override void Run()
		{
			//Grab the methods
			var vanilla = this.SourceDefinition.Type("Terraria.Main").Method("LoadContent");
			var cbkBegin = this.Method(() => OTAPI.Callbacks.Terraria.Main.LoadContentBegin(null));
			var cbkEnd = this.Method(() => OTAPI.Callbacks.Terraria.Main.LoadContentEnd(null));
			
			vanilla.Wrap
			(
				beginCallback: cbkBegin,
				endCallback: cbkEnd,
				beginIsCancellable: true,
				noEndHandling: false,
				allowCallbackInstance: true
			);
		}
	}
}
