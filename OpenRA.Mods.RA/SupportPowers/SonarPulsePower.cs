#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SonarPulsePowerInfo : SupportPowerInfo , ITraitInfo
	{
		public override object Create(ActorInitializer init) { return new PulseDetect(init.self.Owner); }
	}

    class PulseDetect : ISync
    {
        Player owner;
        List<Actor> actors = new List<Actor> { };

        public PulseDetect( Player owner ) { this.owner = owner; }
    }
	public class SonarPulsePower : SupportPower
	{

        List<Actor> actors = new List<Actor> { };

		public SonarPulsePower(Actor self, SonarPulsePowerInfo info) : base(self, info) { }

		public override void Activate(Actor spen, Order order)
		{
			// TODO: Reveal submarines

			// Should this play for all players?
			//Sound.Play("sonpulse.aud");
            self.World.AddFrameEndTask(w =>
            {
                Sound.PlayToPlayer(self.Owner, Info.LaunchSound);

                foreach (TraitPair<PulseDetect> i in spen.World.ActorsWithTrait<PulseDetect>())

            });
		}
	}
}
