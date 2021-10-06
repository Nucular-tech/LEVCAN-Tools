﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;
using LEVCAN;
using HidSharp;
using System.Threading;
using LEVCANsharpTest.paramControls;

namespace LEVCANsharpTest
{
    public partial class MainWindow : Form
    {
        SerialNode sport;
        LC_Node node;
        LC_ParamClient pclient;
        ILC_Object[] objects;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        byte[] name;
        Dictionary<LCRemoteNode, LC_ParamClient> nodeParams = new Dictionary<LCRemoteNode, LC_ParamClient>();
        List<LCRemoteNode> listOfRemotes = new List<LCRemoteNode>();
        LC_ParamClient activeCl = null;
        List<LCPC_Directory> pathSelect = new List<LCPC_Directory>();
        LC_FileServer nfs;

        public MainWindow()
        {
            InitializeComponent();
            for (int i = 1; i < statusStrip1.Items.Count; i++)
            {
                statusStrip1.Items.Insert(i, new System.Windows.Forms.ToolStripSeparator());
                i++;
            }
            listBox_devices.DataSource = listOfRemotes;

            timer1.Start();
            objects = new ILC_Object[2];
            name = new byte[128];
            //objects[0] = new LC_Object((ushort)LC_SystemMessage.NodeName, new LC_Obj_ThrottleV_t(), LC_ObjectAttributes.Writable);
            objects[0] = new LC_ObjectFunction((ushort)LC_Objects_Std.LC_Obj_ThrottleV, data_in, LC_ObjectAttributes.Writable, typeof(LC_Obj_ThrottleV_t));
            var obj = new LC_ObjectString((ushort)LC_SystemMessage.NodeName, null, 128, LC_ObjectAttributes.Writable);
            objects[1] = obj;
            obj.OnChange += nodename;

            //hardware
            //start node
            node = new LEVCAN.LC_Node(65);
            sport = new SerialNode(node);
            LC_Interface.SetAddressCallback(addressChanges);
            node.Objects = objects;
            //init client for params
            pclient = new LC_ParamClient(node, 19);
            nfs = new LC_FileServer(node);
            node.StartNode();
        }

        void addressChanges(LC_NodeShortName shortname, ushort index, LC_AddressState state)
        {
            // listBox1.Invoke((MethodInvoker)(() =>
            {
                switch (state)
                {
                    case LC_AddressState.New:
                        if (index == listOfRemotes.Count)
                        {
                            //new item added
                            LCRemoteNode rnode = new LCRemoteNode(shortname);
                            listOfRemotes.Add(rnode);
                        }
                        else if (index < listOfRemotes.Count)
                        {
                            //replace deleted one
                            if (listOfRemotes[index].ShortName.NodeID >= (ushort)LC_Address.Null)
                            {
                                listOfRemotes[index].ShortName = shortname;
                            }
                        }

                        node.SendRequest(shortname.NodeID, LC_SystemMessage.NodeName);
                        break;

                    case LC_AddressState.Changed:
                        if (index < listOfRemotes.Count)
                        {
                            LCRemoteNode rnode = new LCRemoteNode(shortname);
                            listOfRemotes[index] = rnode; //update
                        }
                        node.SendRequest(shortname.NodeID, LC_SystemMessage.NodeName);
                        break;

                    case LC_AddressState.Deleted:
                        if (index < listOfRemotes.Count)
                        {
                            listOfRemotes.RemoveAt(index);
                            //listOfRemotes[index].ShortName.NodeID = (ushort)LC_Address.Broadcast;
                        }
                        break;
                }
            }

            listbox1_Refrash();
            // ));
        }
        int names = 0;
        bool once = false;
        void nodename(LC_Header hdr, string name)
        {
            var item = listOfRemotes.Cast<LCRemoteNode>().Where(p => p.ShortName.NodeID == hdr.Source);

            if (item.Count<LCRemoteNode>() > 0)
            {
                var rnode = item.First<LCRemoteNode>();

                string formatted = hdr.Source.ToString() + ":" + name;
                if (rnode.Name == null || rnode.Name == "")
                {
                    rnode.Name = name;
                    listbox1_Refrash();
                }
            }
            names++;
            /*   if (once == false)
               {
                   once = true;

               }*/
            //node.SendRequest(19, LC_SystemMessage.NodeName);
        }

