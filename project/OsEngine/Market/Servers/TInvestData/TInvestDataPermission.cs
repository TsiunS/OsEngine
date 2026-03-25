/*
 *Your rights to use the code are governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

namespace OsEngine.Market.Servers.TInvestData
{
    public class TInvestDataServerPermission : IServerPermission
    {
        public ServerType ServerType
        {
            get { return ServerType.TInvestData; }
        }

        #region DataFeedPermissions

        public bool DataFeedTf1SecondCanLoad => false;
        public bool DataFeedTf2SecondCanLoad => false;
        public bool DataFeedTf5SecondCanLoad => false;
        public bool DataFeedTf10SecondCanLoad => false;
        public bool DataFeedTf15SecondCanLoad => false;
        public bool DataFeedTf20SecondCanLoad => false;
        public bool DataFeedTf30SecondCanLoad => false;
        public bool DataFeedTf1MinuteCanLoad => true;
        public bool DataFeedTf2MinuteCanLoad => false;
        public bool DataFeedTf5MinuteCanLoad => false;
        public bool DataFeedTf10MinuteCanLoad => false;
        public bool DataFeedTf15MinuteCanLoad => false;
        public bool DataFeedTf30MinuteCanLoad => false;
        public bool DataFeedTf1HourCanLoad => false;
        public bool DataFeedTf2HourCanLoad => false;
        public bool DataFeedTf4HourCanLoad => false;
        public bool DataFeedTfDayCanLoad => false;
        public bool DataFeedTfTickCanLoad => true;
        public bool DataFeedTfMarketDepthCanLoad => false;

        #endregion

        #region Trade permission
        public bool MarketOrdersIsSupport => false;
        public bool IsTradeServer => false;
        public bool IsCanChangeOrderPrice => false;
        public TimeFramePermission TradeTimeFramePermission => _tradeTimeFramePermission;
        public int WaitTimeSecondsAfterFirstStartToSendOrders => 60;

        private TimeFramePermission _tradeTimeFramePermission = new TimeFramePermission();

        public bool UseStandardCandlesStarter => false;
        public bool IsUseLotToCalculateProfit => false;
        public bool ManuallyClosePositionOnBoard_IsOn => false;
        public string[] ManuallyClosePositionOnBoard_ValuesForTrimmingName => null;
        public string[] ManuallyClosePositionOnBoard_ExceptionPositionNames => null;
        public bool CanQueryOrdersAfterReconnect => false;
        public bool CanQueryOrderStatus => false;
        public bool CanGetOrderLists => false;
        public bool HaveOnlyMakerLimitsRealization => false;
        #endregion

        #region Other Permissions

        public bool IsNewsServer => false;
        public bool IsSupports_CheckDataFeedLogic => false;
        public string[] CheckDataFeedLogic_ExceptionSecuritiesClass => null;
        public int CheckDataFeedLogic_NoDataMinutesToDisconnect => 10;
        public bool IsSupports_MultipleInstances => false;
        public bool IsSupports_ProxyFor_MultipleInstances => false;

        public bool IsSupports_AsyncOrderSending => false;

        public int AsyncOrderSending_RateGateLimitMls => 10;

        public bool IsSupports_AsyncCandlesStarter => false;

        public int AsyncCandlesStarter_RateGateLimitMls => 50;

        public string[] IpAddressServer => null;

        public bool Leverage_IsSupports => false;

        public decimal Leverage_StandardValue => 10;

        public string[] Leverage_SupportClasses => null;

        public bool CanChangeOrderMarketNumber => false;
        #endregion
    }
}

