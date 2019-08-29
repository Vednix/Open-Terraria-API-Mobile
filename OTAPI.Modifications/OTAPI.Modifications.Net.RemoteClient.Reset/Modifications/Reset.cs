﻿using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.Net.RemoteClient
{
	public class RemoteClientReset : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null",
			"TerrariaServer, Version=1.3.0.7, Culture=neutral, PublicKeyToken=null"
		};
		public override string Description => "Hooking RemoteClient.Reset...";

		public override void Run()
		{
			var vanilla = this.Method(() => (new Terraria.RemoteClient()).Reset());

			var cbkBegin = this.Method(() => OTAPI.Callbacks.Terraria.RemoteClient.PreReset(null));
			var cbkEnd = this.Method(() => OTAPI.Callbacks.Terraria.RemoteClient.PostReset(null));

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