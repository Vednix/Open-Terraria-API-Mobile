﻿using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;
using System.Linq;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.Npc
{
    public class NetDefaults : ModificationBase
    {
        public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"Terraria, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null",
            "TerrariaServer, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null",
            "Terraria, Version=1.3.4.4, Culture=neutral, PublicKeyToken=null"
        };
        public override string Description => "Hooking Npc.NetDefaults(int)...";
        public override void Run()
        {
            var vanilla = this.Type<Terraria.NPC>().Method("SetDefaultsFromNetId");

            int tmp = 0;
            var cbkBegin = this.Method(() => OTAPI.Callbacks.Terraria.Npc.NetDefaultsBegin(null, ref tmp));
            var cbkEnd = this.Method(() => OTAPI.Callbacks.Terraria.Npc.NetDefaultsEnd(null, ref tmp));

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