        void listbox1_Refrash()
        {
            listBox_devices.BeginInvoke((MethodInvoker)(() =>
            {
                listBox_devices.DataSource = null;
                listBox_devices.DataSource = listOfRemotes;
            }));
        }

        void data_in(LC_Header hdr, object volt)
        {
            LC_Obj_ThrottleV_t tv = ((LC_Obj_ThrottleV_t)volt);

        }


        int delay = 0;
        TimeSpan totalspan = TimeSpan.Zero;
        private async void timer1_Tick(object sender, EventArgs e)
        {
            delay += timer1.Interval;
            /*  var span = await sport.ResponceTest();
              totalspan += span;
              if (span != TimeSpan.Zero)
                  names++; //function suceeded*/

            if (delay >= 1000)
            {
                delay = 0;
                //  sport.ResponceTest();
                // if (names == 0)
                //     node.SendRequest((byte)LC_Address.Broadcast, (ushort)LC_SystemMessage.NodeName);
                label4.Text = "Names: " + sport.elcntr.ToString();//names.ToString();
                //label2.Text = (totalspan.TotalMilliseconds/ names).ToString();
                errorsLabel.Text = "Errors: " + sport.errb.ToString();
                totalspan = TimeSpan.Zero;
                txrxLabel.Text = "TX/RX: " + (sport.tx + sport.rx).ToString();
                sport.rx = 0;
                names = 0;
                sport.tx = 0;
                sport.errb = 0;
                label2.Text = sport.AvgElapsed.ToString();//sport.elapsed.TotalMilliseconds.ToString();
                label5.Text = "Max:" + sport.maxel.TotalMilliseconds.ToString();

            }
            //   node.SendRequest(19, LC_SystemMessage.NodeName);
            if (activeCl != null)
            {
                //refresh values on panel
                foreach (var conrt in flowSettingsPanel.Controls)
                {
                    await (conrt as EntryPresenter).UpdateLive();
                }
            }

        }

        private async void listBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (listBox_devices.SelectedIndex == -1)
            {
                flowSettingsPanel.Controls.Clear();
                flowSettingsPanel.Enabled = false;
                pathSelect.Clear();
                labelSettings.Text = "Settings";
                activeCl = null;
                return;
            }
            var selected = listOfRemotes[listBox_devices.SelectedIndex];
            if (selected.ShortName.Configurable)
            {
                LC_ParamClient client;
                flowSettingsPanel.Enabled = true;
                if (nodeParams.ContainsKey(selected))
                {//already here
                    client = nodeParams[selected];
                }
                else
                { //new key
                    client = new LC_ParamClient(node, selected.ShortName.NodeID, selected.Encoding);
                    nodeParams.Add(selected, client); //save bind                    
                }

                if (activeCl != client)
                {
                    activeCl = client;
                    await client.UpdateDirectoriesAsync();
                    FlowAddDirectory(client.Directories[0]);
                    pathSelect.Clear();
                    labelSettings.Text = "Settings";
                }
            }
            else
            {
                flowSettingsPanel.Enabled = false;
            }
        }

        private void folderChange(object sender, ushort newDirIndex)
        {

            if (newDirIndex < activeCl.Directories.Count && activeCl.Directories[newDirIndex].Entries != null && activeCl.Directories[newDirIndex].Entries.Count > 0)
            {
                flowSettingsPanel.Controls.Clear();
                FlowAddDirectory(activeCl.Directories[newDirIndex]);
                pathSelect.Add(activeCl.Directories[newDirIndex]);
            }
            else
            {
                labelSettings.Text = "Settings";
            }
        }

