using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;


namespace OsEngine.Market.Servers.TInvestData.Entity
{
    public class CsvFastParser
    {
        public List<Trade> GetTradesFromCsv(string csvFilePath, Security security)
        {
            var trades = new List<Trade>(10000); // Предварительное выделение памяти

            if (!File.Exists(csvFilePath))
                return trades;

            string targetSec = security.Name + "_" + security.NameClass;

            using (var reader = new StreamReader(csvFilePath, Encoding.UTF8, false, 65536)) // 64KB буфер
            {
                // Пропускаем заголовок
                reader.ReadLine();

                char[] buffer = new char[512]; // Буфер для построчной обработки
                var lineBuilder = new StringBuilder(256);

                while (reader.ReadLine() is string line)
                {
                    if (line.Length == 0)
                        continue;

                    // Быстрая проверка инструмента
                    if (!ContainsInstrumentFast(line, targetSec))
                        continue;

                    // Парсинг строки
                    var trade = ParseTradeLineFast(line, security);
                    trades.Add(trade);
                }
            }

            return trades;
        }

        private bool ContainsInstrumentFast(string line, string targetInstrument)
        {
            // Находим позицию первого разделителя
            int firstComma = line.IndexOf(',');
            if (firstComma == -1) return false;

            // Находим позицию второго разделителя
            int secondComma = line.IndexOf(',', firstComma + 1);
            if (secondComma == -1) return false;

            // Проверяем, что есть место для второго поля
            int start = firstComma + 1;
            int length = secondComma - start;

            // Проверка границ
            if (start < 0 || start + length > line.Length) return false;

            // Быстрая проверка длины
            if (length != targetInstrument.Length) return false;

            // Посимвольное сравнение
            for (int i = 0; i < length; i++)
            {
                if (line[start + i] != targetInstrument[i])
                    return false;
            }

            return true;
        }

        private Trade ParseTradeLineFast(string line, Security security)
        {
            var trade = new Trade
            {
                SecurityNameCode = security.Name
            };

            try
            {
                ReadOnlySpan<char> span = line.AsSpan();

                // Находим позиции всех запятых с проверкой границ
                int comma1 = span.IndexOf(',');
                if (comma1 == -1) return trade;

                int comma2 = span.Slice(comma1 + 1).IndexOf(',');
                if (comma2 == -1) return trade;
                comma2 += comma1 + 1;

                int comma3 = span.Slice(comma2 + 1).IndexOf(',');
                if (comma3 == -1) return trade;
                comma3 += comma2 + 1;

                int comma4 = span.Slice(comma3 + 1).IndexOf(',');
                if (comma4 == -1) return trade;
                comma4 += comma3 + 1;

                int comma5 = span.Slice(comma4 + 1).IndexOf(',');
                if (comma5 == -1) return trade;
                comma5 += comma4 + 1;

                int comma6 = span.Slice(comma5 + 1).IndexOf(',');
                if (comma6 == -1) return trade;
                comma6 += comma5 + 1;

                // Проверяем границы перед парсингом
                if (comma1 <= span.Length && comma2 <= span.Length && comma3 <= span.Length &&
                    comma4 <= span.Length && comma5 <= span.Length && comma6 <= span.Length)
                {
                    trade.Time = ParseDateTimeFast(span.Slice(0, comma1));
                    trade.Side = ParseSideFast(span.Slice(comma2 + 1, comma3 - comma2 - 1));

                    ReadOnlySpan<char> priceSlice = span.Slice(comma3 + 1, comma4 - comma3 - 1);
                    string priceString = priceSlice.ToString();
                    trade.Price = priceString.ToDecimal();

                    var volSlice = span.Slice(comma4 + 1, comma5 - comma4 - 1);
                    string volString = volSlice.ToString();
                    trade.Volume = Math.Abs(volString.ToDecimal());

                    trade.Id = (DateTime.UtcNow.Ticks + trade.Time.Millisecond).ToString();
                }
            }
            catch
            {
                // В случае ошибки возвращаем пустую сделку
                return trade;
            }

            return trade;
        }

