using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.FixFastCurrency.FIX
{
    internal class TradingSessionStatus
    {

        public Dictionary<string, string> tradSesStatus = new Dictionary<string, string>
        {

            {"100", "Торговая система перезапущена/возобновлена"},
            {"101", "Соединение с MOEX установлено"},
            {"102", "Соединение с MOEX завершено корректно"},
            {"103", "Соединение с MOEX завершено некорректно"},
            {"104", "Повторное соединение с MOEX"}

        };
    }
}
