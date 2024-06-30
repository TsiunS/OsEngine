using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market.Servers.Entity;
using WebSocket4Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using OsEngine.Market.Servers.FixFastCurrency.FIX;
using OsEngine.Market.Servers.FixFastCurrency.FAST;
using System.Windows.Forms.DataVisualization.Charting;
using System.Text.RegularExpressions;
using OpenFAST;
using System.IO;
using OpenFAST.Codec;
using OpenFAST.Template.Loader;
using OpenFAST.Template;
using System.Xml;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Documents;
using System.Linq;
using System.Diagnostics;




namespace OsEngine.Market.Servers.FixFastCurrency
{
    public class FixFastCurrencyServer : AServer
    {
        public FixFastCurrencyServer()
        {
            FixFastCurrencyServerRealization realization = new FixFastCurrencyServerRealization();
            ServerRealization = realization;

            //FIX
            CreateParameterString("SenderCompID", "");
            CreateParameterPassword("Password", "");

            CreateParameterString("FX MFIX Trade Address", "");
            CreateParameterInt("FX MFIX Trade Port", 0);
            CreateParameterString("FX MFIX Trade TargetCompID", "");

            CreateParameterString("FX MFIX Trade Capture Address", "");
            CreateParameterInt("FX MFIX Trade Capture Port", 0);
            CreateParameterString("FX MFIX Trade Captue TargetCompID", "");

            CreateParameterString("FX Drop Copy Address", "");
            CreateParameterInt("FX Drop Copy Port", 0);
            CreateParameterString("FX Drop Copy TargetCompID", "");


            //FAST
            CreateParameterPath("Multicast Config Directory");

        }
    }

    public class FixFastCurrencyServerRealization : IServerRealization
    {


        #region 1 Constructor, Status, Connection

        public FixFastCurrencyServerRealization()
        {
            ServerStatus = ServerConnectStatus.Disconnect;

            Thread thread1 = new Thread(InstrumentDefinitionsReader);
            thread1.Name = "GetterSecurity";
            thread1.Start();

            Thread thread2 = new Thread(GetFastMessagesByTrades);
            thread2.Name = "GetterTrades";
            thread2.Start();

            Thread thread3 = new Thread(TradeMessagesReader);
            thread3.Name = "TradesReaderFromQueues";
            thread3.Start();

            Thread thread4 = new Thread(GetFastMessagesByOrders);
            thread4.Name = "GetterOrders";
            thread4.Start();

            Thread thread5 = new Thread(OrderMessagesReader);
            thread5.Name = "OrdersReaderFromQueues";
            thread5.Start();

             Thread thread6 = new Thread(RecoverData);
            thread6.Name = "HistoricalReplayMoexFixFastSpot";
            thread6.Start();            

        }

        public DateTime ServerTime { get; set; }

        public void Connect()
        {

            _senderCompID = ((ServerParameterString)ServerParameters[0]).Value;
            _password = ((ServerParameterPassword)ServerParameters[1]).Value;

            _FXMFIXTradeAddress = ((ServerParameterString)ServerParameters[2]).Value;
            _FXMFIXTradePort = ((ServerParameterInt)ServerParameters[3]).Value;
            _FXMFIXTradeTargetCompID = ((ServerParameterString)ServerParameters[4]).Value;

            _FXMFIXTradeCaptureAddress = ((ServerParameterString)ServerParameters[5]).Value;
            _FXMFIXTradeCapturePort = ((ServerParameterInt)ServerParameters[6]).Value;
            _FXMFIXTradeCaptureTargetCompID = ((ServerParameterString)ServerParameters[7]).Value;

            _FXDropCopyAddress = ((ServerParameterString)ServerParameters[8]).Value;
            _FXDropCopyPort = ((ServerParameterInt)ServerParameters[9]).Value;
            _FXDropCopyTargetCompID = ((ServerParameterString)ServerParameters[10]).Value;

            _configDir = ((ServerParameterPath)ServerParameters[11]).Value;


            if (string.IsNullOrEmpty(_senderCompID) || string.IsNullOrEmpty(_password))
            {
                SendLogMessage("Can`t run connector. No CompId or password", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_FXMFIXTradeAddress) || string.IsNullOrEmpty(_FXMFIXTradeTargetCompID) || _FXMFIXTradePort == 0)
            {
                SendLogMessage("Can`t run connector. No MFIX Trade parameters are specified", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_FXMFIXTradeCaptureAddress) || string.IsNullOrEmpty(_FXMFIXTradeCaptureTargetCompID) || _FXMFIXTradeCapturePort == 0)
            {
                SendLogMessage("Can`t run connector. No MFIX Trade Capture parameters are specified", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_FXDropCopyAddress) || string.IsNullOrEmpty(_FXDropCopyTargetCompID) || _FXDropCopyPort == 0)
            {
                SendLogMessage("Can`t run connector. No Drop Copy parameters are specified", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_configDir) || !Directory.Exists(_configDir) || !File.Exists(_configDir + "\\config.xml") || !File.Exists(_configDir + "\\template.xml"))
            {
                SendLogMessage("Can`t run connector. No multicast directory are specified or the config and templates files don't exist", LogMessageType.Error);
                return;
            }

         


            LoadFASTTemplates();

            List<FastConnection> _addressesInstruments = GetAddressesForFastConnect("Instrument Replay");

            CreateSocketConnections(_addressesInstruments);

            if (_socketSecurityStreamA != null || _socketSecurityStreamB != null)
            {
                ServerStatus = ServerConnectStatus.Connect;
                ConnectEvent();
            }
            else
            {
                SendLogMessage("Connection can be open. AAAAAAAAAAAAAAAAAAAA", LogMessageType.Error);
            }

            //   CreateFASTSocketConnections();

            try
                {
                    //TimeLastSendPing = DateTime.Now;
                    //TimeToUprdatePortfolio = DateTime.Now;
                    //FIFOListWebSocketMessage = new ConcurrentQueue<string>();

                 //   CreateFixConnection();
                }
                catch (Exception exeption)
                {
                    SendLogMessage(exeption.ToString(), LogMessageType.Error);
                    SendLogMessage("Connection can be open. BitGet. Error request", LogMessageType.Error);
                    ServerStatus = ServerConnectStatus.Disconnect;
                    DisconnectEvent();
                }
            
           
        }

        private void LoadFASTTemplates()
        {
            IMessageTemplateLoader loader = new XmlMessageTemplateLoader();

            using (FileStream stream = File.OpenRead(_configDir + "\\template.xml"))
            {
                _templates = loader.Load(stream);
            }
        }

        public void Dispose()
        {
            // заходим при нажатии кнопки отключить

            _securities.Clear();
          //  _myPortfolios.Clear();
          //  DeleteWebSocketConnection();

            SendLogMessage("Connection Closed by FixFastEquities. WebSocket Data Closed Event", LogMessageType.System);

            if (ServerStatus != ServerConnectStatus.Disconnect)
            {
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent();
            }
        }

        public ServerType ServerType
        {
            get { return ServerType.FixFastCurrency; }
        }

        public ServerConnectStatus ServerStatus { get; set; }

        public event Action ConnectEvent;

        public event Action DisconnectEvent;

        #endregion

        #region 2 Properties

        public List<IServerParameter> ServerParameters { get; set; }

        // системные сделки: окончание торгов - 16:00

        // for FIX
        private string _senderCompID;
        private string _password;

        private string _FXMFIXTradeAddress;
        private int _FXMFIXTradePort;
        private string _FXMFIXTradeTargetCompID;

        private string _FXMFIXTradeCaptureAddress;
        private int _FXMFIXTradeCapturePort;
        private string _FXMFIXTradeCaptureTargetCompID;

        private string _FXDropCopyAddress;
        private int _FXDropCopyPort;
        private string _FXDropCopyTargetCompID;

        private int _msgSeqNum = 1;
        private int _incomingFixMsgNum;
        private bool _logoutInitiator = false;

        // for FAST
        private bool _afterStartTrading = true;
        private string _configDir;
        private Context _contextFAST;
        private MessageTemplate[] _templates;

        private string _logLock = "locker for stream writer";
        private StreamWriter _logFile = new StreamWriter("FIXFAST_Multicast_UDP-log.txt");

        private string _logLockOrder = "locker for stream writer";
        private StreamWriter _logFileOrders = new StreamWriter("FIXFAST_Multicast_FOR ORDERS.txt");

        private Socket _socketSecurityStreamA;
        private Socket _socketSecurityStreamB;

        private Socket _tradesIncrementalSocketA;
        private Socket _tradesIncrementalSocketB;
        private Socket _tradesSnapshotSocketA;
        private Socket _tradesSnapshotSocketB;
        List<Socket> _socketsTrades = new List<Socket>();
        private Socket _ordersIncrementalSocketA;
        private Socket _ordersIncrementalSocketB;
        private Socket _ordersSnapshotSocketA;
        private Socket _ordersSnapshotSocketB;
        List<Socket> _socketsOrders = new List<Socket>();

        private Socket _historicalReplaySocket;
        private IPEndPoint _historicalReplayEndPoint;
        private long _missingOLRBeginSeqNo = -1;
        private long _missingOLREndSeqNo = 0;
        private bool _missingOLRData = false;
        private long _missingTLRBeginSeqNo = -1;
        private long _missingTLREndSeqNo = 0;
        private bool _missingTLRData = false;

        #endregion

        #region 3 Securities


        public void GetSecurities()
        {
            while(_allSecuritiesLoaded != true)
            {
                SendLogMessage("Securities not dowloaded. Wait, please/", LogMessageType.System);
                Thread.Sleep(1000);
            }


            SecurityEvent(_securities);
        }

        List<Security> _securities = new List<Security>();

        public event Action<List<Security>> SecurityEvent;

        #endregion

        #region 4 Portfolios


        public void GetPortfolios()
        {
  
            UpdatePortfolioREST();
        }


        private void UpdatePortfolioREST()
        {
            Portfolio portfolio = new Portfolio();
            portfolio.Number = "MB9122603459";
            portfolio.ValueBegin = 1;
            portfolio.ValueCurrent = 1;


                //PositionOnBoard newPortf = new PositionOnBoard();
                //newPortf.SecurityNameCode = balance.coin;
                //newPortf.ValueBegin = balance.free.ToDecimal();
                //newPortf.ValueCurrent = balance.free.ToDecimal();
                //newPortf.ValueBlocked = balance.frozen.ToDecimal();
                //newPortf.PortfolioName = "CurrMOEX";
                //portfolio.SetNewPosition(newPortf);
            

            PortfolioEvent(new List<Portfolio> { portfolio });
        }

        public event Action<List<Portfolio>> PortfolioEvent;

        #endregion

        #region 5 Data

        public List<Candle> GetLastCandleHistory(Security security, TimeFrameBuilder timeFrameBuilder, int candleCount)
        {
            return null;
        }

        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        #endregion

        #region 6 Sockets creation