        private DateTime ParseDateTimeFast(ReadOnlySpan<char> dateTimeSpan)
        {
            if (dateTimeSpan.Length < 19)
            {
                return ParseDateTimeSafe(dateTimeSpan);
            }

            try
            {
                int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                int millisecond = 0;

                // Парсим год (0-3)
                for (int i = 0; i < 4; i++)
                    year = year * 10 + (dateTimeSpan[i] - '0');

                // Парсим месяц (5-6)
                for (int i = 5; i < 7; i++)
                    month = month * 10 + (dateTimeSpan[i] - '0');

                // Парсим день (8-9)
                for (int i = 8; i < 10; i++)
                    day = day * 10 + (dateTimeSpan[i] - '0');

                // Парсим час (11-12)
                for (int i = 11; i < 13; i++)
                    hour = hour * 10 + (dateTimeSpan[i] - '0');

                // Парсим минуту (14-15)
                for (int i = 14; i < 16; i++)
                    minute = minute * 10 + (dateTimeSpan[i] - '0');

                // Парсим секунду (17-18)
                for (int i = 17; i < 19; i++)
                    second = second * 10 + (dateTimeSpan[i] - '0');

                // Проверяем наличие миллисекунд
                if (dateTimeSpan.Length > 19 && dateTimeSpan[19] == '.')
                {
                    int startMs = 20;
                    int endMs = dateTimeSpan.Length - 1; // пропускаем 'Z'

                    if (startMs < endMs && startMs < dateTimeSpan.Length)
                    {
                        int msDigits = 0;
                        for (int i = startMs; i < endMs && i < startMs + 6; i++)
                        {
                            if (dateTimeSpan[i] >= '0' && dateTimeSpan[i] <= '9')
                            {
                                millisecond = millisecond * 10 + (dateTimeSpan[i] - '0');
                                msDigits++;
                            }
                        }

                        // Нормализуем до 3 цифр
                        if (msDigits < 3)
                        {
                            for (int i = 0; i < 3 - msDigits; i++)
                                millisecond *= 10;
                        }
                        else if (msDigits > 3)
                        {
                            for (int i = 0; i < msDigits - 3; i++)
                                millisecond /= 10;
                        }
                    }
                }

                var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                return dateTime.AddHours(3).AddMilliseconds(millisecond);
            }
            catch
            {
                return ParseDateTimeSafe(dateTimeSpan);
            }
        }

        private DateTime ParseDateTimeSafe(ReadOnlySpan<char> dateTimeSpan)
        {
            string dateStr = dateTimeSpan.ToString();
            string[] formats = {
                                "yyyy-MM-ddTHH:mm:ss.ffffffZ",
                                "yyyy-MM-ddTHH:mm:ss.fffZ",
                                "yyyy-MM-ddTHH:mm:ssZ",
                                "yyyy-MM-ddTHH:mm:ss.fffffffZ"
                                };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result))
                {
                    return result.AddHours(3);
                }
            }

            // Если ничего не подошло, пробуем стандартный парсинг
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var fallback))
            {
                return fallback.AddHours(3);
            }

            return DateTime.UtcNow.AddHours(3);
        }

        private Side ParseSideFast(ReadOnlySpan<char> sideSpan)
        {
            // Быстрое сравнение "BUY" (3 символа)
            if (sideSpan.Length == 3 &&
                sideSpan[0] == 'B' &&
                sideSpan[1] == 'U' &&
                sideSpan[2] == 'Y')
            {
                return Side.Buy;
            }

            return Side.Sell;
        }

        private decimal ParseDecimalFast(ReadOnlySpan<char> decimalSpan)
        {
            // Ручной парсинг decimal без аллокаций
            long integerPart = 0;
            long fractionalPart = 0;
            int fractionalDigits = 0;
            bool hasFraction = false;

            for (int i = 0; i < decimalSpan.Length; i++)
            {
                char c = decimalSpan[i];

                if (c == '.')
                {
                    hasFraction = true;
                    continue;
                }

                if (!hasFraction)
                {
                    integerPart = integerPart * 10 + (c - '0');
                }
                else
                {
                    fractionalPart = fractionalPart * 10 + (c - '0');
                    fractionalDigits++;
                }
            }

            decimal result = integerPart;

            if (hasFraction)
            {
                // Делим на соответствующую степень 10
                for (int i = 0; i < fractionalDigits; i++)
                    fractionalPart *= 10;

                result += fractionalPart / (decimal)Math.Pow(10, fractionalDigits);
            }

            return result;
        }
    }
}