        void FlowAddDirectory(LCPC_Directory dir)
        {
            List<Control> controlArray = new List<Control>();
            foreach (var entr in dir.Entries)
            {
                switch (entr.EType)
                {
                    case LCP_EntryType.Folder:
                        var fctrl = new FolderControl(entr);
                        fctrl.FolderChange += folderChange;
                        controlArray.Add(fctrl);
                        break;
                    case LCP_EntryType.Label:
                        var lctrl = new LabelControl(entr);
                        controlArray.Add(lctrl);
                        break;
                    case LCP_EntryType.Bool:
                        var bctrl = new BoolControl(entr);
                        controlArray.Add(bctrl);
                        break;
                    case LCP_EntryType.Enum:
                        var ectrl = new EnumControl(entr);
                        controlArray.Add(ectrl);
                        break;
                    case LCP_EntryType.Bitfield32:
                        break;
                    case LCP_EntryType.Int32:
                    case LCP_EntryType.Uint32:
                    case LCP_EntryType.Int64:
                    case LCP_EntryType.Uint64:
                    case LCP_EntryType.Float:
                    case LCP_EntryType.Double:
                    case LCP_EntryType.Decimal32:
                        var vctrl = new ValueControl(entr);
                        controlArray.Add(vctrl);
                        break;
                }
            }
            // SuspendUpdate.Suspend(flowLayoutPanel1);
            flowSettingsPanel.Controls.AddRange(controlArray.ToArray());
            labelSettings.Text = dir.Name;
            MainWindow_ResizeEnd(null, null);
            // SuspendUpdate.Resume(flowLayoutPanel1);
        }


        private void label5_Click(object sender, EventArgs e)
        {
            sport.maxel = TimeSpan.Zero;
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            if (pathSelect.Count > 0)
            {
                pathSelect.RemoveAt(pathSelect.Count - 1); //remove current path
            }
            else
                return;
            flowSettingsPanel.Controls.Clear();
            if (pathSelect.Count > 0)
            {
                FlowAddDirectory(pathSelect[pathSelect.Count - 1]);
            }
            else
                FlowAddDirectory(activeCl.Directories[0]);

        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            listBox_devices.Height = listBox_devices.Parent.Height - 15;
        }

        private void MainWindow_ResizeEnd(object sender, EventArgs e)
        {
            foreach (var conrt in flowSettingsPanel.Controls)
            {
                (conrt as EntryPresenter).Width = flowSettingsPanel.Width - 22;
            }
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            MainWindow_ResizeEnd(sender, null);
        }

        /* private void listDirBox_SelectedValueChanged(object sender, EventArgs e)
         {
             if (listDirBox.SelectedIndex == -1)
                 return;
             List<LCPC_Directory> dirs = (List<LCPC_Directory>)listDirBox.DataSource;
             var selected = dirs[listDirBox.SelectedIndex];

             listEntryBox.DataSource = null;
             listEntryBox.DataSource = selected.Entries;
         }*/
    }
    public static class SuspendUpdate
    {
        private const int WM_SETREDRAW = 0x000B;

        public static void Suspend(Control control)
        {
            Message msgSuspendUpdate = Message.Create(control.Handle, WM_SETREDRAW, IntPtr.Zero,
                IntPtr.Zero);

            NativeWindow window = NativeWindow.FromHandle(control.Handle);
            window.DefWndProc(ref msgSuspendUpdate);
        }

        public static void Resume(Control control)
        {
            // Create a C "true" boolean as an IntPtr
            IntPtr wparam = new IntPtr(1);
            Message msgResumeUpdate = Message.Create(control.Handle, WM_SETREDRAW, wparam,
                IntPtr.Zero);

            NativeWindow window = NativeWindow.FromHandle(control.Handle);
            window.DefWndProc(ref msgResumeUpdate);

            control.Invalidate();
        }
    }
}
