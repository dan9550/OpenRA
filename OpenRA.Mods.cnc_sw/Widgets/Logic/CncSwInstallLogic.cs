﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 * 
 * Modified by Dan9550
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Widgets;

namespace OpenRA.Mods.CncSw.Widgets.Logic
{
    public class CncSwInstallLogic
    {
        [ObjectCreator.UseCtor]
        public CncSwInstallLogic(Widget widget, Dictionary<string, string> installData, Action continueLoading)
        {
            var panel = widget.Get("INSTALL_PANEL");
            var args = new WidgetArgs()
			{
				{ "afterInstall", () => { Ui.CloseWindow(); continueLoading(); } },
				{ "installData", installData }
			};

            panel.Get<ButtonWidget>("DOWNLOAD_BUTTON").OnClick = () =>
                Ui.OpenWindow("INSTALL_DOWNLOAD_PANEL", args);

            panel.Get<ButtonWidget>("INSTALL_BUTTON").OnClick = () =>
               Ui.OpenWindow("INSTALL_FROMCD_PANEL", new WidgetArgs(args)
				{
					{ "filesToCopy", new[] { "desestar.mix", "starmain.mix", "starmusc.mix",
											 "starvocs.mix", "starwars.mix", "tempstar.mix",
                                             "wintystar.mix", "snowstar.mix" } },
					{ "filesToExtract", new[] { "starmain.mix" } },
				});

            panel.Get<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

            panel.Get<ButtonWidget>("MODS_BUTTON").OnClick = () =>
            {
                Ui.OpenWindow("MODS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => {} },
					// Close this panel
					{ "onSwitch", Ui.CloseWindow },
				});
            };
        }
    }
}