        private void CreateSocketConnections(List<FastConnection> connectParams)
        {
            try
            {
                for (int i = 0; i < connectParams.Count; i++)
                {
                    if (connectParams[i].FeedType == "Historical Replay")
                    {
                        IPAddress ipAddr = IPAddress.Parse(connectParams[i].SrsIP);

                        _historicalReplayEndPoint = new IPEndPoint(ipAddr, connectParams[i].Port);

                        return;
                    }

                    // Create a UDP socket
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    // Configure the socket options
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1 * 1024 * 1024); // Set receive buffer size

                    //// Join the multicast group
                    IPAddress multicastAddress = IPAddress.Parse(connectParams[i].MulticastIP);
                    IPAddress sourceAddress = IPAddress.Parse(connectParams[i].SrsIP);

                    //// Bind the socket to the port
                    //// Specify the local IP address and port to bind to.
                    IPAddress localAddress = IPAddress.Any; // Listen on all available interfaces
                    IPEndPoint localEndPoint = new IPEndPoint(localAddress, connectParams[i].Port);


                    socket.Bind(localEndPoint);

                    byte[] membershipAddresses = new byte[12]; // 3 IPs * 4 bytes (IPv4)
                    Buffer.BlockCopy(multicastAddress.GetAddressBytes(), 0, membershipAddresses, 0, 4);
                    Buffer.BlockCopy(sourceAddress.GetAddressBytes(), 0, membershipAddresses, 4, 4);
                    Buffer.BlockCopy(localAddress.GetAddressBytes(), 0, membershipAddresses, 8, 4);
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddSourceMembership, membershipAddresses);


                    if (connectParams[i].FeedType == "Instrument Replay")
                    {
                        if (connectParams[i].FeedID == "A")
                        {
                            _socketSecurityStreamA = socket;
                        }

                        if (connectParams[i].FeedID == "B")
                        {
                            _socketSecurityStreamB = socket;
                        }
                    }

                    if (connectParams[i].FeedType == "Trades Incremental")
                    {
                        if (connectParams[i].FeedID == "A")
                        {
                            _tradesIncrementalSocketA = socket;
                            _socketsTrades.Add(_tradesIncrementalSocketA);
                        }

                        if (connectParams[i].FeedID == "B")
                        {
                            _tradesIncrementalSocketB = socket;
                            _socketsTrades.Add(_tradesIncrementalSocketB);
                        }
                    }

                    if (connectParams[i].FeedType == "Trades Snapshot")
                    {
                        if (connectParams[i].FeedID == "A")
                        {
                            _tradesSnapshotSocketA = socket;
                            _socketsTrades.Add(_tradesSnapshotSocketA);
                        }

                        if (connectParams[i].FeedID == "B")
                        {
                            _tradesSnapshotSocketB = socket;
                            _socketsTrades.Add(_tradesSnapshotSocketB);
                        }
                    }

                    if (connectParams[i].FeedType == "Orders Incremental")
                    {
                        if (connectParams[i].FeedID == "A")
                        {
                              _ordersIncrementalSocketA = socket;
                            _socketsOrders.Add(_ordersIncrementalSocketA);
                        }

                        if (connectParams[i].FeedID == "B")
                        {
                               _ordersIncrementalSocketB = socket;
                            _socketsOrders.Add(_ordersIncrementalSocketB);
                        }
                    }

                    if (connectParams[i].FeedType == "Orders Snapshot")
                    {
                        if (connectParams[i].FeedID == "A")
                        {
                              _ordersSnapshotSocketA = socket;
                            _socketsOrders.Add(_ordersSnapshotSocketA);
                        }

                        if (connectParams[i].FeedID == "B")
                        {
                             _ordersSnapshotSocketB = socket;
                            _socketsOrders.Add(_ordersSnapshotSocketB);
                        }
                    }
                }

