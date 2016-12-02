using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Opc.Da;

namespace OpcLib.Sample
{
    public partial class RSLinxNetOPCSample : Form
    {

        public static void Run()
        {
            var f = new RSLinxNetOPCSample();
            f.ShowDialog();
        }




        private OpcClient OpcController { get; set; }
        private long _lUpdate;
        private Opc.IRequest _mRequest;

        private long _mHandle;


        [DllImport("ole32.dll", EntryPoint = "CoInitializeSecurity")]
        public static extern int CoInitializeSecurity(int psd, int cauthz, int authzinfo, int reserveval, int authlevel,
                                                      int implevel, int authlist, int cap, int resev3);

        public RSLinxNetOPCSample()
        {
            //1 - RPC_C_AUTHN_LEVEL_NONE
            //3 - RPC_C_IMP_LEVEL_IMPERSONATE
            var ret = CoInitializeSecurity(0, -1, 0, 0, 1, 3, 0, 0, 0);
            
            InitializeComponent();
        }

        /// <summary>
        /// Use control's name and index to set the text
        /// </summary>
        /// <param name="controlName"></param>
        /// <param name="count"></param>
        /// <param name="text"></param>
        private void TextGroupControls(string controlName, long count, string text)
        {
            for (var i = 0; i <= count - 1; i += 1)
            {
                var control = FindControlByName(controlName, i, this);
                control.Text = text;
            }
        }

