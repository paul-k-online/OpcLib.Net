using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Net;

using Opc;
using Opc.Da;
using OpcXml.Da;

using Subscription = Opc.Da.Subscription;

namespace OpcLib
{
    public class OpcClient : IDisposable
    {
        //TODO чем отличаются?
        //private const string OpcServerName = "RSLinx OPC Server";
        private const string OpcServerName = "RSLinx Remote OPC Server"; 
        private const string OpcAppGuid = "a05bb6d5-2f8a-11d1-9bb0-080009d01446";

        private const string OpcConnectionStringTemplate = "opcda://{0}/{1}/{{{2}}}";

        private System.Net.NetworkCredential Credential { get; set; }
        
        public string Machine { get; set; }
        public Opc.Server OpcServer { get; private set; }
        public Opc.Da.Subscription Subscription { get; private set; }
        private ItemResult[] Items { get; set; }

        public bool Advised { get; private set; }
        public void ResetAdviseStatus()
        {
            Advised = false;
        }


        public string OpcConnectionString 
        { 
            get
            {
                var machine = Machine;
                if (string.IsNullOrEmpty(machine) || machine == ".")
                {
                    machine = "";
                }

                return string.Format(OpcConnectionStringTemplate, Machine, OpcServerName, OpcAppGuid);
            }
        }

        
        public OpcClient(string machine = "", System.Net.NetworkCredential credential = null)
        {
            Machine = machine;
            Credential = credential;
            Connect();
        }

        public OpcClient(string machine, string user, string password) 
            : this(machine, new NetworkCredential(user, password))
        {}
        

        public bool Connected
        {
            get { return OpcServer != null && OpcServer.IsConnected; }
        }

        public string VersionInfo { get; private set; }
        public string VendorInfo { get; private set; }
        public string StatusInfo { get; private set; }


        /// <summary>
        /// Create the server(Using the specified URL)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static Opc.Server GetServerForUrl(Opc.URL url)
        {
            if (url == null)
                throw new ArgumentNullException("url");
            return url.Scheme == Opc.UrlScheme.DA ? new Opc.Da.Server(new OpcCom.Factory(), url) : null;
        }

        /// <summary>
        /// Connect/Disconnect to the server.
        /// </summary>
        /// <returns></returns>
        private bool Connect()
        {
            // Connect server
            var url = new Opc.URL(OpcConnectionString);

            // Create an unconnected server object.
            OpcServer = GetServerForUrl(url);             
            if (OpcServer == null)
                return false;

            // Invoke the connect server callback.
            try
            {
                var connectData = new Opc.ConnectData(Credential, null);
                OpcServer.Connect(connectData);

                var opcDaSrerver =  OpcServer as Opc.Da.Server;
               
                if (opcDaSrerver == null)
                    return false;

                VendorInfo = opcDaSrerver.GetStatus().VendorInfo;
                VersionInfo = opcDaSrerver.GetStatus().ProductVersion;
                StatusInfo = opcDaSrerver.GetStatus().StatusInfo;

                var state = new Opc.Da.SubscriptionState
                                {
                                    ClientHandle = Guid.NewGuid().ToString(),
                                    ServerHandle = null,
                                    //Name = "DEFAULT",
                                    Active = false,
                                    UpdateRate = 1000,
                                    KeepAlive = 0,
                                    //Deadband = 0,
                                    //Locale = null
                                };
                Subscription = (Subscription) opcDaSrerver.CreateSubscription(state);
            }
            catch (Exception e)
            {
                throw e;
            }
            return true;
        }

        /// <summary>
        /// Disconnect server
        /// </summary>
        private void Disconnect()
        {
            if (OpcServer == null) 
                return;
            
            try
            {
                OpcServer.Disconnect();
            }
            catch
            {}

            OpcServer.Dispose();
            OpcServer = null;
        }

