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

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Linq;

namespace ZWaveLib.Devices
{

    public enum ParameterType
    {
        PARAMETER_BASIC,
        PARAMETER_ZWAVE_MANUFACTURER_SPECIFIC,
        PARAMETER_GENERIC,
        PARAMETER_WATTS,
        PARAMETER_TEMPERATURE,
        PARAMETER_HUMIDITY,
        PARAMETER_LUMINANCE,
        PARAMETER_MOTION,
        PARAMETER_ALARM_DOORWINDOW,
        PARAMETER_ALARM_GENERIC,
        PARAMETER_ALARM_SMOKE,
        PARAMETER_ALARM_CARBONMONOXIDE,
        PARAMETER_ALARM_CARBONDIOXIDE,
        PARAMETER_ALARM_HEAT,
        PARAMETER_ALARM_FLOOD,
        PARAMETER_ALARM_TAMPERED,
        PARAMETER_CONFIG,
        PARAMETER_WAKEUP_INTERVAL,
        PARAMETER_WAKEUP_NOTIFY,
        PARAMETER_ASSOC,
        PARAMETER_BATTERY,
        PARAMETER_NODE_INFO,
        PARAMETER_MULTIINSTANCE_SWITCH_BINARY_COUNT,
        PARAMETER_MULTIINSTANCE_SWITCH_BINARY,
        PARAMETER_MULTIINSTANCE_SWITCH_MULTILEVEL_COUNT,
        PARAMETER_MULTIINSTANCE_SWITCH_MULTILEVEL,
        PARAMETER_MULTIINSTANCE_SENSOR_BINARY_COUNT,
        PARAMETER_MULTIINSTANCE_SENSOR_BINARY,
        PARAMETER_MULTIINSTANCE_SENSOR_MULTILEVEL_COUNT,
        PARAMETER_MULTIINSTANCE_SENSOR_MULTILEVEL
    }

    public enum ZWaveSensorAlarmParameter
    {
        GENERIC = 0,
        SMOKE,
        CARBONMONOXIDE,
        CARBONDIOXIDE,
        HEAT,
        FLOOD
    }

    public enum ZWaveSensorParameter
    {
        UNKNOWN = -1,
        TEMPERATURE = 1,
        GENERAL_PURPOSE_VALUE = 2,
        LUMINANCE = 3,
        POWER = 4,
        RELATIVE_HUMIDITY = 5,
        VELOCITY = 6,
        DIRECTION = 7,
        ATMOSPHERIC_PRESSURE = 8,
        BAROMETRIC_PRESSURE = 9,
        SOLAR_RADIATION = 10,
        DEW_POINT = 11,
        RAIN_RATE = 12,
        TIDE_LEVEL = 13,
        WEIGHT = 14,
        VOLTAGE = 15,
        CURRENT = 16,
        CO2_LEVEL = 17,
        AIR_FLOW = 18,
        TANK_CAPACITY = 19,
        DISTANCE = 20,
        ANGLE_POSITION = 21
    }

    public class UpdateNodeParameterEventArgs
    {
        public int NodeId { get; internal set; }
        public int ParameterId { get; internal set; }
        public ParameterType ParameterType { get; internal set; }
        public object Value { get; internal set; }
    }

    public class ManufacturerSpecific
    {
        public string ManufacturerId { get; set; }
        public string TypeId { get; set; }
        public string ProductId { get; set; }
    }


    public class ManufacturerSpecificResponseEventArg
    {
        public int NodeId { get; internal set; }
        public ManufacturerSpecific ManufacturerSpecific;
    }


    public class ZWaveNode
    {
        public byte NodeId { get; protected set; }
        //
        public string ManufacturerId { get; protected set; }
        public string TypeId { get; protected set; }
        public string ProductId { get; protected set; }
        //
        public byte BasicClass { get; internal set; }
        public byte GenericClass { get; internal set; }
        public byte SpecificClass { get; internal set; }
        //
        public IZWaveDeviceHandler DeviceHandler = null;

        public delegate void UpdateNodeParameterEventHandler(object sender, UpdateNodeParameterEventArgs upargs);
        public event UpdateNodeParameterEventHandler UpdateNodeParameter;

        public delegate void ManufacturerSpecificResponseEventHandler(object sender, ManufacturerSpecificResponseEventArg mfargs);
        public virtual event ManufacturerSpecificResponseEventHandler ManufacturerSpecificResponse;



