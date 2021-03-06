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

using OpenSource.UPnP;
using OpenSource.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MIG.Interfaces.Protocols
{
    public class UPnP : MIGInterface
    {


        #region Implemented MIG Commands
        // typesafe enum
        public sealed class Command : GatewayCommand
        {

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>()
            {
                {701, "Control.On"},
                {702, "Control.Off"},
                {705, "Control.Level"},
                {706, "Control.Toggle"},

                {801, "AvMedia.Browse"},
                {802, "AvMedia.GetUri"},
                {803, "AvMedia.SetUri"},
                {804, "AvMedia.GetTransportInfo"},
                {805, "AvMedia.GetMediaInfo"},
                {806, "AvMedia.GetPositionInfo"},

                {807, "AvMedia.Play"},
                {808, "AvMedia.Pause"},
                {809, "AvMedia.Stop"},

                {810, "AvMedia.Previous"},
                {811, "AvMedia.Next"},
                {812, "AvMedia.SetNext"},

                {813, "AvMedia.GetMute"},
                {814, "AvMedia.SetMute"},
                {815, "AvMedia.GetVolume"},
                {816, "AvMedia.SetVolume"},

            };

            // <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
            public static readonly Command CONTROL_ON = new Command(701);
            public static readonly Command CONTROL_OFF = new Command(702);
            public static readonly Command CONTROL_LEVEL = new Command(705);
            public static readonly Command CONTROL_TOGGLE = new Command(706);

            public static readonly Command AVMEDIA_BROWSE = new Command(801);
            public static readonly Command AVMEDIA_GETURI = new Command(802);
            public static readonly Command AVMEDIA_SETURI = new Command(803);
            public static readonly Command AVMEDIA_GETTRANSPORTINFO = new Command(804);
            public static readonly Command AVMEDIA_GETMEDIAINFO = new Command(805);
            public static readonly Command AVMEDIA_GETPOSITIONINFO = new Command(806);

            public static readonly Command AVMEDIA_PLAY = new Command(807);
            public static readonly Command AVMEDIA_PAUSE = new Command(808);
            public static readonly Command AVMEDIA_STOP = new Command(809);

            public static readonly Command AVMEDIA_PREVIOUS = new Command(810);
            public static readonly Command AVMEDIA_NEXT = new Command(811);
            public static readonly Command AVMEDIA_SETNEXT = new Command(812);

            public static readonly Command AVMEDIA_GETMUTE = new Command(813);
            public static readonly Command AVMEDIA_SETMUTE = new Command(814);
            public static readonly Command AVMEDIA_GETVOLUME = new Command(815);
            public static readonly Command AVMEDIA_SETVOLUME = new Command(816);

            private readonly String name;
            private readonly int value;

            private Command(int value)
            {
                this.name = CommandsList[value];
                this.value = value;
            }

            public Dictionary<int, string> ListCommands()
            {
                return Command.CommandsList;
            }

            public int Value
            {
                get { return this.value; }
            }

            public override String ToString()
            {
                return name;
            }

            public static implicit operator String(Command a)
            {
                return a.ToString();
            }

            public static explicit operator Command(int idx)
            {
                return new Command(idx);
            }

            public static explicit operator Command(string str)
            {
                if (CommandsList.ContainsValue(str))
                {
                    var cmd = from c in CommandsList where c.Value == str select c.Key;
                    return new Command(cmd.First());
                }
                else
                {
                    throw new InvalidCastException();
                }
            }
            public static bool operator ==(Command a, Command b)
            {
                return a.value == b.value;
            }
            public static bool operator !=(Command a, Command b)
            {
                return a.value != b.value;
            }
        }
        #endregion


        private UpnpSmartControlPoint _contropoint;
        private bool _isconnected = false;
        private UPnPDevice _localdevice;

        public UPnP()
        {

        }

        #region MIG Interface members

        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public string Domain
        {
            get
            {
                string ifacedomain = this.GetType().Namespace.ToString();
                ifacedomain = ifacedomain.Substring(ifacedomain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return ifacedomain;
            }
        }

        public void WaitOnPending()
        {

        }

        public bool IsConnected
        {
            get { return _isconnected; }
        }

        public bool Connect()
        {
            if (_contropoint == null)
            {
                _contropoint = new UpnpSmartControlPoint();
                _contropoint.OnAddedDevice += _contropoint_OnAddedDevice;
                _isconnected = true;
            }
            return true;

        }

        public void Disconnect()
        {
            if (_localdevice != null)
            {
                _localdevice.StopDevice();
                _localdevice = null;
            }
            if (_contropoint != null)
            {
                _contropoint.OnAddedDevice -= _contropoint_OnAddedDevice;
                _contropoint = null;
            }
            _isconnected = false;
        }

        public bool IsDevicePresent()
        {
            return true;
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string returnvalue = "";
            bool raisepropchanged = false;
            string parampath = "Status.Unhandled";
            string raiseparam = "";
            //
            UPnPDevice device = _upnpDeviceGet(request.nodeid);

            //////////////////// Commands: SwitchPower, Dimming
            if (request.command == Command.CONTROL_ON || request.command == Command.CONTROL_OFF)
            {
                bool cmdval = (request.command == Command.CONTROL_ON ? true : false);
                UPnPArgument newvalue = new UPnPArgument("newTargetValue", cmdval);
                UPnPArgument[] args = new UPnPArgument[] { 
                                    newvalue
                                };
                _upnpDeviceServiceInvoke(device, "SwitchPower", "SetTarget", args);
                //
                raisepropchanged = true;
                parampath = "Status.Level";
                raiseparam = (cmdval ? "1" : "0");
            }
            else if (request.command == Command.CONTROL_LEVEL)
            {
                UPnPArgument newvalue = new UPnPArgument("NewLoadLevelTarget", (byte)uint.Parse(request.GetOption(0)));
                UPnPArgument[] args = new UPnPArgument[] { 
                                    newvalue
                                };
                _upnpDeviceServiceInvoke(device, "Dimming", "SetLoadLevelTarget", args);
                //
                raisepropchanged = true;
                parampath = "Status.Level";
                raiseparam = (double.Parse(request.GetOption(0)) / 100d).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (request.command == Command.CONTROL_TOGGLE)
            {
            }
            //////////////////// Commands: Browse, AVTransport
            else if (request.command == Command.AVMEDIA_GETURI)
            {
                string devid = request.nodeid;
                string objid = request.GetOption(0);
                //
                UPnPArgument objectid = new UPnPArgument("ObjectID", objid);
                UPnPArgument flags = new UPnPArgument("BrowseFlag", "BrowseMetadata");
                UPnPArgument filter = new UPnPArgument("Filter", "upnp:album,upnp:artist,upnp:genre,upnp:title,res@size,res@duration,res@bitrate,res@sampleFrequency,res@bitsPerSample,res@nrAudioChannels,res@protocolInfo,res@protection,res@importUri");
                UPnPArgument startindex = new UPnPArgument("StartingIndex", (uint)0);
                UPnPArgument reqcount = new UPnPArgument("RequestedCount", (uint)1);
                UPnPArgument sortcriteria = new UPnPArgument("SortCriteria", "");
                //
                UPnPArgument result = new UPnPArgument("Result", "");
                UPnPArgument numreturned = new UPnPArgument("NumberReturned", "");
                UPnPArgument totalmatches = new UPnPArgument("TotalMatches", "");
                UPnPArgument updateid = new UPnPArgument("UpdateID", "");
                //
                _upnpDeviceServiceInvoke(device, "ContentDirectory", "Browse", new UPnPArgument[] { 
                    objectid,
                    flags,
                    filter,
                    startindex,
                    reqcount,
                    sortcriteria,
                    result,
                    numreturned,
                    totalmatches,
                    updateid
                });
                //
                try
                {
                    string ss = result.DataValue.ToString();
                    XElement item = XDocument.Parse(ss, LoadOptions.SetBaseUri).Descendants().Where(ii => ii.Name.LocalName == "item").First();
                    //
                    foreach (XElement i in item.Elements())
                    {
                        XAttribute item_protocoluri = i.Attribute("protocolInfo");
                        if (item_protocoluri != null)
                        {
                            returnvalue = i.Value;
                            break;
                        }
                    }
                }
                catch { }

            }
            else if (request.command == Command.AVMEDIA_BROWSE)
            {
                string devid = request.nodeid;
                string objid = request.GetOption(0);
                //
                UPnPArgument objectid = new UPnPArgument("ObjectID", objid);
                UPnPArgument flags = new UPnPArgument("BrowseFlag", "BrowseDirectChildren");
                UPnPArgument filter = new UPnPArgument("Filter", "upnp:album,upnp:artist,upnp:genre,upnp:title,res@size,res@duration,res@bitrate,res@sampleFrequency,res@bitsPerSample,res@nrAudioChannels,res@protocolInfo,res@protection,res@importUri");
                UPnPArgument startindex = new UPnPArgument("StartingIndex", (uint)0);
                UPnPArgument reqcount = new UPnPArgument("RequestedCount", (uint)0);
                UPnPArgument sortcriteria = new UPnPArgument("SortCriteria", "");
                //
                UPnPArgument result = new UPnPArgument("Result", "");
                UPnPArgument numreturned = new UPnPArgument("NumberReturned", "");
                UPnPArgument totalmatches = new UPnPArgument("TotalMatches", "");
                UPnPArgument updateid = new UPnPArgument("UpdateID", "");
                //
                _upnpDeviceServiceInvoke(device, "ContentDirectory", "Browse", new UPnPArgument[] { 
                    objectid,
                    flags,
                    filter,
                    startindex,
                    reqcount,
                    sortcriteria,
                    result,
                    numreturned,
                    totalmatches,
                    updateid
                });
                //
                try
                {
                    string ss = result.DataValue.ToString();
                    IEnumerable<XElement> root = XDocument.Parse(ss, LoadOptions.SetBaseUri).Elements();
                    //
                    string jsonres = "[";
                    foreach (XElement i in root.Elements())
                    {
                        string item_id = i.Attribute("id").Value;
                        string item_title = i.Descendants().Where(n => n.Name.LocalName == "title").First().Value;
                        string item_class = i.Descendants().Where(n => n.Name.LocalName == "class").First().Value;
                        jsonres += "{ \"Id\" : \"" + item_id + "\", \"Title\" : \"" + item_title + "\", \"Class\" : \"" + item_class + "\" },\n";
                    }
                    jsonres = jsonres.TrimEnd(',', '\n') + "]";
                    //
                    returnvalue = jsonres;
                }
                catch { }

            }
            else if (request.command == Command.AVMEDIA_GETTRANSPORTINFO)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument transportstate = new UPnPArgument("CurrentTransportState", "");
                UPnPArgument transportstatus = new UPnPArgument("CurrentTransportStatus", "");
                UPnPArgument currentspeed = new UPnPArgument("CurrentSpeed", "");
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    transportstate,
                                    transportstatus,
                                    currentspeed
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "GetTransportInfo", args);
                //
                string jsonres = "[{ ";
                jsonres += "\"CurrentTransportState\" : \"" + transportstate.DataValue + "\", ";
                jsonres += "\"CurrentTransportStatus\" : \"" + transportstatus.DataValue + "\", ";
                jsonres += "\"CurrentSpeed\" : \"" + currentspeed.DataValue + "\"";
                jsonres += " }]";
                //
                returnvalue = jsonres;
            }
            else if (request.command == Command.AVMEDIA_GETMEDIAINFO)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument nrtracks = new UPnPArgument("NrTracks", (uint)0);
                UPnPArgument mediaduration = new UPnPArgument("MediaDuration", "");
                UPnPArgument currenturi = new UPnPArgument("CurrentURI", "");
                UPnPArgument currenturimetadata = new UPnPArgument("CurrentURIMetaData", "");
                UPnPArgument nexturi = new UPnPArgument("NextURI", "");
                UPnPArgument nexturimetadata = new UPnPArgument("NextURIMetaData", "");
                UPnPArgument playmedium = new UPnPArgument("PlayMedium", "");
                UPnPArgument recordmedium = new UPnPArgument("RecordMedium", "");
                UPnPArgument writestatus = new UPnPArgument("WriteStatus", "");
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    nrtracks,
                                    mediaduration,
                                    currenturi,
                                    currenturimetadata,
                                    nexturi,
                                    nexturimetadata,
                                    playmedium,
                                    recordmedium,
                                    writestatus
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "GetMediaInfo", args);
                //
                string jsonres = "[{ ";
                jsonres += "\"NrTracks\" : \"" + nrtracks.DataValue + "\", ";
                jsonres += "\"MediaDuration\" : \"" + mediaduration.DataValue + "\", ";
                jsonres += "\"CurrentURI\" : \"" + currenturi.DataValue + "\", ";
                jsonres += "\"CurrentURIMetaData\" : \"" + currenturimetadata.DataValue + "\", ";
                jsonres += "\"NextURI\" : \"" + nexturi.DataValue + "\", ";
                jsonres += "\"NextURIMetaData\" : \"" + nexturimetadata.DataValue + "\", ";
                jsonres += "\"PlayMedium\" : \"" + playmedium.DataValue + "\", ";
                jsonres += "\"RecordMedium\" : \"" + recordmedium.DataValue + "\", ";
                jsonres += "\"WriteStatus\" : \"" + writestatus.DataValue + "\"";
                jsonres += " }]";
                //
                returnvalue = jsonres;
            }
            else if (request.command == Command.AVMEDIA_GETPOSITIONINFO)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument currenttrack = new UPnPArgument("Track", (uint)0);
                UPnPArgument trackduration = new UPnPArgument("TrackDuration", "");
                UPnPArgument trackmetadata = new UPnPArgument("TrackMetaData", "");
                UPnPArgument trackuri = new UPnPArgument("TrackURI", "");
                UPnPArgument reltime = new UPnPArgument("RelTime", "");
                UPnPArgument abstime = new UPnPArgument("AbsTime", "");
                UPnPArgument relcount = new UPnPArgument("RelCount", (uint)0);
                UPnPArgument abscount = new UPnPArgument("AbsCount", (uint)0);
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    currenttrack,
                                    trackduration,
                                    trackmetadata,
                                    trackuri,
                                    reltime,
                                    abstime,
                                    relcount,
                                    abscount
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "GetPositionInfo", args);
                //
                string jsonres = "[{";
                jsonres += "\"Track\" : \"" + currenttrack.DataValue + "\",";
                jsonres += "\"TrackDuration\" : \"" + trackduration.DataValue + "\",";
                jsonres += "\"TrackMetaData\" : \"" + trackmetadata.DataValue + "\",";
                jsonres += "\"TrackURI\" : \"" + trackuri.DataValue + "\",";
                jsonres += "\"RelTime\" : \"" + reltime.DataValue + "\",";
                jsonres += "\"AbsTime\" : \"" + abstime.DataValue + "\",";
                jsonres += "\"RelCount\" : \"" + relcount.DataValue + "\",";
                jsonres += "\"AbsCount\" : \"" + abscount.DataValue + "\"";
                jsonres += "}]";
                //
                returnvalue = jsonres;
            }
            else if (request.command == Command.AVMEDIA_SETURI)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument currenturi = new UPnPArgument("CurrentURI", request.GetOption(0));
                UPnPArgument urimetadata = new UPnPArgument("CurrentURIMetaData", "");
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    currenturi,
                                    urimetadata
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "SetAVTransportURI", args);
            }
            else if (request.command == Command.AVMEDIA_PLAY)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument speed = new UPnPArgument("Speed", "1");
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    speed
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "Play", args);
            }
            else if (request.command == Command.AVMEDIA_PAUSE)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "Pause", args);
            }
            else if (request.command == Command.AVMEDIA_STOP)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "Stop", args);
            }
            else if (request.command == Command.AVMEDIA_PREVIOUS)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "Previous", args);
            }
            else if (request.command == Command.AVMEDIA_NEXT)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid
                                };
                _upnpDeviceServiceInvoke(device, "AVTransport", "Next", args);
            }
            else if (request.command == Command.AVMEDIA_SETNEXT)
            {
            }
            else if (request.command == Command.AVMEDIA_GETMUTE)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument channel = new UPnPArgument("Channel", "Master");
                UPnPArgument currentmute = new UPnPArgument("CurrentMute", "");
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    channel,
                                    currentmute
                                };
                _upnpDeviceServiceInvoke(device, "RenderingControl", "GetMute", args);
                returnvalue = currentmute.DataValue.ToString();
            }
            else if (request.command == Command.AVMEDIA_SETMUTE)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument channel = new UPnPArgument("Channel", "Master");
                UPnPArgument mute = new UPnPArgument("DesiredMute", request.GetOption(0) == "1" ? true : false);
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    channel,
                                    mute
                                };
                _upnpDeviceServiceInvoke(device, "RenderingControl", "SetMute", args);
            }
            else if (request.command == Command.AVMEDIA_GETVOLUME)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument channel = new UPnPArgument("Channel", "Master");
                UPnPArgument currentvolume = new UPnPArgument("CurrentVolume", "");
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    channel,
                                    currentvolume
                                };
                _upnpDeviceServiceInvoke(device, "RenderingControl", "GetVolume", args);
                returnvalue = currentvolume.DataValue.ToString();
            }
            else if (request.command == Command.AVMEDIA_SETVOLUME)
            {
                UPnPArgument instanceid = new UPnPArgument("InstanceID", (uint)0);
                UPnPArgument channel = new UPnPArgument("Channel", "Master");
                UPnPArgument volume = new UPnPArgument("DesiredVolume", UInt16.Parse(request.GetOption(0)));
                UPnPArgument[] args = new UPnPArgument[] { 
                                    instanceid,
                                    channel,
                                    volume
                                };
                _upnpDeviceServiceInvoke(device, "RenderingControl", "SetVolume", args);
            }


            // signal event
            if (raisepropchanged && InterfacePropertyChangedAction != null)
            {
                try
                {
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = device.UniqueDeviceName, SourceType = "UPnP " + (device != null ? device.StandardDeviceType : "device"), Path = parampath, Value = raiseparam });
                }
                catch { }
            }


            return returnvalue;
        }

        #endregion



        #region non-MIGInterface public members

        public UpnpSmartControlPoint UpnpControlPoint
        {
            get { return _contropoint; }
        }

        public void CreateLocalDevice(string deviceguid, string devicetype, string presentationurl, string rootdir, string modelname, string modeldescription, string modelurl, string modelnumber, string manufacturer, string manufacturerurl)
        {
            if (_localdevice != null)
            {
                _localdevice.StopDevice();
                _localdevice = null;
            }
            //
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            _localdevice = UPnPDevice.CreateRootDevice(900, 1, rootdir);
            //hgdevice.Icon = null;
            if (presentationurl != "")
            {
                _localdevice.HasPresentation = true;
                _localdevice.PresentationURL = presentationurl;
            }
            _localdevice.FriendlyName = modelname + ": " + Environment.MachineName;
            _localdevice.Manufacturer = manufacturer;
            _localdevice.ManufacturerURL = manufacturerurl;
            _localdevice.ModelName = modelname;
            _localdevice.ModelDescription = modeldescription;
            if (Uri.IsWellFormedUriString(manufacturerurl, UriKind.Absolute))
            {
                _localdevice.ModelURL = new Uri(manufacturerurl);
            }
            _localdevice.ModelNumber = modelnumber;
            _localdevice.StandardDeviceType = devicetype;
            _localdevice.UniqueDeviceName = deviceguid;
            _localdevice.StartDevice();

        }

        #endregion




        private UPnPDevice _upnpDeviceGet(string devid)
        {
            UPnPDevice dev = null;
            foreach (UPnPDevice d in _contropoint.DeviceTable)
            {
                if (d.UniqueDeviceName == devid)
                {
                    dev = d;
                    break;
                }
            }
            return dev;
        }

        private void _upnpDeviceServiceInvoke(UPnPDevice d, string serviceid, string methodname, params UPnPArgument[] args)
        {
            foreach (UPnPService s in d.Services)
            {
                if (s.ServiceID.StartsWith("urn:upnp-org:serviceId:" + serviceid))
                {
                    s.InvokeSync(methodname, args);
                }
            }
        }

        private void _contropoint_OnAddedDevice(UpnpSmartControlPoint sender, UPnPDevice device)
        {
            if (InterfacePropertyChangedAction != null)
            {
                //foreach (UPnPService s in device.Services)
                //{
                //    s.Subscribe(1000, new UPnPService.UPnPEventSubscribeHandler(_subscribe_sink));
                //}
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() { Domain = this.Domain, SourceId = device.UniqueDeviceName, SourceType = "UPnP " + device.FriendlyName, Path = "UPnP.DeviceType", Value = device.StandardDeviceType });
            }
        }

        //        private void _subscribe_sink(UPnPService sender, bool SubscribeOK)
        //        {
        //Console.WriteLine("\n\n\n" + sender.ServiceURN + "\n\n\n");
        //            if (SubscribeOK)
        //            {
        //                sender.OnUPnPEvent += sender_OnUPnPEvent;
        //            }
        //        }

        //        void sender_OnUPnPEvent(UPnPService sender, long SEQ)
        //        {
        //Console.WriteLine("\n\n\n" + sender.ServiceURN + " - " + SEQ + "\n\n\n");
        //        }


    }













    // original code from 
    // https://code.google.com/p/phanfare-tools/
    // http://phanfare-tools.googlecode.com/svn/trunk/Phanfare.MediaServer/UPnP/Intel/UPNP/UPnPInternalSmartControlPoint.cs

    public sealed class UpnpSmartControlPoint
    {
        private ArrayList activeDeviceList = ArrayList.Synchronized(new ArrayList());
        private UPnPDeviceFactory deviceFactory = new UPnPDeviceFactory();
        private LifeTimeMonitor deviceLifeTimeClock = new LifeTimeMonitor();
        private Hashtable deviceTable = new Hashtable();
        private object deviceTableLock = new object();
        private LifeTimeMonitor deviceUpdateClock = new LifeTimeMonitor();
        private UPnPControlPoint genericControlPoint;
        private NetworkInfo hostNetworkInfo;
        private WeakEvent OnAddedDeviceEvent = new WeakEvent();
        private WeakEvent OnDeviceExpiredEvent = new WeakEvent();
        private WeakEvent OnRemovedDeviceEvent = new WeakEvent();
        private WeakEvent OnUpdatedDeviceEvent = new WeakEvent();
        private string _searchfilter = "upnp:rootdevice"; //"ssdp:all"; //


        public ArrayList DeviceTable
        {
            get { return activeDeviceList; }
        }

        public event DeviceHandler OnAddedDevice
        {
            add
            {
                this.OnAddedDeviceEvent.Register(value);
            }
            remove
            {
                this.OnAddedDeviceEvent.UnRegister(value);
            }
        }

        public event DeviceHandler OnDeviceExpired
        {
            add
            {
                this.OnDeviceExpiredEvent.Register(value);
            }
            remove
            {
                this.OnDeviceExpiredEvent.UnRegister(value);
            }
        }

        public event DeviceHandler OnRemovedDevice
        {
            add
            {
                this.OnRemovedDeviceEvent.Register(value);
            }
            remove
            {
                this.OnRemovedDeviceEvent.UnRegister(value);
            }
        }

        public event DeviceHandler OnUpdatedDevice
        {
            add
            {
                this.OnUpdatedDeviceEvent.Register(value);
            }
            remove
            {
                this.OnUpdatedDeviceEvent.UnRegister(value);
            }
        }

        public UpnpSmartControlPoint()
        {
            this.deviceFactory.OnDevice += new UPnPDeviceFactory.UPnPDeviceHandler(this.DeviceFactoryCreationSink);
            this.deviceLifeTimeClock.OnExpired += new LifeTimeMonitor.LifeTimeHandler(this.DeviceLifeTimeClockSink);
            this.deviceUpdateClock.OnExpired += new LifeTimeMonitor.LifeTimeHandler(this.DeviceUpdateClockSink);
            this.hostNetworkInfo = new NetworkInfo(new NetworkInfo.InterfaceHandler(this.NetworkInfoNewInterfaceSink));
            this.hostNetworkInfo.OnInterfaceDisabled += new NetworkInfo.InterfaceHandler(this.NetworkInfoOldInterfaceSink);
            this.genericControlPoint = new UPnPControlPoint(this.hostNetworkInfo);
            this.genericControlPoint.OnSearch += new UPnPControlPoint.SearchHandler(this.UPnPControlPointSearchSink);
            this.genericControlPoint.OnNotify += new SSDP.NotifyHandler(this.SSDPNotifySink);
            this.genericControlPoint.FindDeviceAsync(_searchfilter);
        }

        private void DeviceFactoryCreationSink(UPnPDeviceFactory sender, UPnPDevice device, Uri locationURL)
        {
            //Console.WriteLine("UPnPDevice[" + device.FriendlyName + "]@" + device.LocationURL + " advertised UDN[" + device.UniqueDeviceName + "]");
            if (!this.deviceTable.Contains(device.UniqueDeviceName))
            {
                EventLogger.Log(this, EventLogEntryType.Error, "UPnPDevice[" + device.FriendlyName + "]@" + device.LocationURL + " advertised UDN[" + device.UniqueDeviceName + "] in xml but not in SSDP");
            }
            else
            {
                lock (this.deviceTableLock)
                {
                    DeviceInfo info2 = (DeviceInfo)this.deviceTable[device.UniqueDeviceName];
                    if (info2.Device != null)
                    {
                        EventLogger.Log(this, EventLogEntryType.Information, "Unexpected UPnP Device Creation: " + device.FriendlyName + "@" + device.LocationURL);
                        return;
                    }
                    DeviceInfo info = (DeviceInfo)this.deviceTable[device.UniqueDeviceName];
                    info.Device = device;
                    this.deviceTable[device.UniqueDeviceName] = info;
                    this.deviceLifeTimeClock.Add(device.UniqueDeviceName, device.ExpirationTimeout);
                    this.activeDeviceList.Add(device);
                }
                this.OnAddedDeviceEvent.Fire(this, device);
            }
        }

        private void DeviceLifeTimeClockSink(LifeTimeMonitor sender, object obj)
        {
            DeviceInfo info;
            lock (this.deviceTableLock)
            {
                if (!this.deviceTable.ContainsKey(obj))
                {
                    return;
                }
                info = (DeviceInfo)this.deviceTable[obj];
                this.deviceTable.Remove(obj);
                this.deviceUpdateClock.Remove(obj);
                if (this.activeDeviceList.Contains(info.Device))
                {
                    this.activeDeviceList.Remove(info.Device);
                }
                else
                {
                    info.Device = null;
                }
            }
            if (info.Device != null)
            {
                //info.Device.Removed();
            }
            if (info.Device != null)
            {
                //info.Device.Removed();
                this.OnDeviceExpiredEvent.Fire(this, info.Device);
            }
        }

        private void DeviceUpdateClockSink(LifeTimeMonitor sender, object obj)
        {
            lock (this.deviceTableLock)
            {
                if (this.deviceTable.ContainsKey(obj))
                {
                    DeviceInfo info = (DeviceInfo)this.deviceTable[obj];
                    if (info.PendingBaseURL != null)
                    {
                        info.BaseURL = info.PendingBaseURL;
                        info.MaxAge = info.PendingMaxAge;
                        info.SourceEP = info.PendingSourceEP;
                        info.LocalEP = info.PendingLocalEP;
                        info.NotifyTime = DateTime.Now;
                        info.Device.UpdateDevice(info.BaseURL, info.LocalEP.Address);
                        this.deviceTable[obj] = info;
                        this.deviceLifeTimeClock.Add(info.UDN, info.MaxAge);
                    }
                }
            }
        }

        public UPnPDevice[] GetCurrentDevices()
        {
            return (UPnPDevice[])this.activeDeviceList.ToArray(typeof(UPnPDevice));
        }

        private void NetworkInfoNewInterfaceSink(NetworkInfo sender, IPAddress Intfce)
        {
            if (this.genericControlPoint != null)
            {
                this.genericControlPoint.FindDeviceAsync(_searchfilter);
            }
        }

        private void NetworkInfoOldInterfaceSink(NetworkInfo sender, IPAddress Intfce)
        {
            ArrayList list = new ArrayList();
            lock (this.deviceTableLock)
            {
                foreach (UPnPDevice device in this.GetCurrentDevices())
                {
                    if (device.InterfaceToHost.Equals(Intfce))
                    {
                        list.Add(this.UnprotectedRemoveMe(device));
                    }
                }
            }
            foreach (UPnPDevice device2 in list)
            {
                //device2.Removed();
                this.OnRemovedDeviceEvent.Fire(this, device2);
            }
            this.genericControlPoint.FindDeviceAsync(_searchfilter);
        }

        internal void RemoveMe(UPnPDevice _d)
        {
            UPnPDevice parentDevice = _d;
            UPnPDevice device2 = null;
            while (parentDevice.ParentDevice != null)
            {
                parentDevice = parentDevice.ParentDevice;
            }
            lock (this.deviceTableLock)
            {
                if (!this.deviceTable.ContainsKey(parentDevice.UniqueDeviceName))
                {
                    return;
                }
                device2 = this.UnprotectedRemoveMe(parentDevice);
            }
            if (device2 != null)
            {
                //device2.Removed();
            }
            if (device2 != null)
            {
                this.OnRemovedDeviceEvent.Fire(this, device2);
            }
        }

        public void Rescan()
        {
            lock (this.deviceTableLock)
            {
                IDictionaryEnumerator enumerator = this.deviceTable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = (string)enumerator.Key;
                    this.deviceLifeTimeClock.Add(key, 20);
                }
            }
            this.genericControlPoint.FindDeviceAsync(_searchfilter);
        }

        internal void SSDPNotifySink(IPEndPoint source, IPEndPoint local, Uri LocationURL, bool IsAlive, string USN, string SearchTarget, int MaxAge, HTTPMessage Packet)
        {
            UPnPDevice device = null;
            if (SearchTarget == _searchfilter)
            {
                if (!IsAlive)
                {
                    lock (this.deviceTableLock)
                    {
                        device = this.UnprotectedRemoveMe(USN);
                    }
                    if (device != null)
                    {
                        //device.Removed();
                    }
                    if (device != null)
                    {
                        this.OnRemovedDeviceEvent.Fire(this, device);
                    }
                }
                else
                {
                    lock (this.deviceTableLock)
                    {
                        if (!this.deviceTable.ContainsKey(USN))
                        {
                            DeviceInfo info = new DeviceInfo();
                            info.Device = null;
                            info.UDN = USN;
                            info.NotifyTime = DateTime.Now;
                            info.BaseURL = LocationURL;
                            info.MaxAge = MaxAge;
                            info.LocalEP = local;
                            info.SourceEP = source;
                            this.deviceTable[USN] = info;
                            this.deviceFactory.CreateDevice(info.BaseURL, info.MaxAge, IPAddress.Any, info.UDN);
                        }
                        else
                        {
                            DeviceInfo info2 = (DeviceInfo)this.deviceTable[USN];
                            if (info2.Device != null)
                            {
                                if (info2.BaseURL.Equals(LocationURL))
                                {
                                    this.deviceUpdateClock.Remove(info2);
                                    info2.PendingBaseURL = null;
                                    info2.PendingMaxAge = 0;
                                    info2.PendingLocalEP = null;
                                    info2.PendingSourceEP = null;
                                    info2.NotifyTime = DateTime.Now;
                                    this.deviceTable[USN] = info2;
                                    this.deviceLifeTimeClock.Add(info2.UDN, MaxAge);
                                }
                                else if (info2.NotifyTime.AddSeconds(10.0).Ticks < DateTime.Now.Ticks)
                                {
                                    info2.PendingBaseURL = LocationURL;
                                    info2.PendingMaxAge = MaxAge;
                                    info2.PendingLocalEP = local;
                                    info2.PendingSourceEP = source;
                                    this.deviceTable[USN] = info2;
                                    this.deviceUpdateClock.Add(info2.UDN, 3);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal UPnPDevice UnprotectedRemoveMe(UPnPDevice _d)
        {
            UPnPDevice parentDevice = _d;
            while (parentDevice.ParentDevice != null)
            {
                parentDevice = parentDevice.ParentDevice;
            }
            return this.UnprotectedRemoveMe(parentDevice.UniqueDeviceName);
        }

        internal UPnPDevice UnprotectedRemoveMe(string UDN)
        {
            UPnPDevice device = null;
            try
            {
                DeviceInfo info = (DeviceInfo)this.deviceTable[UDN];
                device = info.Device;
                this.deviceTable.Remove(UDN);
                this.deviceLifeTimeClock.Remove(info.UDN);
                this.deviceUpdateClock.Remove(info);
                this.activeDeviceList.Remove(device);
            }
            catch { }
            return device;
        }

        private void UPnPControlPointSearchSink(IPEndPoint source, IPEndPoint local, Uri LocationURL, string USN, string SearchTarget, int MaxAge)
        {
            lock (this.deviceTableLock)
            {
                if (!this.deviceTable.ContainsKey(USN))
                {
                    DeviceInfo info = new DeviceInfo();
                    info.Device = null;
                    info.UDN = USN;
                    info.NotifyTime = DateTime.Now;
                    info.BaseURL = LocationURL;
                    info.MaxAge = MaxAge;
                    info.LocalEP = local;
                    info.SourceEP = source;
                    this.deviceTable[USN] = info;
                    this.deviceFactory.CreateDevice(info.BaseURL, info.MaxAge, IPAddress.Any, info.UDN);
                }
                else
                {
                    DeviceInfo info2 = (DeviceInfo)this.deviceTable[USN];
                    if (info2.Device != null)
                    {
                        if (info2.BaseURL.Equals(LocationURL))
                        {
                            this.deviceUpdateClock.Remove(info2);
                            info2.PendingBaseURL = null;
                            info2.PendingMaxAge = 0;
                            info2.PendingLocalEP = null;
                            info2.PendingSourceEP = null;
                            info2.NotifyTime = DateTime.Now;
                            this.deviceTable[USN] = info2;
                            this.deviceLifeTimeClock.Add(info2.UDN, MaxAge);
                        }
                        else if (info2.NotifyTime.AddSeconds(10.0).Ticks < DateTime.Now.Ticks)
                        {
                            info2.PendingBaseURL = LocationURL;
                            info2.PendingMaxAge = MaxAge;
                            info2.PendingLocalEP = local;
                            info2.PendingSourceEP = source;
                            this.deviceUpdateClock.Add(info2.UDN, 3);
                        }
                    }
                }
            }
        }

        public delegate void DeviceHandler(UpnpSmartControlPoint sender, UPnPDevice Device);

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceInfo
        {
            public UPnPDevice Device;
            public DateTime NotifyTime;
            public string UDN;
            public Uri BaseURL;
            public int MaxAge;
            public IPEndPoint LocalEP;
            public IPEndPoint SourceEP;
            public Uri PendingBaseURL;
            public int PendingMaxAge;
            public IPEndPoint PendingLocalEP;
            public IPEndPoint PendingSourceEP;
        }
    }





}
