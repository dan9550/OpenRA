﻿#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#endregion

using System;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.TS.Widgets.Logic
{
	class InstalledContentLogic
	{
		[ObjectCreator.UseCtor]
		public InstalledContentLogic(Widget widget,  Action onExit)
		{
			var panel = widget.Get("CONTENT_PANEL");

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			panel.Get<ButtonWidget>("INSTALL_BUTTON").OnClick = () =>
			{
				Ui.OpenWindow("INSTALL_FROMCD_PANEL");
			};
		}
	}
}