        internal ZWavePort zp;

		private object _cblock = new object();
		private DateTime _lastmanufacturerget = DateTime.Now;
        private Dictionary<byte, int> _nodeconfigparamslen = new Dictionary<byte, int>();

		public ZWaveNode(byte nodeId, ZWavePort zp)
        {
            this.NodeId = nodeId;
            this.zp = zp;
        }


        public ZWaveNode(byte nodeId, ZWavePort zp, byte genericType)
        {
            this.NodeId = nodeId;
            this.zp = zp;
            this.GenericClass = genericType;
        }

        internal byte SendMessage(byte[] message, bool disablecallback = false)
        {
            ZWaveMessage zmsg = new ZWaveMessage() { Node = this, Message = message };
            return zp.SendMessage(zmsg, disablecallback);
        }

        public void SetDeviceHandlerFromName(string fullname)
        {
            Type type = Assembly.GetExecutingAssembly().GetType(fullname); // full name - i.e. with namespace (perhaps concatenate)
            try
            {
                IZWaveDeviceHandler devhandler = (IZWaveDeviceHandler)Activator.CreateInstance(type);
                //
                this.DeviceHandler = devhandler;
                this.DeviceHandler.SetNodeHost(this);
            }
            catch (Exception ex)
            {
                // TODO: add error logging 
            }
        }


