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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

namespace ZWaveLib.Devices
{
    public delegate void DiscoveryEventHandler(object source, DiscoveryEventArgs e);



    public enum DISCOVERY_STATUS
    {
        NODE_ADDED = 0x00,
        NODE_REMOVED = 0x01
    }

    public class DiscoveryEventArgs : EventArgs
    {
        public readonly byte NodeId;
        public readonly DISCOVERY_STATUS Status;

        public DiscoveryEventArgs(byte nodeId, DISCOVERY_STATUS status)
        {
            this.Status = status;
            this.NodeId = nodeId;
        }
    }


    public class Controller : ZWaveNode
    {
        public override event ManufacturerSpecificResponseEventHandler ManufacturerSpecificResponse;
        public Action<object, DiscoveryEventArgs> DiscoveryEvent;

        public void OnDiscoveryEvent(DiscoveryEventArgs e)
        {
            if (DiscoveryEvent != null)
            {
                DiscoveryEvent(this, e);
            }
        }

        private List<ZWaveNode> _devices = new List<ZWaveNode>();

        private byte _nodeoperationidcheck = 0;

        public Controller(ZWavePort zp)
            : base(1, zp)
        {
            zp.ZWaveMessageReceived += new ZWavePort.ZWaveMessageReceivedEvent((object sender, ZWaveMessageReceivedEventArgs args) =>
            {
                try
                {
                    _zwavemessagereceived(sender, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ZWaveLib: ERROR in _zwavemessagereceived(...) " + ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        public void Discovery()
        {
            zp.Discovery();
        }

        public ZWaveNode GetDevice(byte nodeId)
        {
            return _devices.Find(zn => zn.NodeId == nodeId);
        }
        public List<ZWaveNode> Devices
        {
            get { return _devices; }
        }

        public virtual byte BeginNodeAdd()
        {
            byte[] header = new byte[] { 0x01 /* Start Of Frame */, 0x05 /*packet len */, 0x00 /* type req/res */, 0x4a };
            byte[] footer = new byte[] { 0x03 | 0x80, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            byte callbackid = SendMessage(message);

            return callbackid;
        }


        public virtual byte StopNodeAdd()
        {
            byte[] header = new byte[] { 0x01 /* Start Of Frame */, 0x05 /*packet len */, 0x00 /* type req/res */, 0x4a };
            byte[] footer = new byte[] { 0x05, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }


        public virtual byte BeginNodeRemove()
        {

            byte[] header = new byte[] { 0x01 /* Start Of Frame */, 0x05 /*packet len */, 0x00 /* type req/res */, 0x4b };
            byte[] footer = new byte[] { 0x01 | 0x80, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }

        public virtual byte StopNodeRemove()
        {
            byte[] header = new byte[] { 0x01 /* Start Of Frame */, 0x05 /*packet len */, 0x00 /* type req/res */, 0x4b };
            byte[] footer = new byte[] { 0x05, 0x00, 0x00 };
            byte[] message = new byte[header.Length + footer.Length];

            System.Array.Copy(header, 0, message, 0, header.Length);
            System.Array.Copy(footer, 0, message, message.Length - footer.Length, footer.Length);

            return SendMessage(message);
        }


        private void GetNodeCapabilities(byte nodeId)
        {
            currentCommandTargetNode = nodeId;
            byte[] message = new byte[] { 0x01, 0x04, 0x00, (byte)ZWaveCommandClass.CMD_GET_NODE_PROTOCOL_INFO, nodeId, 0x00 };
            SendMessage(message, true);
        }

        public void GetNodeInformationFrame(byte nodeId)
        {
            byte[] message = new byte[] { 0x01, 0x04, 0x00, (byte)ZWaveCommandClass.CMD_REQUEST_NODE_INFO, nodeId, 0x00 };
            SendMessage(message, true);
        }

        public enum ZWaveCommandClass
        {
            CMD_NONE = 0x00,
            CMD_DISCOVERY_NODES = 0x02,
            CMD_APPLICATION_COMMAND = 0x04,
            CMD_SEND_DATA = 0x13,
            CMD_GET_NODE_PROTOCOL_INFO = 0x41,
            CMD_NODE_UPDATE_INFO = 0x49,
            CMD_NODE_ADD = 0x4A,
            CMD_NODE_REMOVE = 0x4B,
            CMD_REQUEST_NODE_INFO = 0x60
        }


        private void _zwavemessagereceived(object sender, ZWaveMessageReceivedEventArgs args)
        {
            int length = args.Message.Length;
            //
            try
            {
                ZWaveMessageHeader zh = (ZWaveMessageHeader)args.Message[0];
                switch (zh)
                {
                    case ZWaveMessageHeader.CAN:
                        zp.SendAck();
                        // RESEND
//Console.WriteLine("ZWaveLib: received CAN, resending last message");
//                        zp.ResendLastMessage();
                        break;
                    case ZWaveMessageHeader.ACK:
                        zp.SendAck();
                        break;
                    case ZWaveMessageHeader.SOF: // start of zwave frame
                        //
                        // parse frame headers
                        //
                        int msglength = (int)args.Message[1];
                        ZWaveMessageType msgtype = (ZWaveMessageType)args.Message[2];
                        ZWaveCommandClass cmdclass = (args.Message.Length > 3 ? (ZWaveCommandClass)args.Message[3] : 0);
                        //
                        byte sourcenode = 0;
                        //
                        switch (msgtype)
                        {
                            case ZWaveMessageType.REQUEST:
                                zp.SendAck();

                                if (_devices.Count == 0) break;

                                byte callbackid = 0;

                                switch (cmdclass)
                                {
                                    case ZWaveCommandClass.CMD_NONE:
                                        break;

                                    case ZWaveCommandClass.CMD_NODE_ADD:

                                        callbackid = args.Message[4];
                                        if (args.Message[5] == 0x03) // ADD_NODE_STATUS_ADDING_SLAVE
                                        {
                                            //
//Console.WriteLine("\n\nADDING NODE SLAVE {0}\n     ->   ", zp.ByteArrayToString(args.Message));
                                            //
                                            // example response from HSM-100 3in1 sensor 
                                            // 01 15 00 4A 32 03 2E 0E 04 21 01 60 31 70 84 85 80 72 77 86 EF 20 79
                                            //                         bt gt st c1 c2 c3 c4 c5 c6 c7 c8 c9 --------
                                            // node supported classes (c1, c2.... cn)
                                            //
                                            _nodeoperationidcheck = args.Message[6];
                                            CreateDevice(_nodeoperationidcheck, 0x00);
                                        }
                                        else if (args.Message[5] == 0x05) // ADD_NODE_STATUS_PROTOCOL_DONE
                                        {
                                            if (_nodeoperationidcheck == args.Message[6])
                                            {
//Console.WriteLine("\n\nADDING NODE PROTOCOL DONE {0} {1}\n\n", args.Message[6], callbackid);
                                                Thread.Sleep(500);
                                                GetNodeCapabilities(args.Message[6]);
                                                //Discovery();
                                            }
                                        }
                                        else if (args.Message[5] == 0x07) // ADD_NODE_STATUS_ADDING_FAIL
                                        {
//Console.WriteLine("\n\nADDING NODE FAIL {0}\n\n", args.Message[6]);
                                        }
                                        break;

                                    case ZWaveCommandClass.CMD_NODE_REMOVE:

                                        callbackid = args.Message[4];
                                        if (args.Message[5] == 0x03) // REMOVE_NODE_STATUS_REMOVING_SLAVE
                                        {
//Console.WriteLine("\n\nREMOVING NODE SLAVE {0}\n\n", args.Message[6]);
                                            _nodeoperationidcheck = args.Message[6];
                                        }
                                        else if (args.Message[5] == 0x06) // REMOVE_NODE_STATUS_REMOVING_DONE
                                        {
                                            if (_nodeoperationidcheck == args.Message[6])
                                            {
//Console.WriteLine("\n\nREMOVING NODE DONE {0} {1}\n\n", args.Message[6], callbackid);
                                                OnDiscoveryEvent(new DiscoveryEventArgs(args.Message[6], DISCOVERY_STATUS.NODE_REMOVED)); // Send event
                                                RemoveDevice(args.Message[6]);
                                            }
                                        }
                                        else if (args.Message[5] == 0x07) // REMOVE_NODE_STATUS_REMOVING_FAIL
                                        {
//Console.WriteLine("\n\nREMOVING NODE FAIL {0}\n\n", args.Message[6]);
                                        }
                                        break;

                                    case ZWaveCommandClass.CMD_APPLICATION_COMMAND:

                                        sourcenode = args.Message[5];
                                        ZWaveNode node = _devices.Find(n => n.NodeId == sourcenode);
                                        if (node == null)
                                        {
                                            CreateDevice(sourcenode, 0x00);
                                            GetNodeCapabilities(sourcenode);
                                        }
                                        try
                                        {
                                            node.MessageRequestHandler(args.Message);
                                        }
                                        catch (Exception ex)
                                        {
//Console.WriteLine("# " + ex.Message + "\n" + ex.StackTrace);
                                        }
                                        break;

                                    case ZWaveCommandClass.CMD_SEND_DATA:

                                        byte cid = args.Message[4];
                                        if (cid == 0x01) // SEND DATA OK
                                        {
                                            // TODO: ... what does that mean?
                                        }
                                        else if (args.Message[5] == 0x00)
                                        {
                                            // Messaging complete, remove callbackid
                                            zp.PendingMessages.RemoveAll(zm => zm.CallbackId == cid);
                                        }
                                        else if (args.Message[5] == 0x01)
                                        {
                                            // TODO: callback error???
                                            zp.ResendLastMessage(cid);
                                        }
                                        break;

                                    case ZWaveCommandClass.CMD_NODE_UPDATE_INFO:

                                        sourcenode = args.Message[5];
                                        int niflen = (int)args.Message[6];
                                        ZWaveNode znode = _devices.Find(n => n.NodeId == sourcenode);
                                        if (znode != null)
                                        {
                                            byte[] nodeinfo = new byte[niflen - 2];
//Console.WriteLine(ByteArrayToString(args.Message));
                                            Array.Copy(args.Message, 7, nodeinfo, 0, niflen - 2);
                                            //
                                            _raiseUpdateParameterEvent(znode, 0, ParameterType.PARAMETER_NODE_INFO, zp.ByteArrayToString(nodeinfo));
                                            _raiseUpdateParameterEvent(znode, 0, ParameterType.PARAMETER_WAKEUP_NOTIFY, "1");
                                        }
                                        break;

                                    default:
                                        Console.WriteLine("\nUNHANDLED Z-Wave REQUEST\n     " + zp.ByteArrayToString(args.Message) + "\n");
                                        break;

                                }


                                break;
                            case ZWaveMessageType.RESPONSE:


                                switch (cmdclass)
                                {
                                    case ZWaveCommandClass.CMD_DISCOVERY_NODES:
                                        MessageResponseNodeBitMaskHandler(args.Message);
                                        //zp.SendAck();
                                        break;
                                    case ZWaveCommandClass.CMD_GET_NODE_PROTOCOL_INFO:
                                        MessageResponseNodeCapabilityHandler(args.Message);
                                        //zp.SendAck();
                                        break;
                                    case ZWaveCommandClass.CMD_REQUEST_NODE_INFO:
//                                        Console.WriteLine("\nNODE INFO RESPONSE: " + zp.ByteArrayToString(args.Message) + "\n");
                                        break;
                                    case ZWaveCommandClass.CMD_SEND_DATA:
                                        this.ReadyToSend = true;
                                        break;
                                    default:
                                        //if (args.Message.Length > 2 && args.Message[3] != 0x13) 
                                        Console.WriteLine("\nUNHANDLED Z-Wave RESPONSE\n     " + zp.ByteArrayToString(args.Message) + "\n");
                                        break;
                                }


                                break;

                            default:
                                Console.WriteLine("\nUNHANDLED Z-Wave message TYPE\n     " + zp.ByteArrayToString(args.Message) + "\n");
                                break;
                        }



                        //
                        break;
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);

            }
        }



        public String ByteArrayToString(byte[] message)
        {
            String ret = String.Empty;
            foreach (byte b in message)
            {
                ret += b.ToString("X2") + " ";
            }
            return ret.Trim();
        }




        private void MessageResponseNodeBitMaskHandler(byte[] receivedMessage)
        {
            int length = receivedMessage.Length;
            if (length > 3)
            {
                // Is this a discovery response?
                if (receivedMessage[3] == 0x02)
                {
                    CreateDevices(receivedMessage);
                }
            }
        }


        private void MessageResponseNodeCapabilityHandler(byte[] receivedMessage)
        {
            int length = receivedMessage.Length;
            if (length > 8)
            {
                try
                {
                    ZWaveNode node = _devices.Find(n => n.NodeId == currentCommandTargetNode);
                    if (node == null)
                    {
//Console.WriteLine("Z-Wave Adding node " + currentCommandTargetNode + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                        _devices.Add(CreateDevice(currentCommandTargetNode, receivedMessage[8]));
                    }
                    //
                    node.BasicClass = receivedMessage[7];
                    node.GenericClass = receivedMessage[8];
                    node.SpecificClass = receivedMessage[9];
                    node.SetGenericHandler();
                    //
                    if (node.NodeId != 1)
                    {
//Console.WriteLine("Z-Wave Updating node " + node.NodeId + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                        OnDiscoveryEvent(new DiscoveryEventArgs(currentCommandTargetNode, DISCOVERY_STATUS.NODE_ADDED)); // Send event
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine("Z-Wave ERROR adding node " + node.NodeId + " : " + e.Message + "\n" + e.StackTrace);
                    //System.Diagnostics.Debugger.Break();
                }
            }
            ZWaveNode nextnode = _devices.Find(zn => zn.BasicClass == 0x00);
            if (nextnode != null)
            {
                currentCommandTargetNode = nextnode.NodeId;
                GetNodeCapabilities(currentCommandTargetNode);
            }
        }

        private byte currentCommandTargetNode = 0;
        private void CreateDevices(byte[] receivedMessage)
        {
            List<byte> nodeList = ExtractNodesFromBitMask(receivedMessage);
            foreach (byte i in nodeList)
            {
                ZWaveNode node = _devices.Find(n => n.NodeId == i);
                if (node == null)
                {
//Console.WriteLine("Z-Wave Adding node " + i + " Class[ Basic=" + receivedMessage[7].ToString("X2") + " Generic=" + ((GenericType)receivedMessage[8]).ToString() + " Specific=" + receivedMessage[9].ToString("X2") + " ]");
                    _devices.Add(CreateDevice(i, 0x00));
                }
            }
            Thread.Sleep(500);
            if (nodeList.Count > 0)
            {
//Console.WriteLine("Z-Wave Querying node capabilities " + currentCommandTargetNode);
                currentCommandTargetNode = nodeList[0];
                GetNodeCapabilities(currentCommandTargetNode);
            }
        }

        private List<byte> ExtractNodesFromBitMask(byte[] receivedMessage)
        {
            List<byte> nodeList = new List<byte>();
            // Decode the nodes in the bitmask (byte 9 - 37)
            byte k = 1;
            for (int i = 7; i < 36; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    try
                    {
                        if ((receivedMessage[i] & ((byte)Math.Pow(2, j))) == ((byte)Math.Pow(2, j)))
                        {
//Console.WriteLine(this.GetType().Name.ToString() + " Node id: " + k + " discovered");
                            nodeList.Add(k);
                        }
                    }
                    catch (Exception ex)
                    {

                        System.Diagnostics.Debugger.Break();
                    }
                    k++;
                }
            }
            return nodeList;
        }

        private void RemoveDevice(byte nodeId)
        {
            ZWaveNode node = _devices.Find(n => n.NodeId == nodeId);
            if (node != null)
            {
                node.UpdateNodeParameter -= znode_UpdateNodeParameter;
                node.ManufacturerSpecificResponse -= znode_ManufacturerSpecificResponse;
            }
            _devices.RemoveAll(zn => zn.NodeId == nodeId);
        }

        private ZWaveNode CreateDevice(byte nodeId, byte genericClass)
        {
            String className = "ZWaveLib.Devices.";


            switch (genericClass)
            {
                case 0x02:
                    className += "Controller";
                    this.NodeId = nodeId;
                    return (ZWaveNode)this;
                default: // generic node
                    className += "ZWaveNode";
                    break;
            }
            ZWaveNode znode = (ZWaveNode)Activator.CreateInstance(Type.GetType(className), new object[] { nodeId, zp, genericClass });
            znode.UpdateNodeParameter += znode_UpdateNodeParameter;
            znode.ManufacturerSpecificResponse += znode_ManufacturerSpecificResponse;
//            znode.ManufacturerSpecific_Get();
            return znode;
        }

        void znode_ManufacturerSpecificResponse(object sender, ManufacturerSpecificResponseEventArg mfargs)
        {
            _raiseUpdateParameterEvent((ZWaveNode)sender, 0, ParameterType.PARAMETER_ZWAVE_MANUFACTURER_SPECIFIC, mfargs.ManufacturerSpecific);
            if (this.ManufacturerSpecificResponse != null)
            {
                // route nodes events
                ManufacturerSpecificResponse(sender, mfargs);
            }
        }

        void znode_UpdateNodeParameter(object sender, UpdateNodeParameterEventArgs upargs)
        {
            _raiseUpdateParameterEvent((ZWaveNode)sender, upargs.ParameterId, upargs.ParameterType, upargs.Value);
        }

        public bool ReadyToSend { get; set; }
    }
}
