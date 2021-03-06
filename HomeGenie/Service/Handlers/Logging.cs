﻿/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using HomeGenie.Data;
using MIG;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeGenie.Service.Handlers
{
    public class Logging
    {
        private HomeGenieService _hg;
        public Logging(HomeGenieService hg)
        {
            _hg = hg;
        }

        public void ProcessRequest(MIGClientRequest request, MIGInterfaceCommand migcmd)
        {
            List<LogEntry> logs = new List<LogEntry>();
            try
            {
                switch (migcmd.command)
                {
                    case "Recent.From":
                        logs = _hg.RecentEventsLog.ToList().FindAll(le => le != null && le.Domain.StartsWith("MIG.") == false && (le.UnixTimestamp >= double.Parse(migcmd.GetOption(0))));
                        break;

                    case "Recent.Last":
                        logs = _hg.RecentEventsLog.ToList().FindAll(le => le != null && le.Domain.StartsWith("MIG.") == false &&
                             le.Timestamp > DateTime.UtcNow.AddMilliseconds(-int.Parse(migcmd.GetOption(0))));
                        //                            (DateTime.UtcNow - le.Timestamp.AddMilliseconds(int.Parse(cmd.option))).TotalMilliseconds >= 0);
                        break;
                }
            }
            catch { }
            migcmd.response = JsonConvert.SerializeObject(logs); //, Formatting.Indented);
        }

    }
}