        /// <summary>
        /// The event handler to recieve the returned results from OPC Server's read acton.
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="results"></param>
        private void OnReadComplete(object clientHandle, Opc.Da.ItemValueResult[] results)
        {
            try
            {
                if ((object)_mHandle != clientHandle)                   
                {
                    return;
                }

                // Save results.
                _mRequest = null;

                // Check if there is nothing to do.
                if (results == null || results.Length == 0)
                {
                    return;
                }
                WriteControlValues(results);
                ASyncReadButton.Enabled = true;
                StatusBar.Text = "OPC Group Async Read operation is complete.";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// The event handler to recieve the returned results from the Write action.
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="results"></param>
        private void OnWriteComplete(object clientHandle, Opc.IdentifiedResult[] results)
        {
            if (!_mHandle.Equals(clientHandle))
            {
                return;
            }
            _mRequest = null;
            ASyncWriteButton.Enabled = true;
            StatusBar.Text = "OPC Group Async Write operation is complete.";
        }

        /// <summary>
        /// Write the values into the control for the read action, using their name as the identifier.
        /// </summary>
        /// <param name="values"></param>
        private void WriteControlValues(IEnumerable<ItemValueResult> values)
        {
            foreach (var result in values)
            {
                for (var i = 0; i <= 3; i++)
                {
                    var topic = FindControlByName("TopictextBox", i, this).Text;
                    var item = FindControlByName("ItemtextBox", i, this).Text;

                    var tag = new RSOpcTag(topic, item);
                    
                    var itemName = tag.ToString();
                    if (itemName != result.ItemName)
                        continue;

                    FindControlByName("ValuetextBox", i, this).Text = result.Value.ToString();
                    FindControlByName("QualitytextBox", i, this).Text = OpcClient.ResultToString(result);
                }
            }
        }

        /// <summary>
        /// Build up "itemsList" for the write action, using their name as the identifier.
        /// </summary>
        private ItemValue[] PrepareControlValueItemList()
        {
            var itemsList = new List<ItemValue>(OpcController.Subscription.Items.Count());

            foreach (var item in OpcController.Subscription.Items)
            {
                for (var i = 0; i <= 3; i++)
                {
                    var topic = FindControlByName("TopictextBox", i, this).Text;
                    var name = FindControlByName("ItemtextBox", i, this).Text;
                    var value = FindControlByName("ValuetextBox", i, this).Text;

                    var tag = new RSOpcTag(topic, name);
                    
                    if (tag.ToString() != item.ItemName)
                        continue;
                    
                    itemsList.Add(new Opc.Da.ItemValue(item) { Value = value, });
                }
            }
            return itemsList.ToArray();
        }

        /// <summary>
        /// Build up "itemsList" for the additem action.
        /// </summary>
        private RSOpcTag[] PrepareAddItemList()
        {
            var list = new List<RSOpcTag>();
            for (var i = 0; i <= 3; i++)
            {
                var topic = FindControlByName("TopictextBox", i, this).Text;
                var name = FindControlByName("ItemtextBox", i, this).Text;
                list.Add(new RSOpcTag(topic, name));
            }
            return list.ToArray();
        }

        //
        // The event handler to recieve the returned results from the Advise action.
        //
        private void OnDataChange(object subscriptionHandle, object requestHandle, Opc.Da.ItemValueResult[] values)
        {
            // Do nothing more if only a keep alive callback.
            if (values == null || values.Length == 0)
            {
                return;
            }

            try
            {
                _lUpdate += 1;
                WriteControlValues(values);
                UpdateCounttextBox.Text = _lUpdate.ToString();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// Assistant function to get the control handle according to it's name and index.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <param name="parentControl"></param>
        /// <returns></returns>
        private static Control FindControlByName(string name, int index, Control parentControl)
        {
            var controlName = name + index;
            foreach (Control control in parentControl.Controls)
            {
                if (control.Name == controlName)
                {
                    return control;
                }
                if (control.Controls.Count <= 0)
                    continue;
                var rnControl = FindControlByName(name, index, control);
                if (rnControl != null)
                    return rnControl;
            }
            return null;
        }

        /// <summary>
        /// Advise the OPC Server into "auto" mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AdviseButton_Click(object sender, EventArgs e)
        {
            OpcController.AdviseDeadvise();

            if (OpcController.Advised)
            {
                AdviseButton.Text = "Deadvise";
                RefreshButton.Enabled = true;
                StatusBar.Text = "Advise Started";
                _lUpdate = 0;
            }
            else
            {
                AdviseButton.Text = "Advise";
                RefreshButton.Enabled = false;
                StatusBar.Text = "Advise Stoped";
            }
        }

        /// <summary>
        /// Refresh data under "auto" mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            OpcController.Refresh();
        }


        /// <summary>
        /// Connect the server, Local/Remote, enable the relative controls
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnnectButton_Click(object sender, EventArgs e)
        {
            var machineName = MachineNameTxtBox.Text.Trim();

            Cursor = Cursors.WaitCursor;
            OpcController = new OpcClient(machineName);
            Cursor = Cursors.Default;

            if (OpcController.Connected)
            {
                // Server connnected
                var subscription = OpcController.Subscription;
                subscription.DataChanged += OnDataChange;

                // Set button captions to default state
                ConnnectButton.Text = "Disonnect";
                StatusBar.Text = "Connected to server";

                VendorInfotextBox.Text = OpcController.VendorInfo;
                VersionInfotextBox.Text = OpcController.VersionInfo;
                StatusBar.Text = OpcController.StatusInfo;

                UpdateRatetextBox.Text = "1000";

                AddItemButton.Enabled = true;
            }
            else
            {
                // server disconnected
                MachineNameTxtBox.Text = "";
                VendorInfotextBox.Text = "";
                VersionInfotextBox.Text = "";

                // Set button captions to default state
                ConnnectButton.Text = "Connect";
                StatusBar.Text = "Disconnected from server";
                AdviseButton.Text = "Advise";

                AddItemButton.Enabled = false;
                RefreshButton.Enabled = false;
                AdviseButton.Enabled = false;
                ASyncReadButton.Enabled = false;
                ASyncWriteButton.Enabled = false;
                SyncReadButton.Enabled = false;
                SyncWriteButton.Enabled = false;
            }
        }


        private void ASyncWriteButton_Click(System.Object sender, System.EventArgs e)
        {
            try
            {
                var subscription = OpcController.Subscription;
                var itemsvalue = PrepareControlValueItemList();

                // Convert to array of item objects.
                _mRequest = null;
                _mHandle = 0;

                if (subscription != null)
                {
                    // Begin the asynchronous read request.
                    if (itemsvalue != null)
                    {
                        StatusBar.Text = "OPC Group Async Write operation in progress ...";
                        subscription.Write(itemsvalue, +_mHandle, OnWriteComplete, out _mRequest);
                    }
                }

                // Update controls if request successful.
                if ((_mRequest != null))
                {
                    ASyncWriteButton.Enabled = false;
                }
            }
            catch (Exception em)
            {
                MessageBox.Show(em.Message);
            }
        }

        private void ASyncReadButton_Click(System.Object sender, System.EventArgs e)
        {
            try
            {
                _mRequest = null;
                _mHandle = 0;

                var subscription = OpcController.Subscription;
                if (subscription != null)
                {
                    // Begin the asynchronous read request.
                    if ((subscription.Items != null))
                    {
                        StatusBar.Text = "OPC Group Async Read in progress ...";

                        ReadCompleteEventHandler rceh = OnReadComplete;
                        subscription.Read(subscription.Items, +_mHandle, rceh, out _mRequest);
                        
                    }
                }

                // Update controls if request successful.
                if ((_mRequest != null))
                {
                    ASyncReadButton.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        
        private void SyncWriteButton_Click(System.Object sender, System.EventArgs e)
        {
            StatusBar.Text = "OPC Group Sync Write in progress ...";
            
            var itemsList = PrepareControlValueItemList();
            if (itemsList == null) 
                throw new ArgumentNullException("itemsList");

            StatusBar.Text = OpcController.SyncWrite(ref itemsList) ? "OPC Group Sync Write complete." : "OPC Group Sync Write fail.";
        }


        private void SyncReadButton_Click(System.Object sender, System.EventArgs e)
        {
            StatusBar.Text = "OPC Group Sync Read in progress ...";

            Opc.Da.ItemValueResult[] results = null;
            if (OpcController.SyncRead(ref results))
            {
                StatusBar.Text = "OPC Group Sync Read complete.";
                WriteControlValues(results);
            }
            else
            {
                StatusBar.Text = "OPC Group Sync Read fail.";
            }
        }

        private void AddItemButton_Click(System.Object sender, System.EventArgs e)
        {
            // Disable all buttons
            RefreshButton.Enabled = false;
            AdviseButton.Enabled = false;
            ASyncReadButton.Enabled = false;
            ASyncWriteButton.Enabled = false;
            SyncReadButton.Enabled = false;
            SyncWriteButton.Enabled = false;

            TextGroupControls("ValuetextBox", 4, "");
            TextGroupControls("QualitytextBox", 4, "");
            StatusBar.Text = "Adding OPC items ...";

            var itemsList = PrepareAddItemList();
            var updateRate = uint.Parse(UpdateRatetextBox.Text);

            if (!OpcController.AddItems(itemsList, updateRate))
                return;

            AdviseButton.Enabled = true;
            ASyncReadButton.Enabled = true;
            ASyncWriteButton.Enabled = true;
            SyncReadButton.Enabled = true;
            SyncWriteButton.Enabled = true;

            AdviseButton.Text = "Advise";
            OpcController.ResetAdviseStatus();
            _lUpdate = 0;

            StatusBar.Text = "OPC Items added successfully.";
        }
    }
}