                //try
                //{
                //    SendLogMessage("All streams activated. Connect State", LogMessageType.System);
                //    ServerStatus = ServerConnectStatus.Connect;
                //    ConnectEvent();
                //}
                //catch (Exception ex)
                //{
                //    SendLogMessage(ex.ToString(), LogMessageType.Error);
                //}
            }
            catch (Exception exeption)
            {
                SendLogMessage(exeption.ToString(), LogMessageType.Error);
            }
        }







        private TcpClient _client;
        private NetworkStream _stream;
        private MessageConstructor _messageConstructor;

        private void CreateFixConnection()
        {
            //_client = new TcpClient(Address, _tradePort);
            //_stream = _client.GetStream();


            if (_client.Connected)
            {
               // _messageConstructor = new MessageConstructor(_senderCompID, _targetCompId);
                string logonMsg = _messageConstructor.LogonMessage(_password, _msgSeqNum, 30, true);
                SendMessage(logonMsg);
                Thread.Sleep(5000);

            }
            else
            {
                SendLogMessage("TCP client for FIX not created", LogMessageType.Error);
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent();
            }

        }

        private void DeleteWebscoektConnection()
        {
           
        }

        private void CreateAuthMessageWebSocekt()
        {
           
        }

        #endregion

        #region 7 Depth, trades

     

      

        #endregion

        #region 8 Security subscrible

        public void Subscrible(Security security)
        {
            try
            {
                CreateSubscribleSecurity(security);

            }
            catch (Exception exeption)
            {
                SendLogMessage(exeption.ToString(), LogMessageType.Error);
            }
        }

      

        private List<string> _subscribledSecutiries = new List<string>();

        private void CreateSubscribleSecurity(Security security)
        {
            if (ServerStatus == ServerConnectStatus.Disconnect)
            {
                return;
            }

            string uniqueName = security.Name + security.NameClass; // название бумаги может дублироваться в разных режимах, поэтому создаем уникальное имя

            for (int i = 0; i < _subscribledSecutiries.Count; i++)
            {
                if (_subscribledSecutiries[i].Equals(uniqueName))
                {
                    return;
                }
            }

            if (_subscribledSecutiries.Count == 0 && _tradesIncrementalSocketA == null && _tradesIncrementalSocketB == null
                && _ordersIncrementalSocketA == null && _ordersIncrementalSocketB == null)
            {
                CreateSocketConnections(GetAddressesForFastConnect("Trades Incremental"));
                CreateSocketConnections(GetAddressesForFastConnect("Orders Incremental"));
                _depthChanges.Add(uniqueName, new List<OrderChange>());
            }
            if (_afterStartTrading) // если берем инструмент после начала торгов
            {
                if (_ordersSnapshotSocketA == null && _ordersSnapshotSocketB == null
                    && _tradesSnapshotSocketA == null && _tradesSnapshotSocketB == null)
                {
                    CreateSocketConnections(GetAddressesForFastConnect("Trades Snapshot"));
                    CreateSocketConnections(GetAddressesForFastConnect("Orders Snapshot"));
                }

                _tradesSnapshotsByName.Add(uniqueName, new Snapshot());
                _ordersSnapshotsByName.Add(uniqueName, new Snapshot());
                _waitingDepthChanges.Add(uniqueName, new List<OrderChange>());
                _numbersForCheckMissed.Add(uniqueName, new List<NumbersData>());

            }

            _subscribledSecutiries.Add(uniqueName); 


        }

        #endregion

        #region 9 Socket parsing  messages

        //FAST

        private DateTime _lastInstrumentDefinitionsTime = DateTime.MinValue;
        private bool _allSecuritiesLoaded = false;
        private long _totNumReports = 0;

        // очереди сообщений, которые прилетают из FIX/FAST Multicast UPD соединений
        private ConcurrentQueue<OpenFAST.Message> _tradeMessages = new ConcurrentQueue<OpenFAST.Message>();
        private ConcurrentQueue<OpenFAST.Message> _orderMessages = new ConcurrentQueue<OpenFAST.Message>();

        // ИНСТРУМЕНТЫ
        private void InstrumentDefinitionsReader()
        {
            byte[] buffer = new byte[4096];

            List<long> snapshotIds = new List<long>();
            List<Security> securities = new List<Security>();

            Thread.Sleep(1000);

            Context context = null;

            while (true)
            {
                try
                {
                    if (_socketSecurityStreamA == null || _socketSecurityStreamB == null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (_allSecuritiesLoaded)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (context == null)
                    {
                        context = new Context();
                        foreach (MessageTemplate tmplt in _templates)
                        {
                            context.RegisterTemplate(int.Parse(tmplt.Id), tmplt);
                        }
                    }

                    // читаем из потоков А и B
                    // либо сразу обрабатываем либо перемещаем в очередь для разбора
                    for (int s = 0; s < 2; s++)
                    {
                        int length = s == 0 ? _socketSecurityStreamA.Receive(buffer) : _socketSecurityStreamB.Receive(buffer);

                        using (MemoryStream stream = new MemoryStream(buffer, 4, length))
                        {
                            FastDecoder decoder = new FastDecoder(context, stream);
                            OpenFAST.Message msg = decoder.ReadMessage();

                            string msgType = msg.GetString("MessageType");
                            long msgSeqNum = int.Parse(msg.GetString("MsgSeqNum"));

                            if (msgType == "d") /// security definition
                            {
                                _lastInstrumentDefinitionsTime = DateTime.UtcNow;
                                _totNumReports = msg.GetLong("TotNumReports"); // общее число "бумаг" (возможны дубли)

                                if (snapshotIds.FindIndex(nmb => nmb == msgSeqNum) != -1)
                                {
                                    if (snapshotIds.Count == _totNumReports)
                                    {
                                        _securities = securities;
                                        _allSecuritiesLoaded = true;

                                        int cets = 0;
                                        int cngd = 0;

                                        foreach (var sec in _securities)
                                        {
                                            if(sec.NameClass == "CNGD")
                                            {
                                                cngd++;
                                            }
                                            if (sec.NameClass == "CETS")
                                            {
                                                cets++;
                                            }
                                        }

                                        SendLogMessage($"Загружено {_securities.Count} бумаг. Из них в режиме CETS: {cets},  SNGD: {cngd}, others: {_securities.Count - (cets+cngd)}", LogMessageType.System);
                       
                                    }

                                    continue;
                                }

                                snapshotIds.Add(msgSeqNum);

                                string secDecimals = "0";
                                string lot = "1";
                                string TradingSessionID = "CETS"; // по-умолчанию системные сделки
                                string TradingSessionSubID = "N"; // по-умолчанию нормальный период торгов

                                string symbol = msg.GetString("Symbol");

                                if (msg.IsDefined("MarketSegmentGrp"))
                                {
                                    SequenceValue secVal = msg.GetValue("MarketSegmentGrp") as SequenceValue;

                                    for (int i = 0; i < secVal.Length; i++)
                                    {
                                        GroupValue groupVal = secVal[i] as GroupValue;

                                        if (groupVal.IsDefined("RoundLot"))
                                        {
                                            lot = groupVal.GetString("RoundLot");
                                        }

                                        if (groupVal.IsDefined("TradingSessionRulesGrp"))
                                        {
                                            SequenceValue secVal2 = groupVal.GetValue("TradingSessionRulesGrp") as SequenceValue;

                                            for (int j = 0; j < secVal2.Length; j++)
                                            {
                                                GroupValue trdSessionGrp = secVal2[j] as GroupValue;

                                                TradingSessionID = trdSessionGrp.GetString("TradingSessionID");
                                                TradingSessionSubID = trdSessionGrp.GetString("TradingSessionSubID");
                                            }
                                        }
                                    }
                                }

                                string securityID = symbol;
                                string currency = msg.GetString("SettlCurrency");
                                string marketCode = msg.GetString("MarketCode");

                                //if (marketCode != "CURR") // ? если корзина, то маркеткод пустой
                                //    continue;

                                bool securityAlreadyPresent = false;
                                for (int i = 0; i < securities.Count; i++)
                                {
                                    if (securities[i].Name == symbol && securities[i].NameClass == TradingSessionID)
                                    {
                                        securityAlreadyPresent = true;
                                        break;
                                    }
                                }

                                if (securityAlreadyPresent) // если бумага в списке есть, дальше не обрабатываем
                                {
                                    continue;
                                }

                                // Обрабатываем новые бумаги                            
   

                                string name = msg.GetString("SecurityDesc");
                              
                                if (msg.IsDefined("GroupInstrAttrib"))
                                {
                                    SequenceValue secVal = msg.GetValue("GroupInstrAttrib") as SequenceValue;

                                    for (int i = 0; i < secVal.Length; i++)
                                    {
                                        GroupValue groupVal = secVal[i] as GroupValue;

                                        if (groupVal.IsDefined("InstrAttribType"))
                                        {
                                            if (groupVal.GetValue("InstrAttribType").ToString() == "27")
                                            {
                                                secDecimals = groupVal.GetValue("InstrAttribValue").ToString();
                                            }
                                        }
                                    }
                                }

                                Security newSecurity = new Security();
                                newSecurity.Name = symbol;
                                newSecurity.NameId = securityID;
                                newSecurity.NameFull = name;
                                newSecurity.Exchange = "MOEX";

                                if (TradingSessionSubID != "NA")
                                {
                                    newSecurity.State = SecurityStateType.Activ;
                                }
                                else
                                {
                                    newSecurity.State = SecurityStateType.Close;
                                }

                                string productType = msg.IsDefined("Product") ? msg.GetString("Product") : "не определено";
                                switch (productType)
                                {
                                    case "4":
                                       newSecurity.SecurityType = SecurityType.CurrencyPair;
                                        break;
                                    case "7":
                                        newSecurity.SecurityType = SecurityType.Index;
                                        break;
                                    default:
                                        newSecurity.SecurityType = SecurityType.None;
                                        break;
                                }

                                newSecurity.NameClass = TradingSessionID; 
                                newSecurity.Lot = lot.ToDecimal();

                                if (msg.IsDefined("MinPriceIncrement"))
                                {
                                    newSecurity.PriceStep = msg.GetString("MinPriceIncrement").ToDecimal();
                                }
                                else
                                {
                                    newSecurity.PriceStep = 1;
                                }

                                if (newSecurity.PriceStep == 0)
                                {
                                    newSecurity.PriceStep = 1;
                                }

                                newSecurity.PriceStepCost = newSecurity.PriceStep;
                                newSecurity.DecimalsVolume = 1;
                                newSecurity.Decimals = int.Parse(secDecimals);

                               
                                securities.Add(newSecurity);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    SendLogMessage(exception.ToString(), LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        // ТРЕЙДЫ
        Dictionary<long, OpenFAST.Message> _tradesIncremental = new Dictionary<long, OpenFAST.Message>();
        Dictionary<long, OpenFAST.Message> _tradesSnapshotsMsgs = new Dictionary<long, OpenFAST.Message>(); // используется для проверки пропуска собщений
        Dictionary<string, Snapshot> _tradesSnapshotsByName = new Dictionary<string, Snapshot>(); // для обновления
        Dictionary<string, List<NumbersData>> _numbersForCheckMissed = new Dictionary<string, List<NumbersData>>(); // для проверки пропука данных по RptSeq

        private void GetFastMessagesByTrades()
        {
            byte[] buffer = new byte[4096];

            OpenFAST.Context context = null;

            Thread.Sleep(1000);

            while (true)
            {
                try
                {
                    if (_tradesIncrementalSocketA == null || _tradesIncrementalSocketB == null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (context == null)
                    {
                        context = new OpenFAST.Context();
                        foreach (MessageTemplate tmplt in _templates)
                        {
                            context.RegisterTemplate(int.Parse(tmplt.Id), tmplt);
                        }
                    }
                 

                    for (int s = 0; s < _socketsTrades.Count; s++)
                    {
                        int length = _socketsTrades[s].Receive(buffer); //s == 0 ? _tradesIncrementalSocketA.Receive(buffer) : _tradesIncrementalSocketB.Receive(buffer);

                        using (MemoryStream stream = new MemoryStream(buffer, 4, length))
                        {
                            FastDecoder decoder = new FastDecoder(context, stream);
                            OpenFAST.Message msg = decoder.ReadMessage();

                            long msgSeqNum = msg.GetLong("MsgSeqNum");

                            if (msg.GetString("MessageType") == "W")
                            {

                                string name = msg.GetString("Symbol");
                                string TradingSessionID = msg.GetString("TradingSessionID");
                                string uniqueName = name + TradingSessionID;

                                bool needAddMsg = IsMessageMissed(_tradesSnapshotsMsgs, msgSeqNum, msg);

                                if (needAddMsg)
                                {
                                   if(IsSubscribedToThisSecurity(uniqueName))
                                            _tradeMessages.Enqueue(msg);
                                }
                            }
                            if (msg.GetString("MessageType") == "X")
                            {
     

                                bool needAddMsg = IsMessageMissed(_tradesIncremental, msgSeqNum, msg);

                             //   WriteLog($" Сообщение с номером {msgSeqNum} надо добавить: {needAddMsg} в словаре инкрементов {_tradesIncremental.Count}", "IncrementParse");

                                if (needAddMsg) // если предыдущего сообщения с таким номером не было
                                {
                                    // проверка потери данных
                                    if (_tradesIncremental.Count > 0)
                                    {
                                        long beginMsgSeqNum = 0; // начало пропущенных данных
                                        long endMsgSeqNum = 0; // конец пропущенных данных

                                        //!!!!!!!!! Проверить! Ошибка в методе - длина результирующего массива
                                        //bool needToRecoverDates = IsDataMissed(_tradesIncremental, out beginMsgSeqNum, out endMsgSeqNum);

                                        //if (needToRecoverDates)
                                        //{
                                        //  //  WriteLog($"Требуется восстановление трейдов. Номера сообщений с {beginMsgSeqNum} по {endMsgSeqNum}", "TradesReader");
                                        //}
                                    }

                                    if (msg.IsDefined("GroupMDEntries"))
                                    {
                                        SequenceValue secVal = msg.GetValue("GroupMDEntries") as SequenceValue;

                                          string nameForCheck = string.Empty;

                                        for (int i = 0; i < secVal.Length; i++)
                                        {
                                       
                                            GroupValue groupVal = secVal[i] as GroupValue;

                                            string name = groupVal.GetString("Symbol");
                                            string TradingSessionID = groupVal.GetString("TradingSessionID");
                                            string uniqueName = name + TradingSessionID;

                                            if (IsSubscribedToThisSecurity(uniqueName))
                                            {
                                                if(name != nameForCheck) // чтобы не отправлять повторно сообщение, содержащее более 1 трейда по одному инструменту
                                                {
                                                    nameForCheck = name;

                                                    // WriteLog($" Инструмент в сообщении:{name + TradingSessionID}. Сообщение:\n\t{msg}", "В очередь");
                                                     _tradeMessages.Enqueue(msg);
                                                }

                                            }
                
                                        }
                                    }
                                }

                            }

                        }
                    }

                 
                }
                catch (Exception exception)
                {
                    SendLogMessage(exception.ToString(), LogMessageType.Error);
                    WriteLog("!!!!!!!!!!!!Ошибка в потоке первичной обработки трейдов", "TradesIncrementalReader");
                    Thread.Sleep(5000);
                }
            }
        }

        DateTime firstSnapshotTime = DateTime.MinValue;
        DateTime lastSnapshotTime = DateTime.MinValue;

        private void TradeMessagesReader()
        {
            Thread.Sleep(1000);


            // минимальные значения полей RptSeq(83) в накопленых трейдах по разным инструментам
            Dictionary<string, int> minRptSeqFromTrades = new Dictionary<string, int>();

           List<WaitingTrade> _waitingTrades = new List<WaitingTrade>();


            //bool faketradesnotloaded = true;

            while (true)
            {
                try
                {
                    if (_tradeMessages.IsEmpty)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    OpenFAST.Message msg;

                    // в этой очереди снэпшоты и трейды по подписанным инструментам
                    _tradeMessages.TryDequeue(out msg);

                    if (msg == null)
                    {
                        continue;
                    }

                    string msgType = msg.GetString("MessageType");
                    long msgSeqNum = msg.GetLong("MsgSeqNum");

                    if (msgType == "X") /// Market Data - Incremental Refresh (X)
                    {

                        if (msg.IsDefined("GroupMDEntries"))
                        {
                            SequenceValue secVal = msg.GetValue("GroupMDEntries") as SequenceValue;

                            for (int i = 0; i < secVal.Length; i++)
                            {
                                GroupValue groupVal = secVal[i] as GroupValue;
        
                                string name = groupVal.GetString("Symbol");
                                string TradingSessionID = groupVal.GetString("TradingSessionID");
                                string uniqueName = name + TradingSessionID;

                               if(!IsSubscribedToThisSecurity(uniqueName)) // если не подписаны на этот инструмент, трейд не берем
                               {
                                    continue;
                               }

                                string MDEntryType = groupVal.GetString("MDEntryType");
                                int RptSeqFromTrades = groupVal.GetInt("RptSeq");

                                // проверка пропуска даных по RptSeq

                                _numbersForCheckMissed[uniqueName].Add(new NumbersData { MsgSeqNum = msgSeqNum, RptSeq = RptSeqFromTrades });

                                long beginMsgSeqNum = 0; // начало пропущенных данных
                                long endMsgSeqNum = 0; // конец пропущенных данных

                                bool needToRecoverDates = IsDataMissed(_numbersForCheckMissed[uniqueName], out beginMsgSeqNum, out endMsgSeqNum);
                             
                                if (needToRecoverDates)
                                {
                                      WriteLog($"Требуется восстановление трейдов. Номера сообщений с {beginMsgSeqNum} по {endMsgSeqNum}", "TradesReader");
                                }

                                // храним минимальный номер обновления по инструменту
                                if (minRptSeqFromTrades.ContainsKey(uniqueName))
                                {
                                    if (minRptSeqFromTrades[uniqueName] > RptSeqFromTrades)
                                    {
                                        minRptSeqFromTrades[uniqueName] = RptSeqFromTrades; 
                                    }
                                }
                                else
                                {
                                      minRptSeqFromTrades.Add(uniqueName, RptSeqFromTrades);
                                }

                               

                                if (MDEntryType == "z")
                                {
                                    Trade trade = new Trade();
                                    trade.SecurityNameCode = name;
                                    trade.Price = groupVal.GetString("MDEntryPx").ToDecimal();

                                    string time = groupVal.GetString("MDEntryTime");
                                    if (time.Length == 8)
                                    {
                                        time = "0" + time;
                                    }

                                    time = DateTime.UtcNow.ToString("ddMMyyyy") + time;

                                    DateTime tradeDateTime = DateTime.ParseExact(time, "ddMMyyyyHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

                                    trade.Time = tradeDateTime.AddHours(2);

                                    trade.Id = groupVal.GetString("MDEntryID");
                                    trade.Side = groupVal.GetString("OrderSide") == "B" ? Side.Buy : Side.Sell;
                                    trade.Volume = groupVal.GetString("MDEntrySize").ToDecimal();

                                    //(если по этой бумаге снэпшот применен, трейд обновляем сразу)

                                    if(_afterStartTrading && !_tradesSnapshotsByName[uniqueName].SnapshotWasApplied)
                                    {
                                        WaitingTrade _waitingTrade = new WaitingTrade();
                                        _waitingTrade.Trade = trade;
                                        _waitingTrade.UniqueName = uniqueName;
                                        _waitingTrade.RptSeq = RptSeqFromTrades;
                                   
                                        _waitingTrades.Add(_waitingTrade);

                                        WriteLog($"Получен трейд в ОЖИДАЮЩИЕ: RptSeq: {RptSeqFromTrades}, Интсрумент: {trade.SecurityNameCode}, {trade.Side}, Цена: {trade.Price}, Время: {trade.Time}, Объем: {trade.Volume}", "Обработка инкрементов");
                                    }
                                    else
                                    {
                                          NewTradesEvent(trade);

                                        WriteLog($"Получен трейд в СИСТЕМУ: RptSeq: {RptSeqFromTrades}, Интсрумент: {trade.SecurityNameCode}, {trade.Side}, Цена: {trade.Price}, Время: {trade.Time}, Объем: {trade.Volume}", "Обработка инкрементов");
                                    }
                         

                                }

                            }
                        }
                    }

                    // Обрабатываем снэпшот
                    if (msgType == "W") /// Market Data - Snapshot/Full Refresh (W)
                    {

                        string name = msg.GetString("Symbol");
                        string TradingSessionID = msg.GetString("TradingSessionID");
                        long LastMsgSeqNumProcessed = msg.GetLong("LastMsgSeqNumProcessed");
                        long MsgSeqNum = msg.GetLong("MsgSeqNum");
                        string LastFragment = msg.GetString("LastFragment"); // 1 - сообщение последнее, снэпшот сформирован
                        string RouteFirst = msg.GetString("RouteFirst"); // 1 - сообщение первое, формирующее снэпшот по инструменту

                        string uniqueName = name + TradingSessionID;

                        int RptSeq = msg.GetInt("RptSeq");

                        SnapshotFragment fragment = new SnapshotFragment();
                        fragment.MsgSeqNum = MsgSeqNum;
                        fragment.RptSeq = RptSeq;
                        fragment.LastFragment = LastFragment == "1" ? true : false;
                        fragment.RouteFirst = RouteFirst == "1" ? true : false;
                        fragment.Symbol = name;
                        fragment.TradingSessionID = TradingSessionID;

                        WriteLog($"Получен фрагмент снэпшота по инструменту: {uniqueName}, MsgSeqNum: {MsgSeqNum}, Первый: {fragment.RouteFirst}, Последний: {fragment.LastFragment}, RptSeq: {RptSeq}", "Обработка снэпшотов");

                        if(fragment.RouteFirst)
                        {
                          firstSnapshotTime = DateTime.Now;
                        }

                        if(fragment.LastFragment)
                        {
                            lastSnapshotTime = DateTime.Now;
                            TimeSpan timeSpan = lastSnapshotTime - firstSnapshotTime;
                            WriteLog($"\t--------------------\n\tСнэпшот по инструменту: {uniqueName} сформирован за {timeSpan}\n\t-----------------", "SnapshotReader");
                        }

                        if (msg.IsDefined("GroupMDEntries"))
                        {
                            SequenceValue secVal = msg.GetValue("GroupMDEntries") as SequenceValue;
                         
                            for (int i = 0; i < secVal.Length; i++)
                            {
                                GroupValue groupVal = secVal[i] as GroupValue;

                                string MDEntryType = groupVal.GetString("MDEntryType");

                                if (MDEntryType == "z")
                                {
                                    Trade trade = new Trade();
                                    trade.SecurityNameCode = name;
                                    trade.Price = groupVal.GetString("MDEntryPx").ToDecimal();


                                    string time = groupVal.GetString("MDEntryTime");
                                    if (time.Length == 8)
                                    {
                                        time = "0" + time;
                                    }

                                    time = DateTime.UtcNow.ToString("ddMMyyyy") + time;

                                    DateTime tradeDateTime = DateTime.ParseExact(time, "ddMMyyyyHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

                                    trade.Time = tradeDateTime.AddHours(2);

                                    trade.Id = groupVal.GetString("MDEntryID");
                                    trade.Side = groupVal.GetString("OrderSide") == "B" ? Side.Buy : Side.Sell;
                                    trade.Volume = groupVal.GetString("MDEntrySize").ToDecimal();

                                    if (fragment.trades == null)
                                    {
                                        fragment.trades = new List<Trade>();
                                        fragment.trades.Add(trade);

                                      //  WriteLog($"Из фрагмента добавлен первый трейд ID: {trade.Id},", "if (msg.IsDefined(\"GroupMDEntries\"))");
                                    }
                                    else
                                    {
                                        fragment.trades.Add(trade);
                                      //  WriteLog($"Из фрагмента добавлен очередной трейд ID: {trade.Id},", "if (msg.IsDefined(\"GroupMDEntries\"))");
                                    }
                                }

                                if (MDEntryType == "J") // Empty Book
                                {
                                    continue;
                                }
                            }
                        }

                        // добавляем сообщение в снэпшот и проверяем его готовность
                        if (_tradesSnapshotsByName[uniqueName].SnapshotFragments == null)
                        {
                            _tradesSnapshotsByName[uniqueName].SnapshotFragments = new List<SnapshotFragment>();
                            _tradesSnapshotsByName[uniqueName].SnapshotFragments.Add(fragment);
                            _tradesSnapshotsByName[uniqueName].RptSeq = fragment.RptSeq;
                            _tradesSnapshotsByName[uniqueName].Symbol = fragment.Symbol;
                            _tradesSnapshotsByName[uniqueName].TradingSessionID = fragment.TradingSessionID;
                        }
                        else
                        {
                            _tradesSnapshotsByName[uniqueName].SnapshotFragments.Add(fragment);
                            WriteLog($"Фрагмент добавлен в снэпшот", "Фрагмент");
                        }


                      // если снэпшот сформирован
                        if (_tradesSnapshotsByName[uniqueName].IsComletedSnapshot(_tradesSnapshotsByName[uniqueName].SnapshotFragments) == true)
                        {
                            // и его RptSeq больше минимального RptSeq из инкримента, то применяем обновление
                            if (_tradesSnapshotsByName[uniqueName].RptSeq > minRptSeqFromTrades[uniqueName]
                                || _tradesSnapshotsByName[uniqueName].RptSeq == minRptSeqFromTrades[uniqueName] - 1)
                            {
                                WriteLog($"Снэпшот по инструменту {uniqueName} сформирован. Содержит {_tradesSnapshotsByName[uniqueName].SnapshotFragments.Count} фрагментов.", "TradeMessageReader");

                                for (int i = 0; i < _tradesSnapshotsByName[uniqueName].SnapshotFragments.Count; i++)
                                {
                                   var _trades = _tradesSnapshotsByName[uniqueName].SnapshotFragments[i].trades;

                                    for (int k = 0; k < _trades.Count; k++)
                                    {
                                        NewTradesEvent(_trades[k]);
                                    }
                                }

                                int _RptSeqFromSnapshot = _tradesSnapshotsByName[uniqueName].RptSeq;

                                for (int j = 0; j < _waitingTrades.Count; j++)
                                {
                                    if (_waitingTrades[j].RptSeq < _RptSeqFromSnapshot)
                                        continue;

                                    NewTradesEvent(_waitingTrades[j].Trade);
                                }

                                _tradesSnapshotsByName[uniqueName].SnapshotWasApplied = true;

                                WriteLog($"Снэпшот применен", "Обновление");

                                // отправить информацию о том, что поток снэпшота по этому инструменту можно не слушать
                                // и когда по всем подписанным иснтрументам будут применены обновления, закрыть сокеты снэпшотов
                            }
                        }
                    }

                }
                catch(Exception ex)
                {
                    WriteLog($"Ошибка в потоке вторичной обработки трейдов: {ex}", "TradesMessageReader");
                }

            }
        }

        // ОРДЕРА
        Dictionary<long, OpenFAST.Message> _ordersIncremental = new Dictionary<long, OpenFAST.Message>();
        Dictionary<long, OpenFAST.Message> _ordersSnapshotsMsgs = new Dictionary<long, OpenFAST.Message>(); // используется для проверки пропуска собщений
      

        private void GetFastMessagesByOrders()
        {
            byte[] buffer = new byte[4096];

            OpenFAST.Context context = null;

            Thread.Sleep(1000);

            while (true)
            {
                try
                {
                    if (_ordersIncrementalSocketA == null || _ordersIncrementalSocketB == null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (context == null)
                    {
                        context = new OpenFAST.Context();
                        foreach (MessageTemplate tmplt in _templates)
                        {
                            context.RegisterTemplate(int.Parse(tmplt.Id), tmplt);
                        }
                    }


                    for (int s = 0; s < _socketsOrders.Count; s++)
                    {
                        int length = _socketsOrders[s].Receive(buffer); 

                        using (MemoryStream stream = new MemoryStream(buffer, 4, length))
                        {
                            FastDecoder decoder = new FastDecoder(context, stream);
                            OpenFAST.Message msg = decoder.ReadMessage();

                            long msgSeqNum = msg.GetLong("MsgSeqNum");

                            if (msg.GetString("MessageType") == "W")
                            {

                                string name = msg.GetString("Symbol");
                                string TradingSessionID = msg.GetString("TradingSessionID");
                                string uniqueName = name + TradingSessionID;

                                bool needAddMsg = IsMessageMissed(_ordersSnapshotsMsgs, msgSeqNum, msg);

                                if (needAddMsg)
                                {
                                    if (IsSubscribedToThisSecurity(uniqueName))
                                        _orderMessages.Enqueue(msg);
                                }
                            }
                            if (msg.GetString("MessageType") == "X")
                            {


                                bool needAddMsg = IsMessageMissed(_ordersIncremental, msgSeqNum, msg);

                                //   WriteLog($" Сообщение с номером {msgSeqNum} надо добавить: {needAddMsg} в словаре инкрементов {_tradesIncremental.Count}", "IncrementParse");

                                if (needAddMsg) // если предыдущего сообщения с таким номером не было
                                {
                                    // проверка потери данных
                                    if (_ordersIncremental.Count > 0)
                                    {
                                        long beginMsgSeqNum = 0; // начало пропущенных данных
                                        long endMsgSeqNum = 0; // конец пропущенных данных

                                        //!!!!!!!!!ПРОВЕРИТЬ! Ошибка - длина результирующего массива
                                        //bool needToRecoverDates = IsDataMissed(_tradesIncremental, out beginMsgSeqNum, out endMsgSeqNum);

                                        //if (needToRecoverDates)
                                        //{
                                        //    //  WriteLog($"Требуется восстановление трейдов. Номера сообщений с {beginMsgSeqNum} по {endMsgSeqNum}", "TradesReader");
                                        //}
                                    }

                                    if (msg.IsDefined("GroupMDEntries"))
                                    {
                                        SequenceValue secVal = msg.GetValue("GroupMDEntries") as SequenceValue;

                                        string nameForCheck = string.Empty;

                                        for (int i = 0; i < secVal.Length; i++)
                                        {

                                            GroupValue groupVal = secVal[i] as GroupValue;

                                            string name = groupVal.GetString("Symbol");
                                            string TradingSessionID = groupVal.GetString("TradingSessionID");
                                            string uniqueName = name + TradingSessionID;

                                            if (IsSubscribedToThisSecurity(uniqueName))
                                            {
                                                if (name != nameForCheck) // чтобы не отправлять повторно сообщение, содержащее более 1 трейда по одному инструменту
                                                {
                                                    nameForCheck = name;

                                                   // WriteLogOrders($" Инструмент в сообщении:{name + TradingSessionID}. Сообщение:\n\t{msg}", "В очередь");
                                                    _orderMessages.Enqueue(msg);
                                                }

                                            }

                                        }
                                    }
                                }

                            }

                        }
                    }


                }
                catch (Exception exception)
                {
                    SendLogMessage(exception.ToString(), LogMessageType.Error);
                    WriteLog("!!!!!!!!!!!!Ошибка в потоке первичной обработки ордеров", "GetFastMessagesByOrders");
                    Thread.Sleep(5000);
                }
            }
        }

        Dictionary<string, Snapshot> _ordersSnapshotsByName = new Dictionary<string, Snapshot>(); // для обновления
        Dictionary<string, List<OrderChange>> _depthChanges = new Dictionary<string, List<OrderChange>>();
        Dictionary<string, List<OrderChange>> _waitingDepthChanges = new Dictionary<string, List<OrderChange>>();

        private void OrderMessagesReader()
        {
            Thread.Sleep(1000);

            // минимальные значения полей RptSeq(83) в накопленых ордерах по разным инструментам
            Dictionary<string, int> minRptSeqFromOrders = new Dictionary<string, int>();
            DateTime timeForDepth = DateTime.MinValue;

            while (true)
            {
                try
                {
                    if (_orderMessages.IsEmpty)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    OpenFAST.Message msg;

                    // в этой очереди снэпшоты и orders по подписанным инструментам
                    _orderMessages.TryDequeue(out msg);

                    if (msg == null)
                    {
                        continue;
                    }

                    string msgType = msg.GetString("MessageType");

                    if (msgType == "X") /// Market Data - Incremental Refresh (X)
                    {

                        if (msg.IsDefined("GroupMDEntries"))
                        {
                            SequenceValue secVal = msg.GetValue("GroupMDEntries") as SequenceValue;

                            for (int i = 0; i < secVal.Length; i++)
                            {
                                GroupValue groupVal = secVal[i] as GroupValue;

                                string name = groupVal.GetString("Symbol");
                                string TradingSessionID = groupVal.GetString("TradingSessionID");
                                string uniqueName = name + TradingSessionID;

                                if (!IsSubscribedToThisSecurity(uniqueName)) // если не подписаны на этот инструмент, ордер не берем
                                {
                                    continue;
                                }

                                string MDEntryType = groupVal.GetString("MDEntryType");
                                int RptSeqFromOrder = groupVal.GetInt("RptSeq");

                                // храним минимальный номер обновления по инструменту
                                if (minRptSeqFromOrders.ContainsKey(uniqueName))
                                {
                                    if (minRptSeqFromOrders[uniqueName] > RptSeqFromOrder)
                                    {
                                        minRptSeqFromOrders[uniqueName] = RptSeqFromOrder;
                                    }
                                }
                                else
                                {
                                    minRptSeqFromOrders.Add(uniqueName, RptSeqFromOrder);
                                }

                                OrderChange newOrderChange = new OrderChange();

                                string orderType;

                                switch (MDEntryType)// 0 - котировка на покупку, 1 - котровка на продажу
                                {
                                    case "0": orderType = "bid"; break;
                                    case "1": orderType = "ask"; break;
                                    default: orderType = "Order type ERROR"; break;
                                }

                                string action;

                                switch (groupVal.GetString("MDUpdateAction"))
                                {
                                    case "0": action = "add"; break;
                                    case "1": action = "change"; break;
                                    case "2": action = "delete"; break;
                                    default: action = "Action ERROR"; break;
                                }

                                decimal price = groupVal.GetString("MDEntryPx").ToDecimal();

                                string time = groupVal.GetString("MDEntryTime");
                                if (time.Length == 8)
                                {
                                    time = "0" + time;
                                }

                                time = DateTime.UtcNow.ToString("ddMMyyyy") + time;

                                DateTime orderDateTime = DateTime.ParseExact(time, "ddMMyyyyHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

                                string id = groupVal.GetString("MDEntryID");

                                decimal volume = groupVal.GetString("MDEntrySize").ToDecimal();

                                newOrderChange.UniqueName = uniqueName;
                                newOrderChange.MDEntryID = id;
                                newOrderChange.OrderType = orderType;
                                newOrderChange.Price = price;
                                newOrderChange.Action = action;
                                newOrderChange.RptSeq = RptSeqFromOrder;
                                newOrderChange.DateTime = orderDateTime;
                                newOrderChange.Volume = volume;

                                if (_afterStartTrading && !_ordersSnapshotsByName[uniqueName].SnapshotWasApplied)
                                { 
                                    // пока снэпшот не применен, изменения заявок складываем в ожидающие
                                                                    
                                        _waitingDepthChanges[uniqueName].Add(newOrderChange);

                                    WriteLogOrders($"Получен ордер в ОЖИДАЮЩИЕ: RptSeq: {RptSeqFromOrder}, Инструмент: {uniqueName}, ID: {id}, Действие: {action}, Цена: {price}, Время: {time}, Объем: {volume}\n Ожидающих: {_waitingDepthChanges.Count}", "Обработка инкрементов orders");
                                }
                                else
                                {
                                    // обработка в реальном времени

                                    _depthChanges[uniqueName].Add(newOrderChange);
                                    WriteLogOrders($"Получен ордер в СИСТЕМУ. СТАКАН ОБНОВЛЕН: RptSeq: {RptSeqFromOrder}, Интсрумент: {uniqueName}, ID: {id}, Действие: {action}, Цена: {price}, Время: {time}, Объем: {volume}\n Ожидающих: {_waitingDepthChanges.Count}", "Обработка инкрементов orders");

                                    UpdateOrderData(uniqueName, newOrderChange);

                                    UpdateDepth(_depthChanges[uniqueName], name, orderDateTime);

                                }
 
                            }
                        }
                    }

                    // Обрабатываем снэпшот
                    if (msgType == "W") /// Market Data - Snapshot/Full Refresh (W)
                    {

                        string name = msg.GetString("Symbol");
                        string TradingSessionID = msg.GetString("TradingSessionID");
                        long LastMsgSeqNumProcessed = msg.GetLong("LastMsgSeqNumProcessed");
                        long MsgSeqNum = msg.GetLong("MsgSeqNum");
                        string LastFragment = msg.GetString("LastFragment"); // 1 - сообщение последнее, снэпшот сформирован
                        string RouteFirst = msg.GetString("RouteFirst"); // 1 - сообщение первое, формирующее снэпшот по инструменту

                        string uniqueName = name + TradingSessionID;

                        int RptSeq = msg.GetInt("RptSeq");

                        SnapshotFragment fragment = new SnapshotFragment();
                        fragment.MsgSeqNum = MsgSeqNum;
                        fragment.RptSeq = RptSeq;
                        fragment.LastFragment = LastFragment == "1" ? true : false;
                        fragment.RouteFirst = RouteFirst == "1" ? true : false;
                        fragment.Symbol = name;
                        fragment.TradingSessionID = TradingSessionID;
                        fragment.mdLevel = new List<MarketDepthLevel>();

                        WriteLogOrders($"Получен фрагмент снэпшота по инструменту: {uniqueName}, MsgSeqNum: {MsgSeqNum}, Первый: {fragment.RouteFirst}, Последний: {fragment.LastFragment}, RptSeq: {RptSeq}", "Обработка снэпшотов");

                        if (fragment.RouteFirst)
                        {
                            firstSnapshotTime = DateTime.Now;
                        }

                        if (fragment.LastFragment)
                        {
                            lastSnapshotTime = DateTime.Now;
                            TimeSpan timeSpan = lastSnapshotTime - firstSnapshotTime;
                            WriteLogOrders($"\t--------------------\n\tСнэпшот по инструменту: {uniqueName} сформирован за {timeSpan}\n\t-----------------", "SnapshotReader");
                        }

                        if (msg.IsDefined("GroupMDEntries"))
                        {
                            SequenceValue secVal = msg.GetValue("GroupMDEntries") as SequenceValue;

                            for (int i = 0; i < secVal.Length; i++)
                            {
                                GroupValue groupVal = secVal[i] as GroupValue;

                                OrderChange newOrderChange = new OrderChange();
                                MarketDepthLevel level = new MarketDepthLevel();
                         

                                string MDEntryType = groupVal.GetString("MDEntryType");
                                decimal price = groupVal.GetString("MDEntryPx").ToDecimal();
                                string Id = groupVal.GetString("MDEntryID");
                                decimal volume = groupVal.GetString("MDEntrySize").ToDecimal();

                                string orderType;

                                switch (MDEntryType)// 0 - котировка на покупку, 1 - котровка на продажу
                                {
                                    case "0":
                                        orderType = "bid";
                                        level.Bid = volume;
                                        level.Ask = 0;
                                        break;
                                    case "1":
                                        orderType = "ask";
                                        level.Ask = volume;
                                        level.Bid = 0;
                                        break;
                                    case "J": // пустой снэпшот
                                        continue;
                                    default: orderType = "Order type ERROR"; break;
                                }

                                string time = groupVal.GetString("MDEntryTime");
                                if (time.Length == 8)
                                {
                                    time = "0" + time;
                                }

                                time = DateTime.UtcNow.ToString("ddMMyyyy") + time;
                                DateTime orderDateTime = DateTime.ParseExact(time, "ddMMyyyyHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

                                level.Price = price;

                                fragment.mdLevel.Add(level); // для первичного заполнения стакана

                                newOrderChange.UniqueName = uniqueName;
                                newOrderChange.MDEntryID = Id;
                                newOrderChange.Price = price;
                                newOrderChange.Volume = volume;
                                newOrderChange.OrderType = orderType;
                                newOrderChange.DateTime = orderDateTime;
                                newOrderChange.Action = string.Empty;

                                _depthChanges[uniqueName].Add(newOrderChange);

                                timeForDepth = orderDateTime;

                                //  WriteLogOrders($"{orderSide}, price: {price}, volume: {volume}, status: {ordrerStatus}", "Fragment");
                              
                            }
                        }

                        // добавляем сообщение в снэпшот и проверяем его готовность
                        if (_ordersSnapshotsByName[uniqueName].SnapshotFragments == null)
                        {
                            _ordersSnapshotsByName[uniqueName].SnapshotFragments = new List<SnapshotFragment>();
                            _ordersSnapshotsByName[uniqueName].SnapshotFragments.Add(fragment);
                            _ordersSnapshotsByName[uniqueName].RptSeq = fragment.RptSeq;
                            _ordersSnapshotsByName[uniqueName].Symbol = fragment.Symbol;
                            _ordersSnapshotsByName[uniqueName].TradingSessionID = fragment.TradingSessionID;
                        }
                        else
                        {
                            _ordersSnapshotsByName[uniqueName].SnapshotFragments.Add(fragment);
                        }


                             // если снэпшот сформирован
                        if (_ordersSnapshotsByName[uniqueName].IsComletedSnapshot(_ordersSnapshotsByName[uniqueName].SnapshotFragments) == true)
                        {
                            WriteLogOrders($"Снэпшот orders по инструменту {uniqueName} сформирован. Содержит {_ordersSnapshotsByName[uniqueName].SnapshotFragments.Count} фрагментов.", "OrdersMessageReader");
                            // и его RptSeq больше минимального RptSeq из инкримента, то применяем обновление
                            if ( _ordersSnapshotsByName[uniqueName].RptSeq >= minRptSeqFromOrders[uniqueName])
                            {
                                WriteLogOrders($"RptSeq >= minRptSeqFromOrders[uniqueName] по инструменту {uniqueName}. Начинаю делать стакан", "OrdersSnapshot");
 
                                // формируем первый стакан из снэпшота

                                 MarketDepth marketDepth = new MarketDepth();

                                marketDepth = MakeFirstDepth(uniqueName, _ordersSnapshotsByName);

                               marketDepth.SecurityNameCode = name;
                               marketDepth.Time = timeForDepth;
                               MarketDepthEvent(marketDepth);

                                int rptSeqFromSnapshot = _ordersSnapshotsByName[uniqueName].RptSeq;

                                // просто для логов, из прода удалить
                                WriteLogOrders($"В ордерах по инструменту {uniqueName} содержится {_depthChanges[uniqueName].Count} изменений.\n---------------------------------------", "OrdersMessageReader");
                                for (int y = 0; y < _depthChanges[uniqueName].Count; y++)
                                {
                                    var dat = _depthChanges[uniqueName][y];
                                    WriteLogOrders($"Заявка {dat.MDEntryID} Price: {dat.Price}-{dat.OrderType}, Volume: {dat.Volume}, Action: {dat.Action}", "Changes");
                                }


                                // меняем стакан, согласно ожидающих заявок
                                for (int j = 0; j < _waitingDepthChanges[uniqueName].Count; j++)
                                {
                                    if (_waitingDepthChanges[uniqueName][j].RptSeq < rptSeqFromSnapshot)
                                        continue;

                                    UpdateOrderData(uniqueName, _waitingDepthChanges[uniqueName][j]);
                                }

                                WriteLogOrders($"Обработаны ожидающие заявки", "UpdateDepth");

                                _ordersSnapshotsByName[uniqueName].SnapshotWasApplied = true;

                                // отправить информацию о том, что поток снэпшота по этому инструменту можно не слушать
                                // и когда по всем подписанным иснтрументам будут применены обновления, закрыть сокеты снэпшотов
                            }
                        }
                    }

                }
                catch(Exception e)
                {
                    WriteLogOrders($"Ошибка в потоке вторичной обработки ордеров: {e}", "OrderMessageReader");
                }

            }

        }



        // вспомогательные методы для обработки фаст сообщений

        private void UpdateDepth(List<OrderChange> orderChanges, string securityName, DateTime timeDepth)
        {
            MarketDepth marketDepth = new MarketDepth();

            List<MarketDepthLevel> asks = new List<MarketDepthLevel>();
            List<MarketDepthLevel> bids = new List<MarketDepthLevel>();

            marketDepth.SecurityNameCode = securityName;

            var asksByPrice = orderChanges.FindAll(p => p.OrderType == "ask");
            asksByPrice.Sort((x, y) => x.Price.CompareTo(y.Price));

            for (int i = 1; i < asksByPrice.Count; i++)
            {
                if (asksByPrice[i].Price == asksByPrice[i - 1].Price)
                {
                    asksByPrice[i].Volume += asksByPrice[i - 1].Volume;
                    asksByPrice.RemoveAt(i - 1);
                }
                   
            }

            for (int k = 0; k < asksByPrice.Count; k++)
            {
                 asks.Add(new MarketDepthLevel()
                {
                    Ask = asksByPrice[k].Volume,
                    Price = asksByPrice[k].Price
                });

            }

            var bidsByPrice = orderChanges.FindAll(p => p.OrderType == "bid");
            bidsByPrice.Sort((y, x) => x.Price.CompareTo(y.Price));


            for (int j = 1; j < bidsByPrice.Count; j++)
            {
                if (bidsByPrice[j].Price == bidsByPrice[j - 1].Price)
                {
                    bidsByPrice[j].Volume += bidsByPrice[j - 1].Volume;
                    bidsByPrice.RemoveAt(j - 1);
                }

            }

            for (int l = 0; l < bidsByPrice.Count; l++)
            {
                bids.Add(new MarketDepthLevel()
                {
                    Ask = bidsByPrice[l].Volume,
                    Price = bidsByPrice[l].Price
                });

            }

            marketDepth.Asks = asks;
            marketDepth.Bids = bids;

            marketDepth.Time = timeDepth;

            MarketDepthEvent(marketDepth);
        }

        /// <summary>
        /// Внести изменения в данные по заявкам, находящимся в стакане
        /// </summary>
        /// <param name="uniqueName">уникальное имя инструмента</param>
        /// <param name="orderChange">новые данные для изменения заявок в стакане</param>
        private void UpdateOrderData(string uniqueName, OrderChange orderChange)
        {
          var order =  _depthChanges[uniqueName].Find(p => p.MDEntryID == orderChange.MDEntryID);

            if (order != null)
            {
                switch (orderChange.Action)
                {
                    case "add":
                        WriteLogOrders($" В заявку {order.MDEntryID}-{order.OrderType} по цене: {order.Price} с объемом {order.Volume} было добавлено: {orderChange.Volume}", "UpdateDepth");
                        order.Volume += orderChange.Volume;
                        break;

                    case "change":
                        WriteLogOrders($" Заявка {order.MDEntryID}-{order.OrderType} по цене: {order.Price} с объемом {order.Volume} была изменена на объем: {orderChange.Volume}", "UpdateDepth");
                        order.Volume = orderChange.Volume;
                        break;

                    case "delete":
                        WriteLogOrders($" Заявка {order.MDEntryID}-{order.OrderType} по цене: {order.Price} с объемом {order.Volume} была удалена", "UpdateDepth");
                        int index = _depthChanges[uniqueName].FindIndex(p => p.MDEntryID == orderChange.MDEntryID);
                        if (index != -1)
                            _depthChanges[uniqueName].RemoveAt(index);
                        else
                            SendLogMessage($"Change index for delete order not found", LogMessageType.Error);
                        break;

                    default:
                        SendLogMessage($"Action for change order not found", LogMessageType.Error);
                        break;
                }
            }
            else
            {
                _depthChanges[uniqueName].Add(orderChange);

                WriteLogOrders($" Новая заявка {orderChange.MDEntryID}-{orderChange.OrderType} по цене: {orderChange.Price} с объемом {orderChange.Volume} была добавлена в список", "UpdateDepth");
            }
        }

        /// <summary>
        /// Собрать первый стакан из снэпшота
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <param name="ordersSnapshotsByName"></param>
        /// <returns></returns>
        private MarketDepth MakeFirstDepth(string uniqueName, Dictionary<string, Snapshot> ordersSnapshotsByName)
        {
            MarketDepth marketDepth = new MarketDepth();
            List<MarketDepthLevel> asks = new List<MarketDepthLevel>();
            List<MarketDepthLevel> bids = new List<MarketDepthLevel>();

            for (int i = 0; i < ordersSnapshotsByName[uniqueName].SnapshotFragments.Count; i++)
            {
                List<MarketDepthLevel> _mdLevels = ordersSnapshotsByName[uniqueName].SnapshotFragments[i].mdLevel;

                for (int k = 0; k < _mdLevels.Count; k++)
                {
                    MarketDepthLevel level = new MarketDepthLevel();

                    if(_mdLevels[k].Ask != 0)
                    {
                        MarketDepthLevel levelFromAsks =  asks.Find(a => a.Price == _mdLevels[k].Price);

                        if (levelFromAsks != null) //  если в асках уже есть уровень с этой ценой, прибавляем объем
                            levelFromAsks.Ask += _mdLevels[k].Ask;
                        else
                        {
                            level.Ask = _mdLevels[k].Ask;
                            level.Price = _mdLevels[k].Price;
                            asks.Add(level);
                        }
                    }
                    if (_mdLevels[k].Bid != 0)
                    {
                        MarketDepthLevel levelFromBids = bids.Find(b => b.Price == _mdLevels[k].Price);

                        if (levelFromBids != null) //  если в бидах уже есть уровень с этой ценой, прибавляем объем
                            levelFromBids.Bid += _mdLevels[k].Bid;
                        else
                        {
                            level.Bid = _mdLevels[k].Bid;
                            level.Price = _mdLevels[k].Price;
                            bids.Add(level);
                        }
                    }
                }
            }

            asks.Sort((x, y) => x.Price.CompareTo(y.Price));
            bids.Sort((y, x) => x.Price.CompareTo(y.Price));

            marketDepth.Asks = asks;
            marketDepth.Bids = bids;

            WriteLogOrders($"Первый стакан по инструменту {uniqueName} сформирован. Бидов: {bids.Count}, Асков:{asks.Count}.\n" +
                $"[Price] [Volume]\n[{asks[0].Price}] [{asks[0].Ask}]\n----------------------\n[{bids[0].Price}] [{bids[0].Bid}]\n", "MakeFirstDepth");

            return marketDepth;
        }


        private bool IsMessageMissed(Dictionary<long, OpenFAST.Message> dictFastMsg, long msgSeqNum, OpenFAST.Message msg)
        {
                // проверяем нет ли сообщения с таким номером
                if (dictFastMsg.ContainsKey(msgSeqNum))
                {
                    if (dictFastMsg[msgSeqNum] == msg)
                    {
                        return false; // такое сообщение уже есть
                    }
                    else
                    {
                        dictFastMsg[msgSeqNum] = msg;
                        return false;
                    }
                }
                else
                {
                    dictFastMsg.Add(msgSeqNum, msg);
                    return true;
                }
        }

        private bool IsDataMissed(List<NumbersData> nums, out long beginMsgSeqNum, out long endMsgSeqNum)
        {
            // проверяем пропуски данных
            beginMsgSeqNum = 0; // начало пропущенных данных
            endMsgSeqNum = 0; // конец пропущенных данных

            if (nums.Count < 2)
                return false;

            nums.Sort((x, y) => x.RptSeq.CompareTo(y.RptSeq));

            for (int i = 1; i < nums.Count; i++)
            {
                if (nums[i].RptSeq != nums[i - 1].RptSeq + 1)
                {
                    if (beginMsgSeqNum == 0)
                        beginMsgSeqNum = nums[i - 1].MsgSeqNum + 1;

                    endMsgSeqNum = nums[i].MsgSeqNum - 1;

                }
            }

            if (beginMsgSeqNum != 0)
                return true; // данные пропущены
           else return false;
        }

        private bool IsSubscribedToThisSecurity(string uniqueName)
        {
            for (int j = 0; j < _subscribledSecutiries.Count; j++)
            {
                if (_subscribledSecutiries[j] == uniqueName)
                {
                    return true;
                }
          
            }
            return false;
        }

        private void WriteLog(string message, string source)
        {
            lock (_logLock)
            {
                _logFile.WriteLine($"{DateTime.Now} {source}: {message}");
            }
        }

        private void WriteLogOrders(string message, string source)
        {
            lock (_logLockOrder)
            {
              _logFileOrders.WriteLine($"{DateTime.Now} {source}: {message}");
            }
        }

        /// <summary>
        /// Восстановление данных по TCP
        /// </summary>
        private void RecoverData()
        {
            byte[] buffer = new byte[4096];
            byte[] sizeBuffer = new byte[4];

            string currentFeed = "OLR";

            string fastPoint = "TEST";

            Thread.Sleep(1000);

            while (true)
            {
                try
                {
                    if (_historicalReplayEndPoint == null)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    // проверяем нужно ли восстанавливать какие-либо данные
                    if (_missingOLRBeginSeqNo > 0 && _missingOLRData && !_ordersIncremental.ContainsKey(_missingOLRBeginSeqNo))
                    {
                        currentFeed = "OLR"; // восстанавливаем данные из потока ордеров

                        SendLogMessage($"Trying to recover missing OLR SeqNo: {_missingOLRBeginSeqNo}-{_missingOLREndSeqNo}", LogMessageType.System);
                    }
                    else
                    if (_missingTLRBeginSeqNo > 0 && _missingTLRData && !_tradesIncremental.ContainsKey(_missingTLRBeginSeqNo))
                    {
                        currentFeed = "TLR"; // восстанавливаем данные из потока сделок
                        SendLogMessage($"Trying to recover missing TLR SeqNo: {_missingTLRBeginSeqNo}-{_missingTLREndSeqNo}", LogMessageType.System);
                    }
                    else
                    {
                        // отдыхаем полсекунды, ничего не надо восстанавливать
                        Thread.Sleep(500);
                        continue;
                    }

                    lock (_socketLockerHistoricalReplay)
                    {
                        // init historical replay socket
                        _historicalReplaySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        // начинаем общение с 1
                        int msgSeqNum = 1;

                        // 1. Соединяемся с потоком восстановления по TCP
                        _historicalReplaySocket.Connect(_historicalReplayEndPoint);

                        // 2. Отправляем запрос на подключение к потоку восстановления

                        FASTHeader header = new FASTHeader();
                        header.MsgType = "A"; //Тип сообщения на установку сессии
                        header.SenderCompID = "OsEngine";
                        header.TargetCompID = "MOEX"; // TODO: id фирмы брокера (?)
                        header.MsgSeqNum = msgSeqNum++;

                        FASTLogonMessage logonMessageBody = new FASTLogonMessage();
                        logonMessageBody.Username = "user0";
                        logonMessageBody.Password = "pass0";

                        //Отправляем сообщение
                        SendFIXMessage(_historicalReplaySocket, header, logonMessageBody);

                        // получаем ответ - должен быть Logon
                        sizeBuffer = new byte[4];
                        int length = 0;
                        try
                        {
                            length = _historicalReplaySocket.Receive(sizeBuffer, 4, SocketFlags.None);
                        }
                        catch (Exception ex)
                        {
                            SendLogMessage("Error receiving Data " + ex.ToString(), LogMessageType.Error);
                            if (ServerStatus != ServerConnectStatus.Disconnect)
                            {
                                ServerStatus = ServerConnectStatus.Disconnect;
                                DisconnectEvent();
                            }
                        }

                    }

                }
                catch 
                { 
                
                }

            }
        }




                        // FIX
                        List<string> _tagsAndValues = new List<string>();

        private void FixMessageReader()
        {

            Thread.Sleep(1000);

            while (true)
            {
                /* Каждый новый торговый день FIX клиент должен отправлять сообщение Logon (A) с порядковым номером 1. 
                 * Каждый раз при повторном подключении к серверу MFIX Transactional в течение дня FIX клиент 
                 * должен отправлять сообщение Logon (A) с порядковым номером, который больше на 1, 
                 * чем у последнего сообщения в исходящем логе.
                 */



                try
                {
                    if (ServerStatus == ServerConnectStatus.Disconnect)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    if (!_stream.DataAvailable)
                    {
                       continue;
                    }

                    byte[] dataAns = new byte[1024];

                    int bytesRead = _stream.Read(dataAns, 0, dataAns.Length);
                    string _fixMessageFromMOEX = Encoding.ASCII.GetString(dataAns);
                    _tagsAndValues.AddRange(_fixMessageFromMOEX.Split(new char[] { '\u0001' }));
 
                    int newFixMsgNum = Convert.ToInt32(GetValueByTag("34", _tagsAndValues));

                    // проверка очередности входящих сообщений
                    if( newFixMsgNum == _incomingFixMsgNum + 1 )
                    {
                        _incomingFixMsgNum = newFixMsgNum;
                    }
                    else if (newFixMsgNum > _incomingFixMsgNum + 1)
                    {
                        // отправить Resend Request(2), в котором должен быть  указан диапазон  порядковых номеров  пропущенных сообщений(BeginSeqNo, EndSeqNo)
                        int beginSeqNo = _incomingFixMsgNum + 1;
                        int endSeqNo = newFixMsgNum - 1;

                        string resendMsg = _messageConstructor.ResendMessage(_msgSeqNum, beginSeqNo, endSeqNo);
                        SendMessage(resendMsg);
                    }
                    else
                    {
                        /* Если одна из сторон получила сообщение с установленным или незаполненным флагом PossDupFlag,
                         * у которого порядковый номер меньше,  чем  ожидается,  то  это  свидетельствует  о  серьезной  ошибке. 
                         * В  этом  случае  рекомендуется  закрыть  сессию  и  обратиться  к администратору.
                         */
                        SendLogMessage("Получено сообщение с порядковыим номером меньше, чем ожидалось. Обратитесь к администратору.", LogMessageType.Error);
                        string logout = _messageConstructor.LogoutMessage(_msgSeqNum);
                        SendMessage(logout);
                        _logoutInitiator = true;
                    }

                    string typeMsg = GetValueByTag("35", _tagsAndValues);

                    switch (typeMsg)
                    {
                        case "A": // Logon
                            SendLogMessage("FIX connection open", LogMessageType.System);
                            ServerStatus = ServerConnectStatus.Connect;
                            ConnectEvent();
                            break;
                        case "0": // Hearbeat
                            string hrbtMsg = _messageConstructor.HeartbeatMessage(_msgSeqNum, false, null);
                            SendMessage(hrbtMsg);
                            break;
                        case "1": // Test Request
                            string testReqId = GetValueByTag("112", _tagsAndValues);
                            if (testReqId != string.Empty)
                            {
                                string hrbtMsgForTest = _messageConstructor.HeartbeatMessage(_msgSeqNum, true, testReqId);
                                SendMessage(hrbtMsgForTest);
                            }
                            break;
                        case "5":
                            if(!_logoutInitiator)
                            {
                                string logout = _messageConstructor.LogoutMessage(_msgSeqNum);
                                SendMessage(logout);
                            }
                            SendLogMessage("FIX connection closed", LogMessageType.System);
                            ServerStatus = ServerConnectStatus.Disconnect;
                            DisconnectEvent();
                            break;

                        case "3":
                            RejectMessage rejectMessage = new RejectMessage();
                            string reason;
                            if (rejectMessage.sessionRejectReason.TryGetValue(GetValueByTag("373", _tagsAndValues), out reason))
                            {
                                SendLogMessage($"The message Regect has been received. Reson: {reason}. {GetValueByTag("58", _tagsAndValues)}", LogMessageType.Error);
                            }
                            break;

                        case "h":
                            TradingSessionStatus status = new TradingSessionStatus();
                            string msgStatus;
                            if(status.tradSesStatus.TryGetValue(GetValueByTag("340", _tagsAndValues), out msgStatus))
                            {
                                SendLogMessage($"Trading Session Status: {msgStatus}. {GetValueByTag("58", _tagsAndValues)}", LogMessageType.System);
                            }
                            break;

                        case "8":
                            //SendLogMessage(ExequtionReportRead(GetValueByTag("11", _tagsAndValues), GetValueByTag("37", _tagsAndValues), GetValueByTag("150", _tagsAndValues),
                            //                                  GetValueByTag("39", _tagsAndValues), GetValueByTag("636", _tagsAndValues), GetValueByTag("103", _tagsAndValues),
                            //                                  GetValueByTag("58", _tagsAndValues)), LogMessageType.Error);

                            break;
                        case "9":
                            //  формируется в случае получения некорректного запроса на снятие или изменение заявки
                            SendLogMessage($"Order Cancel Reject (9). Reason: {(GetValueByTag("58", _tagsAndValues))} ", LogMessageType.Error);
 
                            break;

                        case "4":
                            /* На протяжении торгового дня, в случае, если MFIX Transactional не может корректно повторно отправить
                             * пропущенные клиентом сообщения в ответ на Resend Request (2) сообщение,
                             * например, в случае, если произошел сбой и некоторые потерянные сообщения нельзя восстановить, 
                             * тогда MFIX Transactional предлагает увеличить порядковый номер сообщений (с возможной потерей данных) 
                             * и продолжить с него, т.е. формирует сообщение Sequence Reset (4) с GapFillFlag (123) = N (Sequence Reset)
                             * и NewSeqNo (36) = <новый порядковый номер>.
                             */

                            if(GetValueByTag("123", _tagsAndValues) == "N")
                            {
                              _incomingFixMsgNum = Convert.ToInt32(GetValueByTag("36", _tagsAndValues));
                            }
                            SendLogMessage("The message SequenceReset has been received", LogMessageType.Error);
                            break;

                        case "2":
                            // сервер просит отправить ему сообщения с GetValueByTag("7", _tagsAndValues) до GetValueByTag("16", _tagsAndValues)
                            // если GetValueByTag("16", _tagsAndValues) == "0", то просит все, начиная с указанного
                            // отправить Sequence Reset (4) с режимом заполнения пробелов GapFillFlag (123) field = "Y"

                            SendLogMessage("The message ResendRequest has been received", LogMessageType.NoName);
                            break;

                        default:
                            SendLogMessage($" Message type: {typeMsg}. Template selection error", LogMessageType.Error);
                            break;
                    }

                    _tagsAndValues.Clear();












                        //if (action.arg != null)
                        //{
                        //    if (action.arg.channel.Equals("books15"))
                        //    {
                        //        UpdateDepth(message);
                        //        continue;
                        //    }
                        //    if (action.arg.channel.Equals("trade"))
                        //    {
                        //        UpdateTrade(message);
                        //        continue;
                        //    }
                        //    if (action.arg.channel.Equals("orders"))
                        //    {
                        //        UpdateOrder(message);
                        //        continue;
                        //    }
                        //}
                    
                }
                catch (Exception exeption)
                {
                    SendLogMessage(exeption.ToString(), LogMessageType.Error);
                    Thread.Sleep(3000);
                }
            }
        }


        private string GetValueByTag(string tag, List<string> tv)
        {
            int index = tv.FindIndex(p => p.Contains(tag + "="));
            if (index != -1)
            {
                return tv[index].Substring(tv[index].IndexOf('=') + 1);
            }
            else
            {
                SendLogMessage($"Tag {tag} not found!", LogMessageType.Error);
                return string.Empty;
            }
        }



        private void UpdateMytrade(string json)
        {
          //  ResponseMessageRest<List<ResponseMyTrade>> responseMyTrades = JsonConvert.DeserializeAnonymousType(json, new ResponseMessageRest<List<ResponseMyTrade>>());


            //for (int i = 0; i < responseMyTrades.data.Count; i++)
            //{
              //  ResponseMyTrade responseT = responseMyTrades.data[i];

                MyTrade myTrade = new MyTrade();

                //myTrade.Time = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(responseT.cTime));
                //myTrade.NumberOrderParent = responseT.orderId;
                //myTrade.NumberTrade = responseT.fillId.ToString();
                //myTrade.Price = responseT.fillPrice.ToDecimal();
                //myTrade.SecurityNameCode = responseT.symbol.ToUpper().Replace("_SPBL", "");
                //myTrade.Side = responseT.side.Equals("buy") ? Side.Buy : Side.Sell;


                //if (string.IsNullOrEmpty(responseT.feeCcy) == false
                //    && string.IsNullOrEmpty(responseT.fees) == false
                //    && responseT.fees.ToDecimal() != 0)
                //{// комиссия берёться в какой-то монете
                //    string comissionSecName = responseT.feeCcy;

                //    if (myTrade.SecurityNameCode.StartsWith("BGB")
                //        || myTrade.SecurityNameCode.StartsWith(comissionSecName))
                //    {
                //        myTrade.Volume = responseT.fillQuantity.ToDecimal() + responseT.fees.ToDecimal();
                //    }
                //    else
                //    {
                //        myTrade.Volume = responseT.fillQuantity.ToDecimal();
                //    }
                //}
                //else
                //{// не известная монета комиссии. Берём весь объём
                //    myTrade.Volume = responseT.fillQuantity.ToDecimal();
                //}

            //    MyTradeEvent(myTrade);
            //}

        }

        private void UpdateOrder(string message)
        {
            //ResponseWebSocketMessageAction<List<ResponseWebSocketOrder>> Order = JsonConvert.DeserializeAnonymousType(message, new ResponseWebSocketMessageAction<List<ResponseWebSocketOrder>>());

            //if (Order.data == null ||
            //    Order.data.Count == 0)
            //{
            //    return;
            //}

            //for (int i = 0; i < Order.data.Count; i++)
            //{
            //    var item = Order.data[i];

            //    OrderStateType stateType = GetOrderState(item.status);

            //    if (item.ordType.Equals("market") && stateType == OrderStateType.Activ)
            //    {
            //        continue;
            //    }

            //    Order newOrder = new Order();
            //    newOrder.SecurityNameCode = item.instId.Replace("_SPBL", "");
            //    newOrder.TimeCallBack = TimeManager.GetDateTimeFromTimeStamp(Convert.ToInt64(item.cTime));

            //    if (!item.clOrdId.Equals(String.Empty) == true)
            //    {
            //        try
            //        {
            //            newOrder.NumberUser = Convert.ToInt32(item.clOrdId);
            //        }
            //        catch
            //        {
            //            // ignore
            //        }
            //    }

            //    newOrder.NumberMarket = item.ordId.ToString();
            //    newOrder.Side = item.side.Equals("buy") ? Side.Buy : Side.Sell;
            //    newOrder.State = stateType;
            //    newOrder.Volume = item.sz.Replace('.', ',').ToDecimal();
            //    newOrder.Price = item.avgPx.Replace('.', ',').ToDecimal();
            //    if (item.px != null)
            //    {
            //        newOrder.Price = item.px.Replace('.', ',').ToDecimal();
            //    }
            //    newOrder.ServerType = ServerType.BitGetSpot;
            //    newOrder.PortfolioNumber = "BitGetSpot";

            //    if (stateType == OrderStateType.Done ||
            //        stateType == OrderStateType.Patrial)
            //    {
            //        // как только приходит ордер исполненный или частично исполненный триггер на запрос моего трейда по имени бумаги
            //        CreateQueryMyTrade(newOrder.SecurityNameCode + "_SPBL", newOrder.NumberMarket);

            //        if (DateTime.Now.AddSeconds(-45) < TimeToUprdatePortfolio)
            //        {
            //            TimeToUprdatePortfolio = DateTime.Now.AddSeconds(-45);
            //        }
            //    }
            //    MyOrderEvent(newOrder);
            //}
        }

        private OrderStateType GetOrderState(string orderStateResponse)
        {
            OrderStateType stateType;

            switch (orderStateResponse)
            {
                case ("init"):
                case ("new"):
                    stateType = OrderStateType.Activ;
                    break;
                case ("partial_fill"):
                case ("partial-fill"):
                    stateType = OrderStateType.Patrial;
                    break;
                case ("full_fill"):
                case ("full-fill"):
                    stateType = OrderStateType.Done;
                    break;
                case ("cancelled"):
                    stateType = OrderStateType.Cancel;
                    break;
                default:
                    stateType = OrderStateType.None;
                    break;
            }

            return stateType;
        }

        public event Action<Order> MyOrderEvent;

        public event Action<MyTrade> MyTradeEvent;

        public event Action<MarketDepth> MarketDepthEvent;

        public event Action<Trade> NewTradesEvent;

        #endregion

        #region 10 Trade

 
        public void SendOrder(Order order)
        {

            string[] instrumentParams = new string[2] {order.SecurityNameCode, "FXSPOT" }; // продумать заполнение

            string ordType = order.TypeOrder == OrderPriceType.Market ? "1" : "2";
            string price = order.TypeOrder == OrderPriceType.Market ? null : order.Price.ToString().Replace(",", ".");

            string newOrder = _messageConstructor.NewOrderMessage(
                    order.NumberUser.ToString(),
                    null,
                    instrumentParams,
                    order.Volume.ToString().Replace(",", "."),
                    order.PortfolioNumber,
                    null,
                    false,
                    order.SecurityClassCode,
                    ((byte)order.Side).ToString(),
                    ordType,
                    price, _msgSeqNum);

            SendMessage(newOrder);



        }

        public void CancelOrder(Order order)
        {
         
        }

        public void ChangeOrderPrice(Order order, decimal newPrice)
        {

        }

        public void CancelAllOrders()
        {

        }

        public void CancelAllOrdersToSecurity(Security security)
        {
          


        }



        private void CreateOrderFail(Order order)
        {
            order.State = OrderStateType.Fail;

            if (MyOrderEvent != null)
            {
                MyOrderEvent(order);
            }
        }

        public void GetAllActivOrders()
        {
  
              
        }

        public void GetOrderStatus(Order order)
        {
           
                  
        }

        #endregion

        #region 11 Queries

        private void SendMessage(string message)
        {
            byte[] dataMes = Encoding.ASCII.GetBytes(message);
            _stream.Write(dataMes, 0, dataMes.Length);

            _msgSeqNum++;
        }

        private void GetDataTCP()
        {
            CreateSocketConnections(GetAddressesForFastConnect("Historical Replay"));

           
        }


        // if (feedType == "Historical Replay") для восстановления данных воткнуть позже
        //  {
        //  string ipAddressString = connectionNode.SelectNodes("ip")[0].InnerText; // берем второй адрес
        //string portString = connectionNode.SelectSingleNode("port").InnerText;
        //IPAddress ipAddr = IPAddress.Parse(ipAddressString);
        //_historicalReplayEndPoint = new IPEndPoint(ipAddr, int.Parse(portString));
        //                continue;
        // }

    public List<FastConnection> GetAddressesForFastConnect(string connectionType)
        {
            // прочитать конфиг FIX/FAST соединения и вернуть список с адресами подключения

            List<FastConnection> fastConnections = new List<FastConnection> ();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(_configDir + "\\config.xml");

            XmlNode chanelNode = xmlDoc.SelectSingleNode("/configuration/channel");

            XmlNodeList connectNodes = chanelNode.SelectSingleNode("connections").SelectNodes("connection");

            for (int i = 0; i < connectNodes.Count; i++)
            {
                string feedType = connectNodes[i].SelectSingleNode("type").Attributes["feed-type"].Value;

                if (!feedType.Equals(connectionType)) continue;

                if (feedType.Equals("Historical Replay"))
                {
                    FastConnection connection = new FastConnection();

                    var repadr = connectNodes[i] as XmlNode;

                    if (repadr != null)
                    {
                        connection.FeedType = feedType;
                        connection.SrsIP = repadr.SelectSingleNode("ip").InnerText;
                        connection.Port = Convert.ToInt32(repadr.SelectSingleNode("port").InnerText);
                    }
                    else { SendLogMessage("Couldn't get a TCP recovery address", LogMessageType.Error); }

                    fastConnections.Add(connection);
                    return fastConnections;
                }
               
                XmlNodeList feed = connectNodes[i].SelectNodes("feed");

                for (int j = 0; j < feed.Count; j++)
                {
                    FastConnection connection = new FastConnection();

                    connection.FeedType = feedType;
                    connection.FeedID = feed[j].Attributes["id"].Value;
                    connection.SrsIP = feed[j].SelectSingleNode("src-ip").InnerText;
                    connection.MulticastIP = feed[j].SelectSingleNode("ip").InnerText;
                    connection.Port = Convert.ToInt32(feed[j].SelectSingleNode("port").InnerText);

                    fastConnections.Add(connection);

                }

            }

            return fastConnections;
        }

        #endregion

        #region 12 Log

        public event Action<string, LogMessageType> LogMessageEvent;

        private void SendLogMessage(string message, LogMessageType messageType)
        {
            LogMessageEvent(message, messageType);
        }

        #endregion


    }
}