        /// <summary>
        /// Add items (according to "itemList" parameter) into OPC.Subscription
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="updateRate"></param>
        /// <returns></returns>
        public bool AddItems(Opc.Da.Item[] itemList, int updateRate = 0)
        {
            var state = new Opc.Da.SubscriptionState();
            try
            {
                // Remove items
                if (Items != null)
                    Subscription.RemoveItems(Items);

                state.Active = false;
                Subscription.ModifyState((int)Opc.Da.StateMask.Active, state);
                
                state.UpdateRate = updateRate > 0 ? updateRate : 1000;
                Subscription.ModifyState((int)Opc.Da.StateMask.UpdateRate, state);

                foreach (var item in itemList)
                    item.ClientHandle = Guid.NewGuid().ToString();

                Items = Subscription.AddItems(itemList);
                Advised = false;
                return true;
            }
            catch (Exception e)
            {
                //MessageBox.Show(ex.Message);
                throw e;
            }
        }

        /*
        public bool AddItems(ArrayList itemsList, int updateRate =0)
        {
            var list = (Opc.Da.Item[])itemsList.ToArray(typeof(Opc.Da.Item));
            return AddItems(list, updateRate);
        }
         * */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagList"></param>
        /// <param name="updateRate"></param>
        /// <returns></returns>
        public bool AddItems(IOpcTag[] tagList, uint updateRate = 1000)
        {
 
            var s1 = tagList.Select(t => new Item()
                                             {
                                                 Active = false,
                                                 ItemName = t.Address
                                             }).ToArray();
            return AddItems(s1);
        }

        /// <summary>
        /// Synchronize read from the OPC Server
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public bool SyncRead(ref Opc.Da.ItemValueResult[] results)
        {
            if (Subscription == null)
                return false;

            try 
            {
                results = Subscription.Read(Subscription.Items);
            } 
            catch (Exception e) 
            {
                throw e;
            }
            return results != null && results.Length != 0;
        }

        /// <summary>
        /// Active/Deactive the OPC Server as the "auto" mode. 
        ///     When timer reaches, update the data from OPC server automatically.
        /// </summary>
        public void AdviseDeadvise()
        {
            if (Subscription == null)
                return;
            Advised = !Advised;

            try 
            {
                var state = new Opc.Da.SubscriptionState { Active = Advised };
                Subscription.ModifyState((int) Opc.Da.StateMask.Active, state);
            } 
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Refesh the data, only works in "auto" mode.
        /// </summary>
        public void Refresh()
        {
            try
            {
                Subscription.Refresh();
            }
            catch (Exception e) 
            {
                throw e;
            }
        }


        /// <summary>
        /// Sync read tags
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public bool ReadValues(ref IOpcTag[] values) //ItemValueResult
        {
            if (!AddItems(values))
                return false;
            
            var results = new ItemValueResult[] { };

            if (!SyncRead(ref results))
                return false;

            if (values.Count() != results.Count())
                return false;

            for (var i=0; i<values.Count(); i++)
            {
                if (values[i].Address != results[i].ItemName) 
                    continue;
                //values[i].IsGood = results[i].Quality == Quality.Good; 
                values[i].Value = results[i].Quality == Quality.Good ? 
                    results[i].Value : null;
            }
            return true;
        }

        /// <summary>
        /// Synchronize write to the OPC Server
        /// </summary>
        public bool SyncWrite(ref ItemValue[] items)
        {
            if (Subscription == null)
                return false;
            try
            {
                var results = Subscription.Write(items);
                return true;
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                throw e;
            }
        }

        public bool WriteValues(ref RSOpcTag[] values)
        {
            var items = values.Select(tag => new Item()
                        {
                            ItemName = tag.Address,
                        }).ToArray();

            if (!AddItems(items))
                return false;

            var itemsValue = new List<ItemValue>();
            foreach (var item in Subscription.Items)
            {
                var tag = values.FirstOrDefault(t => t.Address == item.ItemName);
                if (tag == null)
                    continue;

                var itemValue = new ItemValue(item)
                                    {
                                        Value = tag.Value,
                                    };
                itemsValue.Add(itemValue);
            }
            var arrItemsValue = itemsValue.ToArray();
            return SyncWrite(ref arrItemsValue);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Assitant function to translate the "result" into "Quality" string, "Good" or "Bad"
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static string ResultToString(Opc.Da.ItemValueResult result)
        {
            var qb = qualityBits.bad;
            if (result.QualitySpecified && result.Quality == Quality.Good)
            {
                qb = result.Quality.QualityBits;
            }
            return Opc.Convert.ToString(qb);
        }
    }
}