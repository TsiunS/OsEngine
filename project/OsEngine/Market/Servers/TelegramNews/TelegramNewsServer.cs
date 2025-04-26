using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Servers.Entity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OsEngine.Market.Servers.TelegramNews.TGAuthEntity;
using TL;
using Message = TL.Message;
using System.IO;


namespace OsEngine.Market.Servers.TelegramNews
{
    class TelegramNewsServer : AServer
    {
        public TelegramNewsServer()
        {
            TelegramNewsServerRealization realization = new TelegramNewsServerRealization();
            ServerRealization = realization;

            CreateParameterString("Telegram channel IDs", "");
            CreateParameterInt("API ID", 0);
            CreateParameterPassword("API Hash", "");
            CreateParameterString("Phone number", "");
        }
    }

    public class TelegramNewsServerRealization : IServerRealization
    {
        #region 1 Constructor, Status, Connection

        public TelegramNewsServerRealization()
        {
            ServerStatus = ServerConnectStatus.Disconnect;

            string logDir = @"Engine\Log\TelegramLogs";
            Directory.CreateDirectory(logDir); // Создаём папку, если её нет

            var logFile = new FileInfo(Path.Combine(logDir, "wteleg.log"));

            if (logFile.Exists && logFile.Length > 1024 * 1024)
                File.WriteAllText(logFile.FullName, "");

            // Настройка логов
            WTelegram.Helpers.Log = (level, message) =>
            {
                if (level == 4) // Фильтруем только ошибки
                {
                    SendLogMessage(message, LogMessageType.Error);
                }

                string logPath = Path.Combine(logDir, "wteleg.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] LogEvent : {message}\n");
            };
        }

        public DateTime ServerTime { get; set; }

        public void Connect()
        {
            _channelsIDsString = ((ServerParameterString)ServerParameters[0]).Value;
            _apiID = ((ServerParameterInt)ServerParameters[1]).Value;
            _apiHash = ((ServerParameterPassword)ServerParameters[2]).Value;
            _phoneNumber = ((ServerParameterString)ServerParameters[3]).Value;

            if (string.IsNullOrEmpty(_channelsIDsString))
            {
                SendLogMessage("Can`t run connector. ID Telegram channel not are specified", LogMessageType.Error);
                return;
            }

            if (_apiID <= 0)
            {
                SendLogMessage("Can`t run connector. Enter API ID.", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_apiHash))
            {
                SendLogMessage("Can`t run connector. API Hash not are specified", LogMessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(_phoneNumber))
            {
                SendLogMessage("Can`t run connector. Phone number not are specified", LogMessageType.Error);
                return;
            }

            string[] tgChannelsIDs = _channelsIDsString.Split(',');

            for (int i = 0; i < tgChannelsIDs.Length; i++)
            {
                if (tgChannelsIDs[i].StartsWith("-100"))
                {
                    _trackedChats.Add(tgChannelsIDs[i].Substring(4), "");
                }
                else if (tgChannelsIDs[i].StartsWith("-"))
                {
                    _trackedChats.Add(tgChannelsIDs[i].Substring(1), "");
                }
                else
                {
                    _trackedChats.Add(tgChannelsIDs[i], "");
                }
            }

            if (_trackedChats.Count == 0)
            {
                SendLogMessage("There is not a single channel to download", LogMessageType.Error);
                return;
            }

            try
            {
                _client = new WTelegram.Client(Config);

                if (CheckConnect())
                {
                    ServerStatus = ServerConnectStatus.Connect;
                    ConnectEvent();

                    _client.OnOther += Client_OnOther;

                    Task<Messages_Chats> chats = _client.Messages_GetAllChats();

                    if (chats != null)
                    {
                        Dictionary<long, ChatBase>.Enumerator en = chats.Result.chats.GetEnumerator();

                        while (en.MoveNext())
                        {
                            if (_trackedChats.ContainsKey(en.Current.Key.ToString()))
                            {
                                _trackedChats[en.Current.Key.ToString()] = en.Current.Value.Title;
                            }
                        }
                    }

                    Thread.Sleep(100);

                    Task<Messages_Dialogs> dialogs = _client.Messages_GetAllDialogs();

                    if (dialogs != null)
                    {
                        Dictionary<long, User>.Enumerator enr = dialogs.Result.users.GetEnumerator();

                        while (enr.MoveNext())
                        {
                            if (_trackedChats.ContainsKey(enr.Current.Key.ToString()))
                            {
                                _trackedChats[enr.Current.Key.ToString()] = enr.Current.Value.first_name;
                            }
                        }
                    }

                    Thread.Sleep(100);

                    Manager = _client.WithUpdateManager(UpdatesMessages);
                }
                else
                {
                    SendLogMessage("Authorization failed. See the log file", LogMessageType.Error);
                    return;
                }
            }
            catch (RpcException ex) when (ex.Code == 401) // AUTH_KEY_UNREGISTERED
            {
                SendLogMessage("The session is invalid. Delete the WTelegram.session log file and try again.", LogMessageType.Error);
                return;
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.Message, LogMessageType.Error);
                return;
            }
        }

        private async Task<bool> CheckConnectAsync()
        {
            try
            {
                // 1. Проверяем авторизацию
                var my = await _client.LoginUserIfNeeded();

                if (my == null)
                {
                    SendLogMessage("Authorization error", LogMessageType.Error);
                    return false;
                }

                SendLogMessage($"Successful login! ID: {my.id}, Name: {my.first_name}", LogMessageType.Connect);

                // 2. Проверяем доступность API
                var config = await _client.Help_GetConfig();
                SendLogMessage($"Current DC: {config.this_dc}", LogMessageType.Connect);

                return true;
            }
            catch (RpcException rpcEx)
            {
                SendLogMessage($"Telegram API Error: {rpcEx.Message} (code: {rpcEx.Code})", LogMessageType.Error);
                return false;
            }
            catch (Exception ex)
            {
                SendLogMessage($"Critical error: {ex.Message}", LogMessageType.Error);
                return false;
            }
        }