        public virtual bool MessageRequestHandler(byte[] receivedMessage)
        {
//Console.WriteLine("\n   _z_ [" + this.NodeId + "]  " + (this.DeviceHandler != null ? this.DeviceHandler.ToString() : "!" + this.GenericClass.ToString()));
//Console.WriteLine("   >>> " + zp.ByteArrayToString(receivedMessage) + "\n");
            // Do nothing
            //if (this.NodeId != 1 && (this.TypeId == null || this.DeviceHandler == null))
            //{
            //    TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - _lastmanufacturerget.Ticks);
            //    if (ts.TotalSeconds > 10)
            //    {
            //        _lastmanufacturerget = DateTime.Now;
            //        ManufacturerSpecific_Get();
            //    } 
            //}
            //
            bool handled = false;
            int message_length = receivedMessage.Length;
            //
            if (this.DeviceHandler != null && this.DeviceHandler.HandleRawMessageRequest(receivedMessage))
            {
                handled = true;
            }
            //
            if (!handled && message_length > 8)
            {

                byte command_length = receivedMessage[6]; 
                byte command_class = receivedMessage[7];
                byte command_type = receivedMessage[8]; // is this the Payload length in bytes? or is it the command type?
                //
                switch (command_class)
                {
////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case (byte)CommandClass.COMMAND_CLASS_BASIC:
                    case (byte)CommandClass.COMMAND_CLASS_ALARM:
                    case (byte)CommandClass.COMMAND_CLASS_SENSOR_BINARY:
                    case (byte)CommandClass.COMMAND_CLASS_SENSOR_ALARM:
                    case (byte)CommandClass.COMMAND_CLASS_SENSOR_MULTILEVEL:
					case (byte)CommandClass.COMMAND_CLASS_SWITCH_BINARY:
					case (byte)CommandClass.COMMAND_CLASS_SWITCH_MULTILEVEL:
                    case (byte)CommandClass.COMMAND_CLASS_SCENE_ACTIVATION:
////////////////////////////////////////////////////////////////////////////////////////////////////////////

                        //if (command_type == (byte)Command.COMMAND_BASIC_REPORT || command_type == (byte)Command.COMMAND_BASIC_GET) // command_type should be PayLoad length !?!?
                        //{
                        if (this.DeviceHandler != null)
                        {
                            handled = this.DeviceHandler.HandleBasicReport(receivedMessage);
                        }
                        //}
                        break;

////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case (byte)CommandClass.COMMAND_CLASS_COONFIGURATION:
////////////////////////////////////////////////////////////////////////////////////////////////////////////
//                        01 0B 00 04 00 2B 05 70 06 02 01 01 AA

                        if (message_length > 11 && command_type == (byte)Command.COMMAND_CONFIG_REPORT) // CONFIGURATION PARAMETER REPORT  0x06
                        {
                            byte paramid = receivedMessage[9];
                            byte paramlen = receivedMessage[10];
                            //
                            if (!_nodeconfigparamslen.ContainsKey(paramid))
                            {
                                _nodeconfigparamslen.Add(paramid, paramlen);
                            }
                            else
                            {
                                // this shouldn't change on read... but you never know! =)
                                _nodeconfigparamslen[paramid] = paramlen;
                            }
                            //
                            byte[] bval = new byte[4];
                            // extract bytes value
                            Array.Copy(receivedMessage, 11, bval, 4 - (int)paramlen, (int)paramlen);
                            uint paramval = bval[0];
                            Array.Reverse(bval);
                            paramval = BitConverter.ToUInt32(bval, 0);
                            // convert it to uint
                            //
                            _raiseUpdateParameterEvent(this, paramid, ParameterType.PARAMETER_CONFIG, paramval);
                            //
                            handled = true;
                        }

                        break;

////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case (byte)CommandClass.COMMAND_CLASS_ASSOCIATION:
////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        //01 0C 00 04 00 2B 06 85 03 01 04 00 01 58

                        if (message_length > 12 && command_type == (byte)Command.COMMAND_ASSOCIATION_REPORT) // ASSOCIATION REPORT 0x03
                        {
                            byte groupid = receivedMessage[9];
                            byte maxassoc = receivedMessage[10];
                            byte numassoc = receivedMessage[11]; // it is always zero ?!?
                            byte nodeassoc = 0;
                            string assocnodes = "";
                            if (receivedMessage.Length > 13)
                            {
                                for (int a = 12; a < receivedMessage.Length - 1; a++)
                                {
                                    assocnodes += receivedMessage[a] + ",";
                                }
                            }
                            assocnodes = assocnodes.TrimEnd(',');
                            //
                            //_raiseUpdateParameterEvent(this, 0, ParameterType.PARAMETER_ASSOC, groupid);
                            _raiseUpdateParameterEvent(this, 1, ParameterType.PARAMETER_ASSOC, maxassoc);
                            _raiseUpdateParameterEvent(this, 2, ParameterType.PARAMETER_ASSOC, numassoc);
                            _raiseUpdateParameterEvent(this, 3, ParameterType.PARAMETER_ASSOC, groupid + ":" + assocnodes);
                            //
                            handled = true;
                        }

                        break;

////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case (byte)CommandClass.COMMAND_CLASS_WAKE_UP:
////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        //01 0C 00 04 00 2B 06 84 06 00 01 68 65 54

                        if (message_length > 11 && command_type == (byte)Command.COMMAND_WAKEUP_REPORT) // WAKE UP REPORT 0x06
                        {
                            uint interval = ((uint)receivedMessage[9]) << 16;
                            interval |= (((uint)receivedMessage[10]) << 8);
                            interval |= (uint)receivedMessage[11];
                            //
                            _raiseUpdateParameterEvent(this, 0, ParameterType.PARAMETER_WAKEUP_INTERVAL, interval);
                            //
                            handled = true;
                        }
                        // 0x01, 0x08, 0x00, 0x04, 0x00, 0x06, 0x02, 0x84, 0x07, 0x74
                        else if (message_length > 7 && command_type == (byte)Command.COMMAND_WAKEUP_NOTIFICATION) // AWAKE NOTIFICATION 0x07
                        {
                            _raiseUpdateParameterEvent(this, 0, ParameterType.PARAMETER_WAKEUP_NOTIFY, 1);
                            //
                            handled = true;
                        }

                        break;

////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case (byte)CommandClass.COMMAND_CLASS_BATTERY:
////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        // 01 0F 00 04 00 2B 09 80 03 64 31 05 01 2A 03 0B 26
                        if (message_length > 7 && /*command_length == (byte)Command.COMMAND_BASIC_REPORT && */ command_type == 0x03) // Battery Report
                        {
                            _raiseUpdateParameterEvent(this, 0, ParameterType.PARAMETER_BATTERY, receivedMessage[9]);
                            //
                            handled = true;
                        }

                        break;

////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case (byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE:
                    case (byte)CommandClass.COMMAND_CLASS_METER:
////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        
                        if (message_length > 10)
                        {
                            if (this.DeviceHandler != null)
                            {
                                handled = this.DeviceHandler.HandleMultiInstanceReport(receivedMessage);
                            }
                        }
                        
                        break;

////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    case (byte)CommandClass.COMMAND_CLASS_MANUFACTURER_SPECIFIC:
////////////////////////////////////////////////////////////////////////////////////////////////////////////
                        if (message_length > 14)
                        {
                            byte[] manufacturerid = new byte[2] { receivedMessage[9], receivedMessage[10] };
                            byte[] typeid = new byte[2] { receivedMessage[11], receivedMessage[12] };
                            byte[] productid = new byte[2] { receivedMessage[13], receivedMessage[14] };

                            this.ManufacturerId = zp.ByteArrayToString(manufacturerid).Replace(" ", "");
                            this.TypeId = zp.ByteArrayToString(typeid).Replace(" ", "");
                            this.ProductId = zp.ByteArrayToString(productid).Replace(" ", "");

                            ManufacturerSpecific manufacturerspecs = new ManufacturerSpecific()
                            {
                                TypeId = zp.ByteArrayToString(typeid).Replace(" ", ""),
                                ProductId = zp.ByteArrayToString(productid).Replace(" ", ""),
                                ManufacturerId = zp.ByteArrayToString(manufacturerid).Replace(" ", "")
                            };
                            //
                            _deviceHandlerCheck(manufacturerspecs);
                            //
                            if (ManufacturerSpecificResponse != null)
                            {
                                try
                                {
                                    ManufacturerSpecificResponse(this, new ManufacturerSpecificResponseEventArg()
                                    {
                                        NodeId = this.NodeId,
                                        ManufacturerSpecific = manufacturerspecs
                                    });
                                }
                                catch (Exception ex) {

                                    Console.WriteLine("ZWaveLib: Error during ManufacturerSpecificResponse callback, " + ex.Message + "\n" + ex.StackTrace);
                                
                                }
                            }
                            //
                            handled = true;
							//
//Console.WriteLine (" ########################################################################################################### ");
//this.SendMessage (new byte[] { 0x01, 0x09, 0x00, 0x13, 0x13, this.NodeId, 0x31, 0x01, 0x25, 0x40, 0xa1 });
                        }

                        break;

                }

            }
            //
            if (!handled && message_length > 3)
            {
                //if (receivedMessage[3] == 0x13)
                //{
                //    // Check the callback id
                //    if (callbackIds.ContainsKey(receivedMessage[4]))
                //    {
                //        MessagingCompleted(receivedMessage[4]);
                //    }
                //    return true;
                //}
                //else
                if (receivedMessage[3] != 0x13)
                {
                    bool log = true;
                    if (message_length > 7 && /* cmd_class */ receivedMessage[7] == (byte)CommandClass.COMMAND_CLASS_MANUFACTURER_SPECIFIC) log = false;
if (log)
Console.WriteLine( "ZWaveLib UNHANDLED message: " + zp.ByteArrayToString(receivedMessage) );
                }
            }

            return false;
        }








