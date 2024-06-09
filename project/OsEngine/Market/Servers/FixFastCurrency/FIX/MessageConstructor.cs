using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.FixFastCurrency.FIX
{
    public class MessageConstructor
    {
        public enum MessageType
        {
            Logon,
            Logout,
            Heartbeat,
            TestRequest,
            ResendRequest,
            Reject,
            SequenceReset,
            NewOrder,
            OrderCancel,
            OrderMassCancel,
            OrderReplace
        }

        private string _senderCompID;
        private string _targetCompID;

        public MessageConstructor(string senderCompID, string targetCompID)
        {
            _senderCompID = senderCompID;
            _targetCompID = targetCompID;
        }

        #region New Order
        /// <summary>
        /// Создать новый ордер
        /// </summary>
        /// <param name="clordId">11. Уникальный  пользовательский  идентификатор  заявки, установленный  торгующей  организацией  или  инвестором, интересы которого представляет посредническая организация.</param>
        /// <param name="partiesParams">Стороны заявки. Обычно содержит код клиента. </param>
        /// <param name="instrumentParams">В  состав  группы  входит  поле  Symbol (55),  идентификатор финансового  инструмента. </param>
        /// <param name="orderQtyParams">Объем заявки, выраженный в лотах</param>
        /// <param name="account">1. Торговый счет, в счет которого подается заявка.</param>
        /// <param name="maxFloor">111. Максимальное  количество  лотов  в  пределах  объема  заявки, которое будет показано на бирже в любой момент времени (Для заявок типа Айсберг).</param>
        /// <param name="aceberg">Тип заявки Айсберг?</param>
        /// <param name="noTradingSessions">386. Количество элементов в группе TradingSessionIDs.</param>
        /// <param name="tradingSessionID">336. Идентификатор  торговой  сессии.  В  качестве  идентификатора торговой сессии используется режим торгов (SECBOARD).</param>
        /// <param name="side">56. Направление заявки. '1' (Покупка)'2' (Продажа)</param>
        /// <param name="ordType">40. Тип заявки. '1' (Рыночная)'2' (Лимитная)</param>
        /// <param name="price">Цена  заявки,  используется  для  лимитной  заявки.  Поле обязательное, если задано в заявке. Для рыночных заявок должно быть заполнено 0.</param>
        /// <returns></returns>
        public string NewOrderMessage(string clordId, string[] partiesParams, string[] instrumentParams, string orderQty,
                                      string account, string maxFloor, bool aceberg, string tradingSessionID,
                                      string side, string ordType, string price, int messageSequenceNumber, string noTradingSessions = "1")
        {
            var body = new StringBuilder();

            body.Append("11=" + clordId + "|");

            if (partiesParams != null)
                body.Append(ConstructParties(partiesParams));

            body.Append("1=" + account + "|");

            if (aceberg)
                body.Append("111=" + maxFloor + "|");

            body.Append("386=" + noTradingSessions + "|");
            body.Append("336=" + tradingSessionID + "|");
            body.Append(ConstructInstrument(instrumentParams));
            body.Append("54=" + side + "|");
            body.Append("60=" + DateTime.UtcNow.ToString("yyMMdd-HH:mm:ss.fffffff").Insert(16, "00") + "|");
            body.Append("38=" + orderQty + "|");
            body.Append("40=" + ordType + "|");

            if (ordType == "1")
                body.Append("44=0|");
            else
                body.Append("44=" + price + "|");

            var header = ConstructHeader(SessionMessageCode(MessageType.NewOrder), messageSequenceNumber, body.ToString());
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }


        /// <summary>
        /// Стороны заявки. Обычно содержит код клиента.
        /// </summary>
        /// <returns></returns>
        public string ConstructParties(string[] partiesParams)
        {
            // [0] - noPartId453, [1] partyId448, [3] partyIdSource447, [4] partyRole452
            var msg = new StringBuilder();

            //Количество сторон
            msg.Append("453=" + partiesParams[0] + "|");

            msg.Append("447=" + partiesParams[3] + "|");

            if (partiesParams[3] != null)
                msg.Append("448=" + partiesParams[1] + "|");

            //Heartbeat interval in seconds.
            msg.Append("452=" + partiesParams[4] + "|");



            return msg.ToString();
        }
        /// <summary>
        /// финансовый инструмент, который торгуется на бирже
        /// </summary>
        /// <param name="symbol55">Код/аббревиатура ценной бумаги</param>
        /// <param name="product460">Идентификатор, показывающий к какому продукту относится ценная бумага (только для валютного рынка).</param>
        /// <param name="CFICode461">Тип финансового инструмента по стандарту ISO 10962 (только для валютного рынка).</param>
        /// <param name="securityType167">Тип финансового инструмента. 'FXSPOT' (Валютный спот); 'FXSWAP' (Валютный своп);</param>
        /// <returns></returns>
        public string ConstructInstrument(string[] instrumentParams)
        {
            // [0] symbol55, [1] CFICode461, [2] securityType167,  product460 = "4" - Валюта

            var msg = new StringBuilder();

            msg.Append("55=" + instrumentParams[0] + "|");
            msg.Append("460=4|");
            // msg.Append("461=MRCXXX|");  необязательный параметр, пробую без него
            msg.Append("167=" + instrumentParams[1] + "|");

            return msg.ToString();
        }

        /// <summary>
        /// количество лотов заявки или сделки
        /// </summary>
        /// <param name="orderQty38">Объем заявки или сделки, выраженный в лотах.</param>
        /// <param name="cashOrderQty152">Объем заявки в единицах денежных средств. </param>
        /// <param name="orderLot">допускается заполнение только одного из полей</param>
        /// <returns></returns>
        public string ConstructOrderQtyData(string[] orderQtyParams)
        {
            // [0] Lots or Cash, [1] quantity

            var qty = new StringBuilder();

            if (orderQtyParams[0] == "lot")
            {
                qty.Append("38=" + orderQtyParams[1] + "|");
            }
            else
            {
                qty.Append("152=" + orderQtyParams[1] + "|");
            }

            return qty.ToString();
        }
        #endregion

        #region Order Cansel/Replace Request
        /// <summary>
        /// 
        /// </summary>
        /// <param name="origClOrdId">Пользовательский  идентификатор  заявки,  которую  надо  снять.</param>
        /// <param name="clordId">Уникальный идентификатор сообщения Order Cancel Request (F) - запроса на снятие заявки.</param>
        /// <param name="side"></param>
        /// <param name="messageSequenceNumber"></param>
        /// <returns></returns>
        public string OrderCanselMessage(string origClOrdId, string clordId, string side, int messageSequenceNumber)
        {
            var body = new StringBuilder();

            body.Append("41=" + origClOrdId + "|");
            body.Append("11=" + clordId + "|");
            body.Append("54=" + side + "|");
            body.Append("60=" + DateTime.UtcNow.ToString("yyMMdd-HH:mm:ss.fffffff").Insert(16, "00") + "|");

            var header = ConstructHeader(SessionMessageCode(MessageType.OrderCancel), messageSequenceNumber, body.ToString());
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }

        /// <summary>
        /// Изменение параметров заявки. Можно изменить цену заявки (44), количество (38), или поле SecondaryClOrdID
        /// </summary>
        /// <param name="clordId">Уникальный идентификатор сообщения на замену заявки.(11)</param>
        /// <param name="origClOrdId">Пользовательский   идентификатор   заявки,   которую   надо   изменить(41).</param>
        /// <param name="account">Торговый счет. Должен совпадать с номером счета исходной заявки.</param>
        /// <param name="instrumentParams">Финансовый инструмент, по которому была подана изменяемая заявка. Должно совпадать со значением в исходной заявке.</param>
        /// <param name="price">Цена за единицу ценной бумаги. </param>
        /// <param name="orderQty">Количество ценных бумаг, выраженное в лотах</param>
        /// <param name="side">Направленность  заявки.  Должно  совпадать  со  значением  в  исходной заявке.</param>
        /// <param name="messageSequenceNumber"></param>
        /// <param name="tradingSessionID">Идентификатор режима торгов для финансового инструмента, заявки по которому  должны  быть  изменены. </param>
        /// <param name="ordType">Тип изменяемой заявки. Должен быть тем же, что и в исходной заявке.</param>
        /// <param name="noTradingSessions"></param>
        /// <returns></returns>
        public string OrderReplaceMessage(string clordId, string origClOrdId, string account, string[] instrumentParams, string price, string orderQty, string side, string tradingSessionID, string ordType, int messageSequenceNumber, string noTradingSessions = "1")
        {
            var body = new StringBuilder();

            body.Append("11=" + clordId + "|");
            body.Append("41=" + origClOrdId + "|");
            body.Append("1=" + account + "|");
            body.Append(ConstructInstrument(instrumentParams));
            body.Append("44=" + price + "|");
            body.Append("38=" + orderQty + "|");
            body.Append("386=" + noTradingSessions + "|");
            body.Append("336=" + tradingSessionID + "|");
            body.Append("40=" + ordType + "|");
            body.Append("54=" + side + "|");
            body.Append("60=" + DateTime.UtcNow.ToString("yyMMdd-HH:mm:ss.fffffff").Insert(16, "00") + "|");

            var header = ConstructHeader(SessionMessageCode(MessageType.OrderReplace), messageSequenceNumber, body.ToString());
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }
        #endregion

        #region Session Messages
        /// <summary>
        /// Logons the message.
        /// </summary>
        /// <param name="qualifier">The session qualifier.</param>
        /// <param name="messageSequenceNumber">The message sequence number.</param>
        /// <param name="heartBeatSeconds">The heart beat seconds.</param>
        /// <param name="resetSeqNum">All sides of FIX session should have sequence numbers reset. Valid value
        ///is "Y" = Yes(reset)..</param>
        /// <returns></returns>
        public string LogonMessage(string password, int messageSequenceNumber, int heartBeatSeconds, bool resetSeqNum)
        {
            var body = new StringBuilder();

            //EncryptMethod
            body.Append("98=0|");

            //Heartbeat interval in seconds.
            body.Append("108=" + heartBeatSeconds + "|");

            // Индикатор,  указывающий  должны  ли  обе  стороны  сбросить  счетчики сообщений. По умолчанию "Нет"
            if (resetSeqNum)
                body.Append("141=Y|");
            else body.Append("141=N|");

            // Пароль пользователя. 
            body.Append("554=" + password + "|");

            var header = ConstructHeader(SessionMessageCode(MessageType.Logon), messageSequenceNumber, body.ToString());
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }

        /// <summary>
        /// Сообщение Heartbeat (0) используется для контроля состояния соединения.
        /// Если Heartbeat(0) сообщение посылается в ответ на Test Request(1) сообщение, то в первом - поле TestReqID(112) 
        /// должно содержать идентификатор Test Request(1) сообщения, на которое оно является ответом.
        /// Это используется для того, чтобы определить является ли Heartbeat(0) сообщение ответом на Test Request(1) сообщение.
        /// </summary>
        /// <param name="messageSequenceNumber"></param>
        /// <param name="isResponce">Является ли ответом на Test Request?</param>
        /// <returns></returns>
        public string HeartbeatMessage(int messageSequenceNumber, bool isResponce, string testReqID)
        {
            var body = new StringBuilder();
            if (isResponce)
                body.Append($"112={testReqID}|");
            var header = ConstructHeader(SessionMessageCode(MessageType.Heartbeat), messageSequenceNumber, body.ToString());
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }

        /// <summary>
        /// Test Request (1) сообщение вызывает/инициирует/запрашивает Heartbeat (0) сообщение с противоположной стороны.
        /// Сообщение Test Request (1) проверяет порядковые номера или проверяет состояние соединения. 
        /// На Test Request (1) сообщение противоположная сторона отвечает Heartbeat (0) сообщением,
        /// в котором TestReqID (112) – идентификатор (1) сообщения.
        /// </summary>
        /// <param name="messageSequenceNumber"></param>
        /// <param name="testRequestID"></param>
        public string TestRequestMessage(int messageSequenceNumber, int testRequestID)
        {
            var body = new StringBuilder();
            //Heartbeat message ID. TestReqID should be incremental.
            body.Append("112=" + testRequestID + "|");
            var header = ConstructHeader(SessionMessageCode(MessageType.TestRequest), messageSequenceNumber, body.ToString());
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }

        public string LogoutMessage(int messageSequenceNumber)
        {
            var header = ConstructHeader(SessionMessageCode(MessageType.Logout), messageSequenceNumber, string.Empty);
            var trailer = ConstructTrailer(header);
            var headerAndMessageAndTrailer = header + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }

        /// <summary>
        /// Сообщение Resend Request (2) используется для инициирования повторной пересылки сообщений.
        /// Эта функция используется в случаях, если обнаружено расхождение в порядковых номерах сообщений или как функция процесса инициализации. 
        /// </summary>
        /// <param name="messageSequenceNumber"></param>
        /// <param name="beginSequenceNo">Номер первого сообщения, которое нужно повторно переслать.(7)</param>
        /// <param name="endSequenceNo">Номер последнего сообщения, которое нужно повторно переслать.(16)</param>
        /// <returns></returns>
        public string ResendMessage(int messageSequenceNumber, int beginSequenceNo, int endSequenceNo)
        {
            var body = new StringBuilder();
            body.Append("7=" + beginSequenceNo + "|");
            body.Append("16=" + endSequenceNo + "|");
            var header = ConstructHeader(SessionMessageCode(MessageType.ResendRequest), messageSequenceNumber, body.ToString());
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }

        /// <summary>
        /// Передается в обоих направлениях. 
        /// Указывает на неверно переданное или недопустимое сообщение сессионного уровня, пришедшее от противоположной стороны.
        /// </summary>
        /// <param name="messageSequenceNumber"></param>
        /// <param name="rejectSequenceNumber">Номер отклоняемого сообщения</param>
        /// <returns></returns>
        public string RejectMessage(int messageSequenceNumber, int rejectSequenceNumber)
        {
            var body = new StringBuilder();

            body.Append("45=" + rejectSequenceNumber + "|");
            var header = ConstructHeader(SessionMessageCode(MessageType.Reject), messageSequenceNumber, string.Empty);
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }

        /// <summary>
        /// Сообщение Sequence Reset (4) имеет следующие режимы:
        ///  - Режим заполнения пробелов(используется поле MsgSeqNum);
        ///  - Режим сбрасывания счетчиков (поле MsgSeqNum игнорируется).
        /// </summary>
        /// <param name="messageSequenceNumber"></param>
        /// <param name="newSequenceNumber">Новый порядковый номер</param>
        /// <param name="fillingGaps">Режим заполнения пробелов(true)/Режим сбрасывания счетчиков(false)</param>

        public string SequenceResetMessage(int messageSequenceNumber, int newSequenceNumber, bool fillingGaps)
        {
            var body = new StringBuilder();

            if (fillingGaps)
                body.Append("123=Y|");
            else
                body.Append("123=N|");

            body.Append("36=" + newSequenceNumber + "|");

            var header = ConstructHeader(SessionMessageCode(MessageType.SequenceReset), messageSequenceNumber, string.Empty);
            var headerAndBody = header + body;
            var trailer = ConstructTrailer(headerAndBody);
            var headerAndMessageAndTrailer = header + body + trailer;
            return headerAndMessageAndTrailer.Replace("|", "\u0001");
        }
        #endregion

        #region Header and Trailer
        /// <summary>
        /// Constructs the message header.
        /// </summary>
        /// <param name="type">The message type.</param>
        /// <param name="messageSequenceNumber">The message sequence number.</param>
        /// <param name="bodyMessage">The body message.</param>

        ///   string endHeader = $"35=A{S}49={SenderCompId}{S}56={TargetCompId}{S}34={++numberMessage}{S}52={_sendingTime}{S}";
        //string beginHeader = $"8=FIX.4.4{S}9={bodyLength + endHeader.Length}{S}";

        private string ConstructHeader(string type, int messageSequenceNumber, string bodyMessage)
        {
            var header = new StringBuilder();

            header.Append("8=FIX.4.4|");

            var message = new StringBuilder();

            // Message type. Always unencrypted, must be third field in message.
            message.Append("35=" + type + "|");

            message.Append("49=" + _senderCompID + "|");

            message.Append("56=" + _targetCompID + "|");

            // Message Sequence Number
            message.Append("34=" + messageSequenceNumber + "|");

            // Время передачи сообщения (выражено во временной зоне UTC). Формат поля YYYYMMDD-HH:MM:SS.sssssssss. 
            message.Append("52=" + DateTime.UtcNow.ToString("yyMMdd-HH:mm:ss.fffffff").Insert(16, "00") + "|");
            var length = message.Length + bodyMessage.Length;

            // Message body length. Must be second field in message.
            header.Append("9=" + length + "|");
            header.Append(message);
            return header.ToString();
        }

        /// <summary>
        /// Constructs the message trailer.
        /// </summary>
        /// <param name="message">The message trailer.</param>
        /// <returns></returns>
        private string ConstructTrailer(string message)
        {
            //Three byte, simple checksum. Always last field in message; i.e. serves,
            //with the trailing<SOH>, 
            //as the end - of - message delimiter. Always defined as three characters
            //(and always unencrypted).
            var trailer = "10=" + CalculateChecksum(message.Replace("|", "\u0001").ToString()).ToString().PadLeft(3, '0') + "|";
            return trailer;
        }

        /// <summary>
        /// Calculates the checksum.
        /// </summary>
        /// <param name="dataToCalculate">The data to calculate.</param>
        /// <returns></returns>
        private int CalculateChecksum(string dataToCalculate)
        {
            byte[] byteToCalculate = Encoding.ASCII.GetBytes(dataToCalculate);
            int checksum = 0;
            foreach (byte chData in byteToCalculate)
            {
                checksum += chData;
            }
            return checksum % 256;
        }

        #endregion

        /// <summary>
        /// Returns the session the message code.
        /// </summary>
        /// <param name="type">The session message type.</param>
        /// <returns></returns>
        private string SessionMessageCode(MessageType type)
        {
            switch (type)
            {
                case MessageType.Heartbeat:
                    return "0";
                    break;

                case MessageType.Logon:
                    return "A";
                    break;

                case MessageType.Logout:
                    return "5";
                    break;

                case MessageType.Reject:
                    return "3";
                    break;

                case MessageType.ResendRequest:
                    return "2";
                    break;

                case MessageType.SequenceReset:
                    return "4";
                    break;

                case MessageType.TestRequest:
                    return "1";
                    break;

                case MessageType.NewOrder:
                    return "D";
                    break;

                case MessageType.OrderCancel:
                    return "F";
                    break;

                case MessageType.OrderMassCancel:
                    return "q";
                    break;

                case MessageType.OrderReplace:
                    return "G";
                    break;

                default:
                    return "0";
            }
        }
    }
}