        // Синхронная обёртка
        private bool CheckConnect()
        {
            return Task.Run(() => CheckConnectAsync()).GetAwaiter().GetResult();
        }

        private static string Config(string what)
        {
            switch (what)
            {
                case "api_id": return _apiID.ToString();
                case "api_hash": return _apiHash;
                case "phone_number": return _phoneNumber;
                case "session_pathname": return @"Engine\Log\TelegramLogs\WTelegram.session";
                case "device_model": return "OsEngine";
                case "verification_code": return GetCode();
                case "password": return GetPassword();     // if user has enabled 2FA
                default: return null;                  // let WTelegramClient decide the default config
            }
        }

        private static string GetPassword()
        {
            string password = "";

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AuthTGPasswordDialogUi dialog = new AuthTGPasswordDialogUi();

                if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.Password))
                {
                    password = dialog.Password;
                }
                else
                {
                    _client?.Dispose();
                }
            });

            return password;
        }

        private static string GetCode()
        {
            string code = "";

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AuthTGCodeDialogUi dialog = new AuthTGCodeDialogUi();

                if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.VerificationCode))
                {
                    code = dialog.VerificationCode;
                }
                else
                {
                    _client?.Dispose();
                }
            });

            return code;
        }

        private Task Client_OnOther(IObject arg)
        {
            if (arg is ReactorError err)
            {
                // typically: network connection was totally lost
                SendLogMessage($"Fatal reactor error: {err.Exception.Message}", LogMessageType.Error);

                if (ServerStatus != ServerConnectStatus.Disconnect)
                {
                    ServerStatus = ServerConnectStatus.Disconnect;
                    DisconnectEvent();
                }

            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _trackedChats.Clear();

            _client?.Dispose();

            if (ServerStatus != ServerConnectStatus.Disconnect)
            {
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent();
            }
        }

        public ServerType ServerType
        {
            get { return ServerType.TelegramNews; }
        }

        public ServerConnectStatus ServerStatus { get; set; }

        public event Action ConnectEvent;
        public event Action DisconnectEvent;

        #endregion

        #region 2 Properties

        public List<IServerParameter> ServerParameters { get; set; }

        private string _channelsIDsString;
        private static int _apiID;
        private static string _apiHash;
        private static string _phoneNumber;

        private static WTelegram.Client _client;
        private static WTelegram.UpdateManager Manager;
        private static Dictionary<string, string> _trackedChats = new Dictionary<string, string>();

        #endregion

        #region 3 News subscrible

        public bool SubscribeNews()
        {
            if (ServerStatus == ServerConnectStatus.Disconnect)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region 4 Messages parsing

        private Task UpdatesMessages(Update update)
        {
            if (update is UpdateNewMessage { message: Message message } &&
                !string.IsNullOrEmpty(message.message))
            {
                HandleMessage(message);
            }

            return Task.CompletedTask;
        }

        private void HandleMessage(Message message)
        {
            string chatId = message.Peer.ID.ToString();

            if (_trackedChats.TryGetValue(chatId, out string chatTitle))
            {
                string messageText = message.message;
                string source = string.IsNullOrEmpty(chatTitle) ? chatId : chatTitle;

                News news = new News();

                news.Source = source;
                news.Value = messageText;
                news.TimeMessage = message.Date.ToLocalTime();

                NewsEvent(news);
            }
        }

        public event Action<News> NewsEvent;

        #endregion

        #region 5 Log

        public event Action<string, LogMessageType> LogMessageEvent;

        private void SendLogMessage(string message, LogMessageType messageType)
        {
            LogMessageEvent(message, messageType);
        }

        #endregion

        #region 6 Not used functions

        public void CancelAllOrders()
        {
        }

        public void CancelAllOrdersToSecurity(Security security)
        {
        }

        public void CancelOrder(Order order)
        {
        }

        public void ChangeOrderPrice(Order order, decimal newPrice)
        {
        }

        public void Subscrible(Security security)
        {
        }

        public void GetAllActivOrders()
        {
        }

        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        public List<Candle> GetLastCandleHistory(Security security, TimeFrameBuilder timeFrameBuilder, int candleCount)
        {
            return null;
        }

        public void GetOrderStatus(Order order)
        {
        }

        public void GetPortfolios()
        {
            Portfolio portfolio = new Portfolio();
            portfolio.ValueCurrent = 1;
            portfolio.Number = "TelegramNews fake portfolio";

            if (PortfolioEvent != null)
            {
                PortfolioEvent(new List<Portfolio>() { portfolio });
            }
        }

        public void GetSecurities()
        {
            List<Security> securities = new List<Security>();

            Security fakeSec = new Security();
            fakeSec.Name = "Noname";
            fakeSec.NameId = "NonameId";
            fakeSec.NameClass = "NoClass";
            fakeSec.NameFull = "Nonamefull";

            securities.Add(fakeSec);

            SecurityEvent(securities);
        }

        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
        }

        public void SendOrder(Order order)
        {
        }

        public event Action<List<Security>> SecurityEvent;
        public event Action<List<Portfolio>> PortfolioEvent;
        public event Action<MarketDepth> MarketDepthEvent;
        public event Action<Trade> NewTradesEvent;
        public event Action<Order> MyOrderEvent;
        public event Action<MyTrade> MyTradeEvent;
        public event Action<OptionMarketDataForConnector> AdditionalMarketDataEvent;

        #endregion
    }
}