        #region private methods

        private List<Type> _getTypesInNamespace(Assembly assembly, string nameSpace)
        {
            List<Type> tlist = new List<Type>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Namespace != null && type.Namespace.StartsWith(nameSpace))
                    tlist.Add(type);
            }
            //return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
            return tlist;
        }

        internal virtual void _raiseUpdateParameterEvent(ZWaveNode node, int pid, ParameterType peventtype,  object value)
        {
            if (UpdateNodeParameter != null)
            {
                UpdateNodeParameter(node, new UpdateNodeParameterEventArgs() { NodeId = (int)node.NodeId, ParameterId = pid, ParameterType = peventtype, Value = value });
            }
        }

        private void _deviceHandlerCheck(ManufacturerSpecific manufacturerspecs)
        {
            //if (this.DeviceHandler == null)
            {
                List<Type> typelist = _getTypesInNamespace(Assembly.GetExecutingAssembly(), "ZWaveLib.Devices.ProductHandlers.");
                for (int i = 0; i < typelist.Count; i++)
                {

                    //Console.WriteLine(typelist[i].FullName);
                    Type type = Assembly.GetExecutingAssembly().GetType(typelist[i].FullName); // full name - i.e. with namespace (perhaps concatenate)
                    try
                    {
                        IZWaveDeviceHandler devhandler = (IZWaveDeviceHandler)Activator.CreateInstance(type);
                        //
                        if (devhandler.CanHandleProduct(manufacturerspecs))
                        {
                            this.DeviceHandler = devhandler;
                            this.DeviceHandler.SetNodeHost(this);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // TODO: add error logging 
//                                        Console.WriteLine("ERROR!!!!!!! " + ex.Message + " : " + ex.StackTrace);
                    }
                }
            }
            
        }


        public void SetGenericHandler()
        {
            if (this.DeviceHandler == null)
            {
                //No specific devicehandler could be found. Use a generic handler
                IZWaveDeviceHandler devhandler = null;
                switch (this.GenericClass)
                {
                    case 0x00:
                        // need to query node capabilities
                        byte[] message = new byte[] { 0x01, 0x04, 0x00, (byte)Controller.ZWaveCommandClass.CMD_GET_NODE_PROTOCOL_INFO, this.NodeId, 0x00 };
                        SendMessage(message);
                        break;
                    case (byte)ZWaveLib.GenericType.SWITCH_BINARY:
                        devhandler = new ProductHandlers.Generic.Switch();
                        break;
                    case (byte)ZWaveLib.GenericType.SWITCH_MULTILEVEL: // eg. dimmer
                        devhandler = new ProductHandlers.Generic.Dimmer();
                        break;
                    case (byte)ZWaveLib.GenericType.THERMOSTAT:
                        devhandler = new ProductHandlers.Generic.Thermostat();
                        break;
                    // Fallback to generic Sensor driver if type is not directly supported.
                    // The Generic.Sensor handler is currently used as some kind of multi-purpose driver 
                    default:
                        devhandler = new ProductHandlers.Generic.Sensor();
                        break;
                }
                if (devhandler != null)
                {
                    this.DeviceHandler = devhandler;
                    this.DeviceHandler.SetNodeHost(this);
                }
            }
        }



        #endregion









        #region ZWave Command Class Basic

        /// <summary>
        /// Basic Set
        /// </summary>
        /// <param name="value"></param>
        public virtual void Basic_Set(int value)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_BASIC, 
				(byte)Command.COMMAND_BASIC_SET, 
				byte.Parse(value.ToString())
			});
        }

        /// <summary>
        /// Basic Get
        /// </summary>
        public virtual void Basic_Get()
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_BASIC, 
				(byte)Command.COMMAND_BASIC_GET 
			});
        }

        #endregion

        #region ZWave Command Class MultiInstance/Channel

        public virtual void MultiInstance_GetCount(byte command_class) // eg. CommandClass.COMMAND_CLASS_SWITCH_BINARY
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE, 
                (byte)Command.COMMAND_MULTIINSTANCE_COUNT_GET, // 0x04 = GET, 0x05 = REPORT
                command_class
			});
        }

        public virtual void MultiInstance_SwitchBinaryGet(byte instance)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE, 
                0x0d, // ??
                0x00, // ??
                instance,
                (byte)CommandClass.COMMAND_CLASS_SWITCH_BINARY,
                (byte)Command.COMMAND_MULTIINSTANCE_GET
			});
        }

        public virtual void MultiInstance_SwitchBinarySet(byte instance, int value)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE, 
                0x0d, // 
                0x00, // ??
                instance,
                (byte)CommandClass.COMMAND_CLASS_SWITCH_BINARY,
                (byte)Command.COMMAND_MULTIINSTANCE_SET,
                byte.Parse(value.ToString())
			});
        }

        public virtual void MultiInstance_SwitchMultiLevelGet(byte instance)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE, 
                0x0d, // ??
                0x00, // ??
                instance,
                (byte)CommandClass.COMMAND_CLASS_SWITCH_MULTILEVEL,
                (byte)Command.COMMAND_MULTIINSTANCE_GET
			});
        }

        public virtual void MultiInstance_SwitchMultiLevelSet(byte instance, int value)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE, 
                0x0d, // 
                0x00, // ??
                instance,
                (byte)CommandClass.COMMAND_CLASS_SWITCH_MULTILEVEL,
                (byte)Command.COMMAND_MULTIINSTANCE_SET,
                byte.Parse(value.ToString())
			});
        }

        public virtual void MultiInstance_SensorBinaryGet(byte instance)
        {
            // 0x01, 0x0C, 0x00, 0x13, node, 0x05, 0x60, 0x06,       0x01, 0x31, 0x04, 0x05, 0x03, 0x00
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE, 
                0x06, // ??
                instance,
                (byte)CommandClass.COMMAND_CLASS_SENSOR_BINARY,
                0x04 //
			});
        }

        public virtual void MultiInstance_SensorMultiLevelGet(byte instance)
        {
            // 0x01, 0x0C, 0x00, 0x13, node, 0x05, 0x60, 0x06,       0x01, 0x31, 0x04, 0x05, 0x03, 0x00
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_MULTIINSTANCE, 
                0x06, // ??
                instance,
                (byte)CommandClass.COMMAND_CLASS_SENSOR_MULTILEVEL,
                0x04 //
			});
        }

        #endregion

        #region ZWave Command Class Association

        /// <summary>
        /// Association Set
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="targetnodeid"></param>
        public virtual void Association_Set(byte groupid, byte targetnodeid)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_ASSOCIATION, 
				(byte)Command.COMMAND_ASSOCIATION_SET, 
				groupid, 
				targetnodeid 
			});
        }

        /// <summary>
        /// Association Get
        /// </summary>
        /// <param name="groupid"></param>
        public virtual void Association_Get(byte groupid)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_ASSOCIATION, 
				(byte)Command.COMMAND_ASSOCIATION_GET, 
				groupid 
			});
        }

        /// <summary>
        /// Association Remove
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="targetnodeid"></param>
        public virtual void Association_Remove(byte groupid, byte targetnodeid)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_ASSOCIATION, 
				(byte)Command.COMMAND_ASSOCIATION_REMOVE, 
				groupid, 
				targetnodeid 
			});
        }

        /// <summary>
        /// Association Grouping Get
        /// </summary>
        public virtual void Association_GroupingsGet()
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_ASSOCIATION,
				(byte)Command.COMMAND_CONFIG_GET 
			});
        }

        #endregion

        #region ZWave Command Class Battery

        public virtual void Battery_Get()
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_BATTERY, 
				(byte)Command.COMMAND_BASIC_GET 
			});
        }

        #endregion

        #region ZWave Command Class Wake Up
        public virtual void WakeUpGetInterval()
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_WAKE_UP, 
				(byte)Command.COMMAND_CONFIG_GET 
			});
        }
		
        public virtual void WakeUpSetInterval(uint interval)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_WAKE_UP, 
				(byte)Command.COMMAND_CONFIG_SET,
                (byte)((interval >> 16) & 0xff),
                (byte)((interval >> 8) & 0xff),
                (byte)((interval) & 0xff),
				0x01
			});
        }
        #endregion

        #region ZWave Command Class Configuration

        public virtual void ConfigParameterSet(byte parameter, Int32 intvalue)
        {
            int valuelen = 1;
            if (!_nodeconfigparamslen.ContainsKey(parameter))
            {
                ConfigParameterGet(parameter);
                int retries = 0;
                while (!_nodeconfigparamslen.ContainsKey(parameter) && retries++ <= 5)
                {
                    Thread.Sleep(1000);
                }
            }
            if (_nodeconfigparamslen.ContainsKey(parameter))
            {
                valuelen = _nodeconfigparamslen[parameter];
            }
//Console.WriteLine("GOT Parameter Length: " + valuelen);
            //
//            byte[] value = new byte[valuelen]; // { (byte)intvalue };//BitConverter.GetBytes(Int32.Parse(intvalue));
            byte[] value32 = BitConverter.GetBytes(intvalue);
            Array.Reverse(value32);
            //int curbyte = valuelen - 1;
            //for (int x = 0; x < value32.Length && curbyte >= 0; x++)
            //{
            //    value[curbyte--] = value32[x];
            //}
            ////if (value32[0] != 0 && valuelen > 1)
            ////{
            ////    value[0] = value32[0];
            ////}
            ////
//Console.WriteLine("\n\n\nCOMPUTED VALUE: " + zp.ByteArrayToString(value32) + "\n->" + zp.ByteArrayToString(BitConverter.GetBytes(intvalue)) + "\n\n");
            //
            byte[] msg = new byte[4 + valuelen];
            msg[0] = (byte)CommandClass.COMMAND_CLASS_COONFIGURATION;
			msg[1] = (byte)Command.COMMAND_CONFIG_SET;
            msg[2] = parameter;
            msg[3] = (byte)valuelen;
            switch (valuelen)
            {
                case 1:
                    Array.Copy(value32, 3, msg, 4, 1);
                    break;
                case 2:
                    Array.Copy(value32, 2, msg, 4, 2);
                    break;
                case 4:
                    Array.Copy(value32, 0, msg, 4, 4);
                    break;
            }
            this.ZWaveMessage(msg);
        }

        public virtual void ConfigParameterGet(byte parameter)
        {
            this.ZWaveMessage(new byte[] { 
				(byte)CommandClass.COMMAND_CLASS_COONFIGURATION, 
				(byte)Command.COMMAND_CONFIG_GET,
                parameter
			});
        }

        #endregion






        public virtual void ManufacturerSpecific_Get()
        {
            byte[] message = new byte[] { 0x01 /* Start Of Frame */, 0x09 /*packet len */, 0x00 /* type req/res */, 0x13 /* func send data */, this.NodeId, 0x02, 0x72 /* class manuf spec */, 0x04 /* get */, 0x05 /* report */, 0x01 | 0x04, 0x00 }; //, 0x05, 0x00, 0x00 };
            SendMessage(message);
        }
     







        public virtual void RequestMeterReport()
        {
            byte[] message = new byte[] { 0x01 /* Start Of Frame */, 0x09 /*packet len */, 0x00 /* type req/res */, 0x13 /* func send data */, this.NodeId, 0x02, 0x32 /* class meter */, 0x04 /* get */, 0x05 /* report */, 0x01 | 0x04, 0x00 }; //, 0x05, 0x00, 0x00 };
            SendMessage(message);
        }
     
        public void RequestMultiLevelReport()
        {
             //SwitchMultilevelCmd_Get 
             //   0x01,0x09, 0x00, 0x13, <nodeid>, 0x02, 0x26, 0x02, 0x05 //, 0x1d, 0xd9
             byte[] message = new byte[] { 0x01, 0x09, 0x00, 0x13, this.NodeId, 0x02, 0x26, 0x02, 0x05, 0x00, 0x00 };
             SendMessage(message);

             Thread.Sleep(200);

             //MultiChannel Encapsulated (instance=1):SwitchMultilevelCmd_Get 
             //   0x01, 0x0d, 0x00, 0x13, <nodeid>, 0x06, 0x60, 0x0d, 0x00, 0x01, 0x26, 0x02, 0x05 //, 0x1e, 0xb5
             message = new byte[] { 0x01, 0x0C, 0x00, 0x13, this.NodeId, 0x05, 0x60, 0x06, 0x01, 0x31, 0x04, 0x05, 0x03, 0x00 };
             SendMessage(message);

             Thread.Sleep(200);

             //MultiChannel Encapsulated (instance=2):SwitchMultilevelCmd_Get 
             //   0x01, 0x0d, 0x00, 0x13, <nodeid>, 0x06, 0x60, 0x0d, 0x00, 0x02, 0x26, 0x02, 0x05 //, 0x1e, 0xb5
             message = new byte[] { 0x01, 0x0C, 0x00, 0x13, this.NodeId, 0x05, 0x60, 0x06, 0x02, 0x31, 0x04, 0x05, 0x03, 0x00 };
             SendMessage(message);

             Thread.Sleep(200);

             //MultiChannel Encapsulated (instance=3):SwitchMultilevelCmd_Get 
             message = new byte[] { 0x01, 0x0C, 0x00, 0x13, this.NodeId, 0x05, 0x60, 0x06, 0x03, 0x31, 0x04, 0x05, 0x03, 0x00 }; //, 0x05, 0x00, 0x00 };
             SendMessage(message);

             Thread.Sleep(200);
        }


		
		
		
		
		
		
		









        public virtual void ZWaveMessage(byte[] msg)
        {
            byte[] header = new byte[] { 0x01 /* Start Of Frame */, (byte)(msg.Length + 7) /*packet len */, 0x00 /* type req/res */, 0x13 /* func send data */, this.NodeId, (byte)(msg.Length) };
            byte[] footer = new byte[] { 0x01 | 0x04, 0x00, 0x00 };
            byte[] message = new byte[header.Length + msg.Length + footer.Length];// { 0x01 /* Start Of Frame */, 0x09 /*packet len */, 0x00 /* type req/res */, 0x13 /* func send data */, this.NodeId, 0x02, 0x31, 0x04, 0x01 | 0x04, 0x00, 0x00 };

            //    byte[] tmp = new byte[3];
            //    System.Array.Copy(tempbytes, tempbytes.Length - 3, tmp, 0, 3);
            //    tempbytes = tmp;

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(msg, 0, message, header.Length, msg.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);
            //
            SendMessage(message);
        }

        protected virtual void MessageResponseHandler(byte[] receivedMessage)
        {
            // Do nothing
        }



    }
}